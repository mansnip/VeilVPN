using Application.Services.Interfaces;
using Domain.Entities;
using Domain.Interfaces;
using Domain.ViewModels.UserPanel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services.Implementations
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly IUserRepository _userRepository;
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly ISubscriptionRepository _subscriptionRepository;

        // قیمت هر گیگابایت (تومان)
        private const int PricePerGB = 2500;
        // قیمت هر روز (تومان)
        private const int PricePerDay = 1500;

        public SubscriptionService(
            IUserRepository userRepository,
            IInvoiceRepository invoiceRepository,
            ISubscriptionRepository subscriptionRepository)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _invoiceRepository = invoiceRepository ?? throw new ArgumentNullException(nameof(invoiceRepository));
            _subscriptionRepository = subscriptionRepository ?? throw new ArgumentNullException(nameof(subscriptionRepository));
        }

        public async Task<SubscriptionViewModel> GetUserActiveSubscriptionAsync(string userId)
        {
            // دریافت اشتراک فعال کاربر
            var subscription = await _subscriptionRepository.GetActiveSubscriptionByUserIdAsync(userId);

            if (subscription == null)
                return null;

            // تبدیل به ViewModel
            return new SubscriptionViewModel
            {
                Id = subscription.Id,
                UserId = subscription.UserId,
                Traffic = subscription.Traffic,
                Duration = subscription.Duration,
                StartDate = subscription.StartDate,
                EndDate = subscription.EndDate,
                RemainingTraffic = subscription.RemainingTraffic,
                IsActive = subscription.IsActive,
                DaysRemaining = (subscription.EndDate - DateTime.Now).Days > 0 ? (subscription.EndDate - DateTime.Now).Days : 0,
                PercentTrafficUsed = (int)(((subscription.Traffic - subscription.RemainingTraffic) * 100) / subscription.Traffic)
            };
        }

        public async Task<List<UsageHistoryViewModel>> GetUsageHistoryAsync(string userId)
        {
            // این متد می‌تواند اطلاعات مصرف را از دیتابیس یا منبع دیگری دریافت کند
            // برای مثال فعلی، داده‌های تصادفی برمی‌گرداند

            List<UsageHistoryViewModel> usageHistory = new List<UsageHistoryViewModel>();

            // ایجاد داده‌های مصرف تصادفی برای ۷ روز گذشته
            DateTime today = DateTime.Now;
            Random random = new Random();

            for (int i = 6; i >= 0; i--)
            {
                DateTime day = today.AddDays(-i);
                double usage = Math.Round(random.NextDouble() * 1.5 + 0.5, 2); // مصرف بین ۰.۵ تا ۲ گیگابایت

                usageHistory.Add(new UsageHistoryViewModel
                {
                    Date = day,
                    UsageGB = usage
                });
            }

            return usageHistory;
        }

        // محاسبه قیمت اشتراک بر اساس ترافیک و مدت زمان
        public async Task<int> CalculatePrice(int trafficGB, int durationDays)
        {
            // کد قبلی بدون تغییر
            // اعتبارسنجی مقادیر ورودی
            trafficGB = ValidateTraffic(trafficGB);
            durationDays = ValidateDuration(durationDays);

            // محاسبه هزینه ترافیک و مدت زمان
            int trafficCost = trafficGB * PricePerGB;
            int durationCost = durationDays * PricePerDay;

            // قیمت پایه
            double basePrice = trafficCost + durationCost;

            // محاسبه تخفیف بر اساس مدت زمان
            double discount = CalculateDiscount(durationDays);

            // اعمال تخفیف
            double finalPrice = basePrice * (1 - discount);

            // گرد کردن به هزار تومان نزدیک‌تر
            int roundedPrice = (int)Math.Round(finalPrice / 1000) * 1000;

            // شبیه‌سازی تأخیر شبکه یا محاسبات پیچیده (می‌توانید حذف کنید)
            await Task.Delay(10);

            return roundedPrice;
        }

        // محاسبه قیمت با جزئیات بیشتر
        public async Task<SubscriptionPriceDetails> CalculateDetailedPrice(int trafficGB, int durationDays)
        {
            // کد قبلی بدون تغییر
            // اعتبارسنجی مقادیر ورودی
            trafficGB = ValidateTraffic(trafficGB);
            durationDays = ValidateDuration(durationDays);

            // محاسبه هزینه ترافیک و مدت زمان
            int trafficCost = trafficGB * PricePerGB;
            int durationCost = durationDays * PricePerDay;

            // قیمت پایه
            decimal basePrice = trafficCost + durationCost;

            // محاسبه درصد تخفیف بر اساس مدت زمان
            double discountRate = CalculateDiscount(durationDays);
            decimal discountPercent = (decimal)(discountRate * 100);

            // محاسبه مبلغ تخفیف
            decimal discountAmount = basePrice * (decimal)discountRate;

            // اعمال تخفیف
            decimal finalPrice = basePrice - discountAmount;

            // گرد کردن به هزار تومان نزدیک‌تر
            finalPrice = Math.Round(finalPrice / 1000) * 1000;

            return new SubscriptionPriceDetails
            {
                BasePrice = basePrice,
                DiscountPercent = discountPercent,
                DiscountAmount = discountAmount,
                FinalPrice = finalPrice
            };
        }

        // متدهای کمکی مانند قبل
        private int ValidateTraffic(int trafficGB)
        {
            return Math.Clamp(trafficGB, 10, 500);
        }

        private int ValidateDuration(int durationDays)
        {
            return Math.Clamp(durationDays, 15, 365);
        }

        public double CalculateDiscount(int durationDays)
        {
            if (durationDays >= 300 && durationDays <= 365)
                return 0.20; // 20% تخفیف
            else if (durationDays >= 180 && durationDays <= 299)
                return 0.15; // 15% تخفیف
            else if (durationDays >= 90 && durationDays <= 179)
                return 0.10; // 10% تخفیف
            else
                return 0.0; // بدون تخفیف
        }

        public async Task<bool> CreateSubscriptionFromInvoiceAsync(string invoiceId)
        {
            try
            {
                // دریافت فاکتور از دیتابیس
                var invoice = await _invoiceRepository.GetByIdAsync(invoiceId);

                if (invoice == null || invoice.Status != "پرداخت شده")
                    return false;

                // ایجاد اشتراک جدید
                var subscription = new Subscription
                {
                    UserId = invoice.UserId,
                    Traffic = invoice.Traffic,
                    Duration = invoice.Duration,
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddDays(invoice.Duration),
                    RemainingTraffic = invoice.Traffic,
                    IsActive = true,
                    InvoiceId = invoice.Id
                };

                // ذخیره اشتراک در دیتابیس
                await _subscriptionRepository.CreateAsync(subscription);

                return true;
            }
            catch
            {
                return false;
            }
        }

        // دریافت اشتراک‌های کاربر
        public async Task<List<SubscriptionViewModel>> GetUserSubscriptionsAsync(string userId)
        {
            var subscriptions = await _subscriptionRepository.GetUserSubscriptionsAsync(userId);

            if (subscriptions == null || !subscriptions.Any())
                return new List<SubscriptionViewModel>();

            return subscriptions.Select(sub => new SubscriptionViewModel
            {
                Id = sub.Id,
                Traffic = sub.Traffic,
                Duration = sub.Duration,
                RemainingTraffic = sub.RemainingTraffic,
                StartDate = sub.StartDate,
                EndDate = sub.EndDate,
                IsActive = sub.IsActive,
                DaysRemaining = (sub.EndDate - DateTime.Now).Days > 0 ? (sub.EndDate - DateTime.Now).Days : 0,
                PercentTrafficUsed = (int)(100 - ((double)sub.RemainingTraffic / sub.Traffic) * 100)
            }).ToList();
        }

        // بررسی وضعیت اشتراک فعال کاربر
        public async Task<SubscriptionStatusViewModel> GetUserSubscriptionStatus(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentNullException(nameof(userId));

            var hasActiveSubscription = await _subscriptionRepository.IsUserHasActiveSubscriptionAsync(userId);

            if (!hasActiveSubscription)
                return new SubscriptionStatusViewModel { HasActiveSubscription = false };

            var activeSubscription = await _subscriptionRepository.GetUserActiveSubscriptionAsync(userId);

            return new SubscriptionStatusViewModel
            {
                HasActiveSubscription = true,
                RemainingTraffic = activeSubscription.RemainingTraffic,
                RemainingDays = (activeSubscription.EndDate - DateTime.Now).Days,
                ExpirationDate = activeSubscription.EndDate
            };
        }
    }
}