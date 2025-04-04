using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.ViewModels.UserPanel.Dashboard
{
    // یک مدل خلاصه‌تر برای نمایش لیست فاکتورها در داشبورد
    public class InvoiceSummaryViewModel
    {
        public string Id { get; set; }
        public string InvoiceNumber { get; set; }
        public DateTime InvoiceDate { get; set; }
        public decimal FinalPrice { get; set; } // یا TotalAmount هر کدام که در سرویس استفاده می‌شود
        public string Status { get; set; } // وضعیت پرداخت
    }
}
