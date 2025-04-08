using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.DTOs.Subscription
{
    public class LiveUsageUpdateViewModel
    {
        public string Id { get; set; } // برای پیدا کردن ردیف در جدول
        public double RemainingTraffic { get; set; } // VpnRemainingTraffic
        public double UsedTraffic { get; set; } // محاسبه شده: Total - Remaining
        public int UsagePercentage { get; set; } // VpnUsagePercentage
        public string StatusText { get; set; } // برای آپدیت badge وضعیت
        public string StatusClass { get; set; } // برای آپدیت رنگ badge وضعیت
        public bool ShowRenewButton { get; set; } // برای نمایش یا عدم نمایش دکمه تمدید
        public bool IsVpnActive { get; set; } // برای آپدیت دقیق تر وضعیت
        public int? VpnRemainingDays { get; set; } // برای آپدیت وضعیت و دکمه تمدید
        public bool HasVpnConnection { get; set; } // برای نمایش خطا یا وضعیت آماده سازی
        public int Traffic { get; set; } // ترافیک کل برای محاسبه مصرف و شرط نامحدود

    }
}
