using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.ViewModels.UserPanel.Dashboard
{
    public class DashboardViewModel
    {
        public string UserName { get; set; } // نام نمایشی کاربر
        public bool HasActiveSubscription { get; set; }
        public ActiveSubscriptionInfo ActiveSubscription { get; set; } // جزئیات اشتراک فعال (اگر وجود دارد)
        public int PendingInvoicesCount { get; set; } // تعداد فاکتورهای در انتظار پرداخت
        public List<InvoiceSummaryViewModel> RecentInvoices { get; set; } // چند فاکتور اخیر برای نمایش

        // می‌توانید اطلاعات دیگری مانند تعداد تیکت‌های باز و... را در آینده اضافه کنید
        // public int OpenTicketsCount { get; set; }
    }
}
