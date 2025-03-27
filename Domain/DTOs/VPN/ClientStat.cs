using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Domain.DTOs.VPN
{
    /// <summary>
    /// کلاس مدل آمار کاربر
    /// </summary>
    public class ClientStat
    {
        /// <summary>
        /// شناسه آمار کاربر
        /// </summary>
        [JsonPropertyName("id")]
        public int Id { get; set; }

        /// <summary>
        /// شناسه Inbound
        /// </summary>
        [JsonPropertyName("inboundId")]
        public int InboundId { get; set; }

        /// <summary>
        /// وضعیت فعال بودن
        /// </summary>
        [JsonPropertyName("enable")]
        public bool Enable { get; set; }

        /// <summary>
        /// ایمیل کاربر
        /// </summary>
        [JsonPropertyName("email")]
        public string Email { get; set; }

        /// <summary>
        /// حجم آپلود شده
        /// </summary>
        [JsonPropertyName("up")]
        public long Up { get; set; }

        /// <summary>
        /// حجم دانلود شده
        /// </summary>
        [JsonPropertyName("down")]
        public long Down { get; set; }

        /// <summary>
        /// زمان انقضا (میلی‌ثانیه Unix)
        /// </summary>
        [JsonPropertyName("expiryTime")]
        public long ExpiryTime { get; set; }

        /// <summary>
        /// کل حجم مجاز (بایت)
        /// </summary>
        [JsonPropertyName("total")]
        public long Total { get; set; }

        /// <summary>
        /// زمان ریست
        /// </summary>
        [JsonPropertyName("reset")]
        public int Reset { get; set; }

        /// <summary>
        /// حجم باقیمانده به گیگابایت
        /// </summary>
        [JsonIgnore]
        public double RemainingGigabytes
        {
            get
            {
                if (Total <= 0) return double.MaxValue; // نامحدود
                long usedBytes = Up + Down;
                long remainingBytes = Total - usedBytes;
                if (remainingBytes < 0) remainingBytes = 0;
                return Math.Round(remainingBytes / (1024.0 * 1024 * 1024), 2);
            }
        }

        /// <summary>
        /// درصد استفاده شده از ترافیک
        /// </summary>
        [JsonIgnore]
        public int UsagePercentage
        {
            get
            {
                if (Total <= 0) return 0; // نامحدود
                long usedBytes = Up + Down;
                double percentage = (usedBytes * 100.0) / Total;
                return (int)Math.Min(100, Math.Round(percentage, 0));
            }
        }

        /// <summary>
        /// تاریخ انقضا به صورت DateTime
        /// </summary>
        [JsonIgnore]
        public DateTime? ExpiryDate
        {
            get
            {
                if (ExpiryTime <= 0) return null; // بدون تاریخ انقضا
                return DateTimeOffset.FromUnixTimeMilliseconds(ExpiryTime).DateTime;
            }
        }

        /// <summary>
        /// وضعیت انقضا
        /// </summary>
        [JsonIgnore]
        public bool IsExpired
        {
            get
            {
                if (ExpiryTime <= 0) return false; // بدون تاریخ انقضا
                return DateTimeOffset.FromUnixTimeMilliseconds(ExpiryTime).DateTime < DateTime.Now;
            }
        }

        /// <summary>
        /// روزهای باقیمانده
        /// </summary>
        [JsonIgnore]
        public int? RemainingDays
        {
            get
            {
                if (ExpiryTime <= 0) return null; // بدون تاریخ انقضا
                var expiry = DateTimeOffset.FromUnixTimeMilliseconds(ExpiryTime).DateTime;
                var remaining = expiry - DateTime.Now;
                return Math.Max(0, (int)remaining.TotalDays);
            }
        }

        /// <summary>
        /// آیا اکانت فعال است
        /// </summary>
        [JsonIgnore]
        public bool IsActive => Enable && !IsExpired && (Total <= 0 || (Up + Down) < Total);
    }
}
