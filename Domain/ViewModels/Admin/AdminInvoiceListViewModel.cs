using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.ViewModels.Admin
{
    public class AdminInvoiceListViewModel
    {
        public string Id { get; set; }

        [Display(Name = "شماره فاکتور")]
        public string InvoiceNumber { get; set; }

        [Display(Name = "ایمیل کاربر")]
        public string UserEmail { get; set; } // نمایش ایمیل کاربر به جای ID

        [Display(Name = "تاریخ ایجاد")]
        public DateTime CreatedDate { get; set; }

        [Display(Name = "مبلغ نهایی")]
        [DisplayFormat(DataFormatString = "{0:N0} تومان")] // فرمت قیمت
        public decimal FinalPrice { get; set; }

        [Display(Name = "وضعیت")]
        public string Status { get; set; }

        [Display(Name = "نوع")]
        public string Type => IsRenewal ? "تمدید" : "جدید"; // خواناتر کردن نوع فاکتور

        public bool IsRenewal { get; set; }

        [Display(Name = "توضیحات")]
        public string? RemarkName { get; set; } // نام پلن یا توضیحات
    }
}
