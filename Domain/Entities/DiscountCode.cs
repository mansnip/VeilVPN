using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class DiscountCode
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required(ErrorMessage = "وارد کردن کد تخفیف الزامی است.")]
        [MaxLength(50, ErrorMessage = "کد تخفیف نمی‌تواند بیشتر از ۵۰ کاراکتر باشد.")]
        public string Code { get; set; } // کد منحصر به فردی که کاربر وارد می‌کند (مثلا C253T)

        [Required(ErrorMessage = "وارد کردن درصد تخفیف الزامی است.")]
        [Range(1, 100, ErrorMessage = "درصد تخفیف باید بین ۱ تا ۱۰۰ باشد.")]
        public int DiscountPercent { get; set; } // درصد تخفیف (مثلا 20)

        [Required(ErrorMessage = "وارد کردن حداکثر تعداد استفاده الزامی است.")]
        [Range(1, int.MaxValue, ErrorMessage = "حداکثر تعداد استفاده باید حداقل ۱ باشد.")]
        public int MaxUses { get; set; } // حداکثر تعداد دفعات قابل استفاده

        public int CurrentUses { get; set; } = 0; // تعداد دفعاتی که تاکنون استفاده شده

        public DateTime? ExpiryDate { get; set; } // تاریخ انقضا (اختیاری، اگر null باشد یعنی نامحدود)

        public bool IsActive { get; set; } = true; // برای فعال/غیرفعال کردن کد

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation Property (اختیاری ولی مفید)
        // public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    }
}
