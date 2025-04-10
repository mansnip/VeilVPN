using Application.Services.Interfaces;
using Domain.DTOs;
using Domain.Entities;
using Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Implimentation
{
    public class DiscountService : IDiscountService
    {
        private readonly IDiscountCodeRepository _discountCodeRepository;
        private readonly IInvoiceRepository _invoiceRepository; // برای بررسی فاکتور

        public DiscountService(IDiscountCodeRepository discountCodeRepository, IInvoiceRepository invoiceRepository)
        {
            _discountCodeRepository = discountCodeRepository;
            _invoiceRepository = invoiceRepository;
        }

        public async Task<ResultDto<DiscountCode>> ValidateDiscountCodeAsync(string code, string userId, string invoiceId)
        {
            var discountCode = await _discountCodeRepository.GetByCodeAsync(code);

            if (discountCode == null)
            {
                return ResultDto<DiscountCode>.Fail("کد تخفیف وارد شده نامعتبر است.");
            }

            if (!discountCode.IsActive)
            {
                return ResultDto<DiscountCode>.Fail("این کد تخفیف غیرفعال است.");
            }

            if (discountCode.ExpiryDate.HasValue && discountCode.ExpiryDate.Value < DateTime.Now)
            {
                return ResultDto<DiscountCode>.Fail("این کد تخفیف منقضی شده است.");
            }

            if (discountCode.CurrentUses >= discountCode.MaxUses)
            {
                return ResultDto<DiscountCode>.Fail("ظرفیت استفاده از این کد تخفیف به اتمام رسیده است.");
            }

            // بررسی اینکه آیا فاکتور وجود دارد و متعلق به کاربر است و هنوز پرداخت نشده
            var invoice = await _invoiceRepository.GetByIdAsync(invoiceId); // فرض وجود این متد
            if (invoice == null || invoice.UserId != userId)
            {
                return ResultDto<DiscountCode>.Fail("فاکتور یافت نشد یا متعلق به شما نیست.");
            }

            if (invoice.Status != "در انتظار پرداخت")
            {
                return ResultDto<DiscountCode>.Fail("کد تخفیف فقط روی فاکتورهای 'در انتظار پرداخت' قابل اعمال است.");
            }

            if (!string.IsNullOrEmpty(invoice.AppliedDiscountCodeId))
            {
                return ResultDto<DiscountCode>.Fail("روی این فاکتور قبلاً کد تخفیف دیگری اعمال شده است.");
            }

            // اگر همه چیز اوکی بود، کد معتبر است
            return ResultDto<DiscountCode>.Ok(discountCode, "کد تخفیف معتبر است.");
        }

        public async Task MarkCodeAsUsedAsync(DiscountCode discountCode)
        {
            // این متد فقط تعداد استفاده را زیاد می‌کند و ذخیره می‌کند
            // بهتر است این کار همراه با آپدیت فاکتور در یک تراکنش انجام شود
            discountCode.CurrentUses++;
            await _discountCodeRepository.UpdateAsync(discountCode);
        }
    }
}
