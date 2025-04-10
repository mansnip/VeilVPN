using System.ComponentModel.DataAnnotations;

namespace Domain.ViewModels.Admin
{
    public class DiscountCodeViewModel
    {
        public string? Id { get; set; } // برای ویرایش و حذف لازم است

        [Required(ErrorMessage = "وارد کردن کد تخفیف الزامی است.")]
        [MaxLength(50, ErrorMessage = "کد تخفیف نمی‌تواند بیشتر از ۵۰ کاراکتر باشد.")]
        [RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "کد تخفیف فقط می‌تواند شامل حروف انگلیسی و اعداد باشد.")] // الگوی پیشنهادی برای جلوگیری از کاراکترهای خاص
        [Display(Name = "کد تخفیف")]
        public string Code { get; set; }

        [Required(ErrorMessage = "وارد کردن درصد تخفیف الزامی است.")]
        [Range(1, 100, ErrorMessage = "درصد تخفیف باید بین ۱ تا ۱۰۰ باشد.")]
        [Display(Name = "درصد تخفیف")]
        public int DiscountPercent { get; set; }

        [Required(ErrorMessage = "وارد کردن حداکثر تعداد استفاده الزامی است.")]
        [Range(1, int.MaxValue, ErrorMessage = "حداکثر تعداد استفاده باید حداقل ۱ باشد.")]
        [Display(Name = "حداکثر تعداد استفاده")]
        public int MaxUses { get; set; }

        [Display(Name = "تعداد استفاده شده")]
        public int CurrentUses { get; set; } // فقط برای نمایش در لیست و ویرایش

        [Display(Name = "تاریخ انقضا (اختیاری)")]
        [DataType(DataType.Date)] // کمک به مدل بایندر و اعتبارسنجی نوع
        public DateTime? ExpiryDate { get; set; }

        [Display(Name = "وضعیت فعال بودن")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "تاریخ ایجاد")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
