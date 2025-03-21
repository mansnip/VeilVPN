using Application.Services.Interfaces;
using Domain.Entities;
using Domain.Interfaces;
using Domain.ViewModels.UserPanel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Implimentation
{
    public class InvoiceService : IInvoiceService
    {
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IUserRepository _userRepository;

        public InvoiceService(IInvoiceRepository invoiceRepository, IUserRepository userRepository)
        {
            _invoiceRepository = invoiceRepository ?? throw new ArgumentNullException(nameof(invoiceRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        }
        
        // سازنده و سایر متدها...

        public async Task<InvoiceViewModel> CreateInvoiceAsync(string userId, int traffic, int duration, SubscriptionPriceDetails priceDetails)
        {
            // ایجاد شماره فاکتور منحصر به فرد
            string invoiceNumber = GenerateInvoiceNumber();

            // ایجاد فاکتور جدید
            var invoice = new Invoice
            {
                InvoiceNumber = invoiceNumber,
                UserId = userId,
                CreatedDate = DateTime.Now,
                Traffic = traffic,
                Duration = duration,
                BasePrice = priceDetails.BasePrice,
                DiscountPercent = (int)priceDetails.DiscountPercent,
                DiscountAmount = priceDetails.DiscountAmount,
                FinalPrice = priceDetails.FinalPrice,
                Status = "در انتظار پرداخت" // تنظیم وضعیت پیش‌فرض
            };

            // ذخیره فاکتور در دیتابیس
            await _invoiceRepository.CreateAsync(invoice);

            // بازگرداندن مدل مناسب برای نمایش
            return new InvoiceViewModel
            {
                Id = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                InvoiceDate = invoice.CreatedDate,
                PaymentStatus = invoice.Status,
                Subscription = new SubscriptionDetails
                {
                    Traffic = traffic,
                    Duration = duration,
                    BasePrice = priceDetails.BasePrice,
                    DiscountPercent = priceDetails.DiscountPercent,
                    DiscountAmount = priceDetails.DiscountAmount,
                    FinalPrice = priceDetails.FinalPrice
                }
            };
        }

        public async Task<InvoiceViewModel> GetByIdAsync(string id)
        {
            var invoice = await _invoiceRepository.GetByIdAsync(id);
            if (invoice == null)
                return null;

            var user = await _userRepository.GetUserById(invoice.UserId);

            return new InvoiceViewModel
            {
                Id = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                InvoiceDate = invoice.CreatedDate,
                PaymentStatus = invoice.Status,
                UserId = invoice.UserId,
                UserFullName = "",
                UserEmail = user?.Email,
                UserPhone = user?.PhoneNumber,
                Subscription = new SubscriptionDetails
                {
                    Traffic = invoice.Traffic,
                    Duration = invoice.Duration,
                    BasePrice = invoice.BasePrice,
                    DiscountPercent = invoice.DiscountPercent,
                    DiscountAmount = invoice.DiscountAmount,
                    FinalPrice = invoice.FinalPrice
                }
            };
        }

        public async Task<List<InvoiceViewModel>> GetUserInvoicesAsync(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                    throw new ArgumentNullException(nameof(userId));

                var invoices = await _invoiceRepository.GetUserInvoicesAsync(userId);
                if (invoices == null)
                    return new List<InvoiceViewModel>();

                var user = await _userRepository.GetUserById(userId);

                return invoices.Select(invoice => new InvoiceViewModel
                {
                    Id = invoice.Id,
                    InvoiceNumber = invoice.InvoiceNumber,
                    InvoiceDate = invoice.CreatedDate,
                    PaymentStatus = invoice.Status,
                    UserId = invoice.UserId,
                    UserFullName = "",
                    UserEmail = user?.Email,
                    UserPhone = user?.PhoneNumber,
                    Subscription = new SubscriptionDetails
                    {
                        Traffic = invoice.Traffic,
                        Duration = invoice.Duration,
                        BasePrice = invoice.BasePrice,
                        DiscountPercent = invoice.DiscountPercent,
                        DiscountAmount = invoice.DiscountAmount,
                        FinalPrice = invoice.FinalPrice
                    }
                }).ToList();
            }
            catch (Exception ex)
            {
                // لاگ کردن خطا
                throw;
            }
        }

        public async Task<bool> UpdateInvoiceStatusAsync(string invoiceId, string newStatus)
        {
            var invoice = await _invoiceRepository.GetByIdAsync(invoiceId);

            if (invoice == null)
                return false;

            invoice.Status = newStatus;

            // در صورت پرداخت شدن، تاریخ پرداخت را نیز ثبت می‌کنیم
            if (newStatus == "پرداخت شده")
            {
                invoice.PaidDate = DateTime.Now;
            }

            return await _invoiceRepository.UpdateAsync(invoice);
        }

        private string GenerateInvoiceNumber()
        {
            // ایجاد یک شماره فاکتور منحصر به فرد مثلاً با ترکیبی از تاریخ و یک عدد تصادفی
            return $"INV-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
        }


        // اضافه کردن متد برای دریافت اطلاعات آماری
        public async Task<InvoiceStatistics> GetUserInvoiceStatisticsAsync(string userId)
        {
            var invoices = await _invoiceRepository.GetUserInvoicesAsync(userId);

            if (invoices == null || !invoices.Any())
                return new InvoiceStatistics();

            return new InvoiceStatistics
            {
                TotalInvoices = invoices.Count,
                PendingInvoices = invoices.Count(i => i.Status == "در انتظار پرداخت"),
                PaidInvoices = invoices.Count(i => i.Status == "پرداخت شده"),
                CanceledInvoices = invoices.Count(i => i.Status == "لغو شده"),
                TotalSpent = invoices.Where(i => i.Status == "پرداخت شده").Sum(i => i.FinalPrice)
            };
        }
    }

}
