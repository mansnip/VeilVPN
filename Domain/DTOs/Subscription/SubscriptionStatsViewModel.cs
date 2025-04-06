using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.DTOs.Subscription
{
    public class SubscriptionStatsViewModel
    {
        public string Id { get; set; }
        public double RemainingTrafficGB { get; set; }
        public double UsedTrafficGB { get; set; }
        public double TotalTrafficGB { get; set; }
        public double UsagePercentage { get; set; } // درصد مصرف
        public int? RemainingDays { get; set; } // ممکن است نامحدود باشد
        public string ExpiryDatePersian { get; set; }
        public string SubscriptionName { get; set; }
        public bool IsVpnActive { get; set; } // وضعیت اتصال از دید سرور VPN
                                              // فیلدهای دیگری که ممکن است در UI نیاز داشته باشید را اضافه کنید
    }
}
