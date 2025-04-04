using Domain.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.DTOs.Expense
{
    public class ExpenseViewModel
    {
        public string Id { get; set; }
        [Display(Name = "توضیحات")]
        public string Description { get; set; } = string.Empty;
        [Display(Name = "مبلغ")]
        public decimal Amount { get; set; }
        [Display(Name = "تاریخ")]
        public DateTime ExpenseDate { get; set; }
        [Display(Name = "تاریخ فرمت شده")]
        public string FormattedExpenseDate => ExpenseDate.ToString("yyyy/MM/dd"); // برای نمایش بهتر
        [Display(Name = "دسته‌بندی")]
        public string Category { get; set; } = string.Empty;
        [Display(Name = "تناوب")]
        public ExpenseFrequency Frequency { get; set; }
        [Display(Name = "تناوب")]
        public string FrequencyDisplayName => Frequency.GetDisplayName(); // نیاز به یک Extension Method برای Enum دارید
        [Display(Name = "یادداشت")]
        public string? Notes { get; set; }
    }


}
