using Domain.Entities.Account;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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

        [Display(Name = "نام")]
        [MaxLength(20, ErrorMessage = "{0} نمی تواند بیشتر از 20 کاراکتر باشد.")]
        public string? RemarkName { get; set; }
        public SubscriptionDetails Subscription { get; set; }

        // پراپرتی‌های جدید برای تمدید اشتراک
        public bool IsRenewal { get; set; } = false;
        public string RenewalSubscriptionId { get; set; }
        public string RenewalSubscriptionName { get; set; }

        // برای سازگاری با کد فعلی، یک پراپرتی Status اضافه می‌کنیم که همان PaymentStatus را برمی‌گرداند
        public string Status
        {
            get { return PaymentStatus; }
            set { PaymentStatus = value; }
        }
    }
}
