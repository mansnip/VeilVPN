using Application.Services.Interfaces;
using DataLayer.Context;
using Domain.DTOs;
using Domain.Entities;
using Domain.Interfaces;
using Domain.ViewModels.Admin;
using Domain.ViewModels.UserPanel;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Implimentation
{
    public class InvoiceService : IInvoiceService
    {
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IUserRepository _userRepository;
        private readonly VeilVpnDbContext _context; // تزریق DbContext برای تراکنش
        private readonly IDiscountService _discountService; // تزریق سرویس تخفیف

        public InvoiceService(IInvoiceRepository invoiceRepository, IUserRepository userRepository, IDiscountService discountService, VeilVpnDbContext context)
        {
            _invoiceRepository = invoiceRepository ?? throw new ArgumentNullException(nameof(invoiceRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _discountService = discountService;
            _context = context;
        }

        // سازنده و سایر متدها...

        public async Task<List<AdminInvoiceListViewModel>> GetAllInvoicesForAdminAsync()
        {
            var invoices = await _invoiceRepository.GetAllInvoicesWithUserAsync();

            if (invoices == null)
                return new List<AdminInvoiceListViewModel>();

            return invoices.Select(invoice => new AdminInvoiceListViewModel
            {
                Id = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                UserEmail = invoice.User?.Email ?? "کاربر حذف شده", // نمایش ایمیل کاربر و مدیریت حالت null
                CreatedDate = invoice.CreatedDate,
                FinalPrice = invoice.FinalPrice,
                Status = invoice.Status,
                IsRenewal = invoice.IsRenewal,
                RemarkName = invoice.RemarkName
            }).ToList();
        }


        public async Task<InvoiceViewModel> CreateInvoiceAsync(string userId, int traffic, int duration, SubscriptionPriceDetails priceDetails, string remarkName, string renewalId = null)
        {
            // ایجاد شماره فاکتور منحصر به فرد
            string invoiceNumber = GenerateInvoiceNumber();

            // بررسی وضعیت تمدید
            bool isRenewal = !string.IsNullOrEmpty(renewalId);

            // ایجاد فاکتور جدید
            var invoice = new Invoice
            {
                InvoiceNumber = invoiceNumber,
                UserId = userId,
                CreatedDate = DateTime.Now,
                Traffic = traffic,
                Duration = duration,
                BasePrice = priceDetails.BasePrice,
                PlanDiscountPercent = (int)priceDetails.DiscountPercent,
                PlanDiscountAmount = priceDetails.DiscountAmount,
                RemarkName = remarkName,
                Status = "در انتظار پرداخت", // تنظیم وضعیت پیش‌فرض
                IsRenewal = isRenewal,
                RenewalSubscriptionId = renewalId
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
                RemarkName = remarkName,
                IsRenewal = isRenewal,
                RenewalSubscriptionId = renewalId,
                Subscription = new SubscriptionDetails
                {
                    Traffic = traffic,
                    Duration = duration,
                    BasePrice = priceDetails.BasePrice,
                    PlanDiscountPercent = priceDetails.DiscountPercent,
                    PlanDiscountAmount = priceDetails.DiscountAmount,
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

            // محاسبه FinalPrice اینجا انجام شود
            decimal finalAmount = invoice.BasePrice - invoice.PlanDiscountAmount - invoice.CouponDiscountAmount;


            return new InvoiceViewModel
            {
                RemarkName = invoice.RemarkName,
                Id = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                InvoiceDate = invoice.CreatedDate,
                PaymentStatus = invoice.Status,
                UserId = invoice.UserId,
                UserFullName = "", // این رو کامل کن اگر نیاز داری
                UserEmail = user?.Email,
                UserPhone = user?.PhoneNumber,
                PaymentToken = invoice.PaymentToken,
                IsComplate = invoice.IsComplate,
                IsRenewal = invoice.IsRenewal, // اضافه کردن مقادیر تمدید
                RenewalSubscriptionId = invoice.RenewalSubscriptionId,
                // RenewalSubscriptionName = ... // این رو باید از اشتراک اصلی بخوانی اگر لازمه

                // اطلاعات کد تخفیف
                AppliedDiscountCode = invoice.AppliedDiscountCode,
                CouponDiscountAmount = invoice.CouponDiscountAmount,
                CouponDiscountPercent = invoice.CouponDiscountPercent,

                // مقدار TotalAmount را با قیمت نهایی محاسبه شده پر کن
                TotalAmount = finalAmount,

                Subscription = new SubscriptionDetails // دقت کن که کلاس داخلی است
                {
                    Traffic = invoice.Traffic,
                    Duration = invoice.Duration,
                    BasePrice = invoice.BasePrice,
                    PlanDiscountPercent = invoice.PlanDiscountPercent, // نام جدید
                    PlanDiscountAmount = invoice.PlanDiscountAmount, // نام جدید
                                                                     // FinalPrice اینجا دیگر معنی ندارد
                }
                // Status را حذف کن چون PaymentStatus هست
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
                        PlanDiscountPercent = invoice.PlanDiscountPercent,
                        PlanDiscountAmount = invoice.PlanDiscountAmount,
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

        public async Task<Invoice> GetOrginalInvoiceById(string id)
        {
            return await _invoiceRepository.GetByIdAsync(id);
        }

        public async Task UpdateInvoice(Invoice invoice)
        {
            await _invoiceRepository.UpdateAsync(invoice);
        }

        // --- متد جدید برای اعمال کد تخفیف ---
        public async Task<ResultDto> ApplyDiscountCodeAsync(string invoiceId, string code, string userId)
        {
            // شروع تراکنش
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. اعتبارسنجی کد تخفیف
                var validationResult = await _discountService.ValidateDiscountCodeAsync(code, userId, invoiceId);
                if (!validationResult.IsSuccess)
                {
                    return ResultDto.Fail(validationResult.Message);
                }
                var discountCode = validationResult.Data;

                // 2. گرفتن فاکتور اصلی از دیتابیس
                var invoice = await _invoiceRepository.GetByIdAsync(invoiceId); // یا GetOrginalInvoiceById
                if (invoice == null) // بررسی اضافی، هرچند در Validate هم چک شد
                {
                    return ResultDto.Fail("فاکتور یافت نشد.");
                }

                // اطمینان از اینکه invoice از نوع Invoice است نه InvoiceViewModel
                var originalInvoice = await _invoiceRepository.GetByIdAsync(invoiceId);
                if (originalInvoice == null)
                {
                    return ResultDto.Fail("خطا در بازیابی اطلاعات اصلی فاکتور.");
                }


                // 3. محاسبه مبلغ تخفیف کوپن
                // تخفیف روی قیمت پایه اعمال می‌شود
                originalInvoice.CouponDiscountPercent = discountCode.DiscountPercent;
                originalInvoice.CouponDiscountAmount = Math.Round(originalInvoice.BasePrice * (discountCode.DiscountPercent / 100.0m), 0); // گرد کردن به عدد صحیح

                // 4. ذخیره اطلاعات کد تخفیف در فاکتور
                originalInvoice.AppliedDiscountCodeId = discountCode.Id;
                originalInvoice.AppliedDiscountCode = discountCode.Code;

                // 5. به‌روزرسانی فاکتور در دیتابیس
                // FinalPrice به طور خودکار در پراپرتی get محاسبه می‌شود. نیازی به ست کردن دستی نیست.
                await _invoiceRepository.UpdateAsync(originalInvoice); // یا _context.Invoices.Update(invoice);

                // 6. افزایش تعداد استفاده از کد تخفیف
                // به جای فراخوانی MarkCodeAsUsedAsync، مستقیما اینجا انجام می‌دهیم تا در همان تراکنش باشد
                discountCode.CurrentUses++;
                _context.DiscountCodes.Update(discountCode); // استفاده از context برای انجام در تراکنش


                // 7. ذخیره تمام تغییرات در تراکنش
                await _context.SaveChangesAsync();

                // 8. تایید تراکنش
                await transaction.CommitAsync();

                return ResultDto.Ok("کد تخفیف با موفقیت روی فاکتور شما اعمال شد.");
            }
            catch (Exception ex)
            {
                // لاگ کردن خطا (ex)
                await transaction.RollbackAsync(); // بازگردانی تغییرات در صورت خطا
                return ResultDto.Fail("خطایی در هنگام اعمال کد تخفیف رخ داد. لطفاً دوباره تلاش کنید یا با پشتیبانی تماس بگیرید.");
            }
        }
    }

}
