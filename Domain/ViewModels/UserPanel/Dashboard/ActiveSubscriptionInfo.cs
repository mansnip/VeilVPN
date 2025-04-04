using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.ViewModels.UserPanel.Dashboard
{
    public class ActiveSubscriptionInfo
    {
        public string Id { get; set; } // برای لینک تمدید
        public string RemarkName { get; set; } // نام دلخواه یا پیش‌فرض اشتراک
        public DateTime ExpiryDate { get; set; }
        public int TotalTrafficGB { get; set; }
        public int UsedTrafficGB { get; set; } // محاسبه شده: Total - Remaining
        public int RemainingTrafficGB { get; set; } // مستقیم از انتیتی Subscription
        public int RemainingDays { get; set; } // محاسبه شده: ExpiryDate - Now
        public double UsagePercentage { get; set; } // محاسبه شده: (Used / Total) * 100
        // سایر اطلاعات مفید از Subscription را می‌توان اضافه کرد
        // public DateTime StartDate { get; set; }
        // public string VpnServerId { get; set; } // شاید برای نمایش سرور متصل
    }
}
