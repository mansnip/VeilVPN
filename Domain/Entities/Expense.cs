using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Expense
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required(ErrorMessage = "وارد کردن توضیحات هزینه الزامی است.")]
        [MaxLength(500, ErrorMessage = "توضیحات نمی‌تواند بیشتر از 500 کاراکتر باشد.")]
        [Display(Name = "توضیحات هزینه")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "وارد کردن مبلغ هزینه الزامی است.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "مبلغ باید بیشتر از صفر باشد.")]
        [Display(Name = "مبلغ (ریال/تومان)")] // واحد پول را مشخص کنید
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "وارد کردن تاریخ هزینه الزامی است.")]
        [DataType(DataType.Date)]
        [Display(Name = "تاریخ هزینه")]
        public DateTime ExpenseDate { get; set; } = DateTime.UtcNow;

        [Required(ErrorMessage = "انتخاب دسته‌بندی الزامی است.")]
        [MaxLength(100)]
        [Display(Name = "دسته‌بندی")]
        public string Category { get; set; } = string.Empty; // مثال: "هاست خارج", "سرور ایران", "ترافیک اضافه", "نرم‌افزار", "سایر"

        [Required(ErrorMessage = "انتخاب تناوب پرداخت الزامی است.")]
        [Display(Name = "تناوب پرداخت")]
        public ExpenseFrequency Frequency { get; set; } = ExpenseFrequency.OneTime;

        [MaxLength(1000, ErrorMessage = "یادداشت نمی‌تواند بیشتر از 1000 کاراکتر باشد.")]
        [Display(Name = "یادداشت (اختیاری)")]
        public string? Notes { get; set; }

        [Display(Name = "تاریخ ثبت")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "تاریخ آخرین ویرایش")]
        public DateTime? ModifiedDate { get; set; }
    }
    public enum ExpenseFrequency
    {
        [Display(Name = "یک‌باره")]
        OneTime,

        [Display(Name = "ماهیانه")]
        Monthly,

        [Display(Name = "سالیانه")]
        Yearly,

        [Display(Name = "دوره‌ای دیگر")] // برای موارد خاص
        Other
    }
}

