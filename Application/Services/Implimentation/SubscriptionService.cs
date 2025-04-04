using Application.API;
using Application.Generators;
using Application.Services.Interfaces;
using Domain.DTOs.VPN;
using Domain.Entities;
using Domain.Interfaces;
using Domain.ViewModels.UserPanel;
using Microsoft.Extensions.Logging;
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
        private readonly ApiManager _apiManager;
        private readonly IServerVPNService _serverVPNService;
        private readonly ILogger<SubscriptionService> _logger;

        // قیمت هر گیگابایت (تومان)
        private const int PricePerGB = 2500;
        // قیمت هر روز (تومان)
        private const int PricePerDay = 1500;

        public SubscriptionService(
            IUserRepository userRepository,
            IInvoiceRepository invoiceRepository,
            ISubscriptionRepository subscriptionRepository,
            ApiManager apiManager,
            IServerVPNService serverVPNService,
            ILogger<SubscriptionService> logger)

        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _invoiceRepository = invoiceRepository ?? throw new ArgumentNullException(nameof(invoiceRepository));
            _subscriptionRepository = subscriptionRepository ?? throw new ArgumentNullException(nameof(subscriptionRepository));
            _apiManager = apiManager ?? throw new ArgumentNullException(nameof(apiManager));
            _serverVPNService = serverVPNService ?? throw new ArgumentNullException(nameof(serverVPNService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }


        public async Task<SubscriptionnViewModel> GetUserActiveSubscriptionAsync(string userId)
        {
            // دریافت اشتراک فعال کاربر
            var subscription = await _subscriptionRepository.GetActiveSubscriptionByUserIdAsync(userId);

            if (subscription == null)
                return null;

            // تبدیل به ViewModel
            return new SubscriptionnViewModel
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

        public async Task<(bool success, string Message)> CreateSubscriptionFromInvoiceAsync(string invoiceId)
        {
            try
            {
                // دریافت فاکتور از دیتابیس
                var invoice = await _invoiceRepository.GetByIdAsync(invoiceId);

                if (invoice == null)
                    return (false, "فاکتور مورد نظر یافت نشد");

                if (invoice.Status != "پرداخت شده")
                    return (false, "وضعیت فاکتور برای ایجاد اشتراک مناسب نیست");

                // گرفتن کاربر
                var user = await _userRepository.GetUserById(invoice.UserId);
                if (user == null)
                    return (false, "کاربر مورد نظر یافت نشد");

                // بررسی آیا عملیات تمدید است یا خرید جدید
                bool isRenewal = !string.IsNullOrEmpty(invoice.RenewalSubscriptionId);
                _logger?.LogInformation($"شروع {(isRenewal ? "تمدید" : "ایجاد")} اشتراک برای کاربر {user.Email} با شناسه فاکتور {invoiceId}");

                if (isRenewal)
                {
                    // دریافت اشتراک قبلی برای تمدید
                    var existingSubscription = await _subscriptionRepository.GetByIdAsync(invoice.RenewalSubscriptionId);
                    if (existingSubscription == null)
                        return (false, "اشتراک مورد نظر برای تمدید یافت نشد");

                    // دریافت سرور مربوط به اشتراک قبلی
                    var server = await _serverVPNService.GetServerByIdAsync(existingSubscription.VpnServerID);
                    if (server == null)
                        return (false, "سرور مربوط به اشتراک یافت نشد");

                    // ریست کردن ترافیک کاربر قبل از تمدید
                    var resetResult = await _apiManager.ResetClientTraffic(server, existingSubscription.VpnEmailName);
                    if (!resetResult.Success)
                    {
                        _logger?.LogWarning($"خطا در ریست ترافیک کاربر: {resetResult.Message}");
                        // ادامه می‌دهیم حتی اگر ریست ترافیک با خطا مواجه شد
                    }
                    else
                    {
                        _logger?.LogInformation("ترافیک کاربر با موفقیت ریست شد");
                    }

                    // مقادیر جدید برای تمدید (جایگزین کردن کامل مقادیر قبلی، نه اضافه کردن)
                    int newDuration = invoice.Duration;
                    double newTraffic = invoice.Traffic;

                    // به‌روزرسانی اشتراک در سرور VPN
                    var updateResult = await _apiManager.UpdateClient(
                        server,
                        existingSubscription.VpnId,
                        existingSubscription.VpnEmailName,
                        newTraffic,      // مقدار جدید ترافیک
                        newDuration,     // مقدار جدید مدت زمان
                        true,
                        0,
                        existingSubscription.Id);

                    if (!updateResult.Success)
                        return (false, $"خطا در به‌روزرسانی اشتراک در سرور: {updateResult.Message}");

                    // به‌روزرسانی اطلاعات اشتراک در دیتابیس - جایگزینی کامل مقادیر
                    existingSubscription.Traffic = (int)newTraffic;
                    existingSubscription.RemainingTraffic = (int)newTraffic; // ریست کردن ترافیک باقیمانده
                    existingSubscription.Duration = newDuration;

                    // تنظیم تاریخ شروع از امروز
                    existingSubscription.StartDate = DateTime.Now;
                    existingSubscription.EndDate = DateTime.Now.AddDays(newDuration);

                    existingSubscription.IsActive = true;
                    existingSubscription.InvoiceId = invoice.Id;
                    existingSubscription.RemarkName = invoice.RemarkName;

                    // ذخیره تغییرات اشتراک
                    await _subscriptionRepository.UpdateAsync(existingSubscription);

                    return (true, $"اشتراک با موفقیت تمدید شد و تا تاریخ {existingSubscription.EndDate.ToShortDateString()} معتبر است.");
                }
                else
                {
                    // گرفتن لیست سرور ها 
                    var servers = await _serverVPNService.GetAllServersAsync();
                    if (servers == null || !servers.Any())
                        return (false, "هیچ سروری برای ایجاد اشتراک یافت نشد");

                    // انتخاب سروری که کمترین کاربر را دارد
                    var server = servers.OrderBy(s => s.CurrentUsers).First();

                    // ساخت نام اشتراک که تکراری هم نباشد
                    string email = UniqueRandomStringGenerator.GenerateUniqueRandomString();

                    // ساخت آیدی اشتراک کاربر
                    string vpnId = Guid.NewGuid().ToString();

                    // ایجاد اشتراک در سرور 
                    var result = await _apiManager.AddClient(
                        server,
                        email,
                        vpnId,
                        invoice.Traffic,
                        invoice.Duration,
                        true,
                        0);

                    // اگر افزودن کاربر به سرور موفقیت آمیز نبود
                    if (!result.Success)
                        return (false, result.Message);

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
                        InvoiceId = invoice.Id,
                        VpnServerID = server.Id,
                        VpnEmailName = email,
                        RemarkName = invoice.RemarkName,
                        VpnId = vpnId,
                    };

                    // ذخیره اشتراک در دیتابیس
                    await _subscriptionRepository.CreateAsync(subscription);
                    server.CurrentUsers++;
                    await _serverVPNService.UpdateServerAsync(server);

                    return (true, "اشتراک جدید با موفقیت ساخته و فعال شده است.");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "خطا در ایجاد/تمدید اشتراک");
                return (false, $"خطا در سیستم: {ex.Message}");
            }
        }


        // متد کمکی برای گرفتن اشتراک با کاربر (نمونه - ممکن است در ریپازیتوری شما متفاوت باشد)
        private async Task<Subscription?> GetSubscriptionWithUserAsync(string subscriptionId)
        {
            // این متد باید اشتراک را به همراه اطلاعات کاربر (User) برگرداند اگر نیاز دارید
            // return await _dbContext.Subscriptions.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == subscriptionId);
            // فعلا فقط اشتراک را می‌گیریم چون ViewModel اطلاعات کاربر را مستقیم نیاز ندارد
            return await _subscriptionRepository.GetByIdAsync(subscriptionId);
        }


        public async Task<SubscriptionViewModel?> GetSubscriptionDetailsForAdminAsync(string subscriptionId)
        {
            // 1. دریافت اشتراک از دیتابیس (شامل اطلاعات کاربر و سرور اگر لازم است)
            var subscription = await GetSubscriptionWithUserAsync(subscriptionId); // یا متد مناسب دیگر
            if (subscription == null || string.IsNullOrEmpty(subscription.VpnServerID) || string.IsNullOrEmpty(subscription.VpnEmailName))
            {
                _logger.LogWarning($"Subscription not found or lacks VPN info for ID: {subscriptionId}");
                return null; // اشتراک یافت نشد یا اطلاعات VPN ناقص است
            }

            // 2. ساخت ViewModel اولیه با اطلاعات دیتابیس
            var viewModel = new SubscriptionViewModel
            {
                Id = subscription.Id,
                Traffic = subscription.Traffic,
                Duration = subscription.Duration,
                // RemainingTraffic = subscription.RemainingTraffic, // این را با مقدار زنده جایگزین می‌کنیم
                StartDate = subscription.StartDate,
                EndDate = subscription.EndDate,
                IsActive = subscription.IsActive,
                DaysRemaining = (subscription.EndDate - DateTime.Now).Days > 0 ? (subscription.EndDate - DateTime.Now).Days : 0,
                // PercentTrafficUsed = ... // این را با مقدار زنده جایگزین می‌کنیم
                VpnEmailName = subscription.VpnEmailName,
                RemarkName = subscription.RemarkName,
                VpnId = subscription.VpnId,
                // VpnServerName = "درحال بارگذاری...", // مقدار اولیه
                HasVpnConnection = true // فرض اولیه، در صورت خطا false می‌شود
            };

            // 3. دریافت اطلاعات سرور
            var server = await _serverVPNService.GetServerByIdAsync(subscription.VpnServerID);
            if (server == null)
            {
                _logger.LogWarning($"Server VPN not found for ID: {subscription.VpnServerID}");
                viewModel.HasVpnConnection = false;
                viewModel.VpnServerName = "سرور نامعتبر";
                return viewModel; // سرور یافت نشد، ViewModel ناقص را برمی‌گردانیم
            }
            viewModel.VpnServerName = server.Name;
            viewModel.VpnServerUrl = server.ApiUrl; // برای ساخت لینک اتصال نیاز است

            // 4. دریافت اطلاعات زنده از سرور VPN
            try
            {
                var inboundsResponse = await _apiManager.GetInbounds(server);
                if (!inboundsResponse.Success || inboundsResponse.Data?.Inbounds == null)
                {
                    _logger.LogWarning($"Error fetching inbounds from server {server.Name}: {inboundsResponse.Message}");
                    viewModel.HasVpnConnection = false; // نمی‌توان اطلاعات زنده را گرفت
                    return viewModel;
                }

                // 5. جستجوی کلاینت در پاسخ API
                ClientStat? clientStat = null;
                foreach (var inbound in inboundsResponse.Data.Inbounds)
                {
                    clientStat = inbound.ClientStats?
                        .FirstOrDefault(c => c.Email?.Equals(subscription.VpnEmailName, StringComparison.OrdinalIgnoreCase) == true);

                    if (clientStat != null)
                    {
                        viewModel.Port = inbound.Port; // پورت را از inbound می‌گیریم
                        break;
                    }
                }

                // 6. به‌روزرسانی ViewModel با اطلاعات زنده
                if (clientStat != null)
                {
                    viewModel.IsVpnActive = clientStat.IsActive;
                    viewModel.VpnRemainingTraffic = clientStat.RemainingGigabytes; // <<< مقدار زنده ترافیک
                    viewModel.VpnRemainingDays = clientStat.RemainingDays;       // <<< مقدار زنده روزها
                    viewModel.VpnUsagePercentage = clientStat.UsagePercentage;   // <<< درصد مصرف زنده
                    // viewModel.SubscriptionLink از پراپرتی‌های بالا برای ساخت لینک استفاده می‌کند
                }
                else
                {
                    _logger.LogWarning($"Client with email {subscription.VpnEmailName} not found on server {server.Name}.");
                    viewModel.HasVpnConnection = false; // کلاینت در سرور یافت نشد
                    viewModel.IsVpnActive = false;
                    viewModel.VpnRemainingTraffic = 0; // یا مقدار پیش‌فرض دیگر
                    viewModel.VpnRemainingDays = 0;
                    viewModel.VpnUsagePercentage = 100; // یا 0؟ بستگی به نمایش دارد
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error communicating with VPN server {server.Name} for subscription {subscriptionId}.");
                viewModel.HasVpnConnection = false;
                viewModel.IsVpnActive = false;
                // مقادیر پیش‌فرض یا خطا را در ViewModel تنظیم کنید
            }

            return viewModel;
        }



        // دریافت اشتراک‌های کاربر
        public async Task<List<SubscriptionViewModel>> GetUserSubscriptionsAsync(string userId)
        {
            // دریافت اشتراک‌های کاربر از دیتابیس
            var subscriptions = await _subscriptionRepository.GetUserSubscriptionsAsync(userId);

            if (subscriptions == null || !subscriptions.Any())
                return new List<SubscriptionViewModel>();

            // ایجاد لیست ویومدل‌ها با اطلاعات پایه از دیتابیس
            var viewModels = subscriptions.Select(sub => new SubscriptionViewModel
            {
                Id = sub.Id,
                Traffic = sub.Traffic,
                Duration = sub.Duration,
                RemainingTraffic = sub.RemainingTraffic,
                StartDate = sub.StartDate,
                EndDate = sub.EndDate,
                IsActive = sub.IsActive,
                DaysRemaining = (sub.EndDate - DateTime.Now).Days > 0 ? (sub.EndDate - DateTime.Now).Days : 0,
                PercentTrafficUsed = (int)(100 - ((double)sub.RemainingTraffic / sub.Traffic) * 100),
                VpnEmailName = sub.VpnEmailName,
                HasVpnConnection = !string.IsNullOrEmpty(sub.VpnServerID) && !string.IsNullOrEmpty(sub.VpnEmailName),
            }).ToList();

            // گروه‌بندی اشتراک‌ها بر اساس سرور VPN برای کاهش تعداد درخواست‌ها
            var subscriptionsByServer = subscriptions
                .Where(s => !string.IsNullOrEmpty(s.VpnServerID))
                .GroupBy(s => s.VpnServerID)
                .ToDictionary(g => g.Key, g => g.ToList());

            // برای هر سرور VPN، اطلاعات را دریافت می‌کنیم
            foreach (var serverGroup in subscriptionsByServer)
            {
                var serverId = serverGroup.Key;
                var serverSubscriptions = serverGroup.Value;

                // دریافت اطلاعات سرور
                var server = await _serverVPNService.GetServerByIdAsync(serverId);
                if (server == null)
                    continue;

                try
                {
                    // دریافت اطلاعات inbound‌ها از سرور VPN
                    var inboundsResponse = await _apiManager.GetInbounds(server);

                    if (!inboundsResponse.Success || inboundsResponse.Data?.Inbounds == null)
                    {
                        _logger.LogWarning($"خطا در دریافت اطلاعات Inbound از سرور {server.Name}: {inboundsResponse.Message}");
                        continue;
                    }

                    // پردازش اطلاعات کلاینت‌ها برای هر اشتراک در این سرور
                    foreach (var subscription in serverSubscriptions)
                    {
                        // یافتن ویومدل مرتبط
                        var viewModel = viewModels.FirstOrDefault(vm => vm.Id == subscription.Id);
                        if (viewModel == null)
                            continue;

                        viewModel.VpnServerName = server.Name;

                        // جستجوی کلاینت مرتبط در تمام inbound‌ها
                        ClientStat clientStat = null;
                        foreach (var inbound in inboundsResponse.Data.Inbounds)
                        {
                            clientStat = inbound.ClientStats?
                                .FirstOrDefault(c => c.Email?.Equals(subscription.VpnEmailName, StringComparison.OrdinalIgnoreCase) == true);

                            if (clientStat != null)
                            {
                                viewModel.Port = inbound.Port;
                                break;
                            }
                        }

                        // اگر کلاینت پیدا شد، اطلاعات را به روزرسانی می‌کنیم
                        if (clientStat != null)
                        {
                            viewModel.IsVpnActive = clientStat.IsActive;
                            viewModel.VpnRemainingTraffic = clientStat.RemainingGigabytes;
                            viewModel.VpnRemainingDays = clientStat.RemainingDays;
                            viewModel.VpnUsagePercentage = clientStat.UsagePercentage;
                            viewModel.RemarkName = subscription.RemarkName;
                            viewModel.VpnServerUrl = server.ApiUrl;
                            viewModel.VpnId = subscription.VpnId;
                            
                        }
                        else
                        {
                            // اگر کلاینت در سرور پیدا نشد
                            viewModel.HasVpnConnection = false;
                            _logger.LogWarning($"کلاینت با ایمیل {subscription.VpnEmailName} در سرور {server.Name} یافت نشد");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"خطا در دریافت اطلاعات از سرور VPN {server.Name}");
                }
            }

            return viewModels;
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

        public async Task<Subscription> GetSubscriptionById(string subscriptionId)
        {
            return await _subscriptionRepository.GetSubscriptionById(subscriptionId);
        }
    }
}