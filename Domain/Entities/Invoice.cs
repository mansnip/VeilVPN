using Domain.Entities.Account;

namespace Domain.Entities
{
    public class Invoice
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string InvoiceNumber { get; set; }
        public string UserId { get; set; }
        public DateTime CreatedDate { get; set; }
        public int Traffic { get; set; }
        public int Duration { get; set; }
        public decimal BasePrice { get; set; }
        public decimal PlanDiscountAmount { get; set; }
        public int PlanDiscountPercent { get; set; }
        public string? RemarkName { get; set; }
        public string? PaymentToken { get; set; } = Guid.NewGuid().ToString();
        public bool IsComplate { get; set; }


        // فیلدهای جدید برای تمدید
        public bool IsRenewal { get; set; }
        public string? RenewalSubscriptionId { get; set; }

        // --- فیلدهای جدید برای کد تخفیف ---
        public string? AppliedDiscountCodeId { get; set; } // شناسه کد تخفیف اعمال شده (Nullable Foreign Key)
        public string? AppliedDiscountCode { get; set; } // خود کد برای نمایش راحت‌تر (مثلا C253T)
        public decimal CouponDiscountAmount { get; set; } = 0; // مبلغ تخفیف حاصل از کوپن
        public int CouponDiscountPercent { get; set; } = 0; // درصد تخفیف کوپن اعمال شده

        // تغییر از IsPaid به Status
        public string Status { get; set; } = "در انتظار پرداخت"; // مقادیر: "در انتظار پرداخت"، "پرداخت شده"، "لغو شده"

        public DateTime? PaidDate { get; set; }
        public long? PaymentRefId { get; set; }

        // Navigation properties
        public virtual User User { get; set; }
        public virtual Subscription Subscription { get; set; }

        public decimal FinalPrice { get; set; } // <--- setter اضافه شد
        public void CalculateFinalPrice() // یک متد کمکی مثال
        {
            this.FinalPrice = this.BasePrice - this.PlanDiscountAmount - this.CouponDiscountAmount;
            // یا هر منطق محاسبه دیگری که دارید
        }
    }

}
