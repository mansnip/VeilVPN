using Domain.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.DTOs.Expense
{
    // ViewModel برای فرم ایجاد و ویرایش
    public class CreateEditExpenseViewModel
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required(ErrorMessage = "وارد کردن توضیحات هزینه الزامی است.")]
        [MaxLength(500, ErrorMessage = "توضیحات نمی‌تواند بیشتر از 500 کاراکتر باشد.")]
        [Display(Name = "توضیحات هزینه")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "وارد کردن مبلغ هزینه الزامی است.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "مبلغ باید بیشتر از صفر باشد.")]
        [Display(Name = "مبلغ (ریال/تومان)")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "وارد کردن تاریخ هزینه الزامی است.")]
        [DataType(DataType.Date)]
        [Display(Name = "تاریخ هزینه")]
        public DateTime ExpenseDate { get; set; } = DateTime.Today; // مقدار پیش‌فرض مناسب

        [Required(ErrorMessage = "انتخاب دسته‌بندی الزامی است.")]
        [MaxLength(100)]
        [Display(Name = "دسته‌بندی")]
        public string Category { get; set; } = string.Empty;

        [Required(ErrorMessage = "انتخاب تناوب پرداخت الزامی است.")]
        [Display(Name = "تناوب پرداخت")]
        public ExpenseFrequency Frequency { get; set; } = ExpenseFrequency.OneTime;

        [MaxLength(1000, ErrorMessage = "یادداشت نمی‌تواند بیشتر از 1000 کاراکتر باشد.")]
        [Display(Name = "یادداشت (اختیاری)")]
        public string? Notes { get; set; }

        // می‌توانید لیست‌های لازم برای Dropdown ها را هم اینجا اضافه کنید،
        // مثلاً لیستی از دسته‌بندی‌های موجود اگر آن‌ها را داینامیک کنید.
        // public SelectList AvailableFrequencies { get; set; }
    }
}
