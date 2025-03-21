using Domain.Entities.Account;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.ViewModels.UserPanel
{
    public class InvoiceViewModel
    {
        public string Id { get; set; } // شناسه فاکتور برای استفاده در اکشن‌های پرداخت و لغو
        public string InvoiceNumber { get; set; }
        public DateTime InvoiceDate { get; set; }
        public string PaymentStatus { get; set; }
        public string UserId { get; set; } // اضافه شده
        public string UserFullName { get; set; }
        public string UserEmail { get; set; }
        public string UserPhone { get; set; }
        public decimal TotalAmount => Subscription?.FinalPrice ?? 0;
        public SubscriptionDetails Subscription { get; set; }

        // برای سازگاری با کد فعلی، یک پراپرتی Status اضافه می‌کنیم که همان PaymentStatus را برمی‌گرداند
        public string Status
        {
            get { return PaymentStatus; }
            set { PaymentStatus = value; }
        }
    }

}
