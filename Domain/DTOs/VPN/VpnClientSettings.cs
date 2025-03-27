using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Domain.DTOs.VPN
{
    /// <summary>
    /// تنظیمات کلاینت VPN
    /// </summary>
    public class VpnClientSettings
    {
        /// <summary>
        /// شناسه منحصربفرد کلاینت (UUID)
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// نوع جریان - معمولاً خالی
        /// </summary>
        [JsonPropertyName("flow")]
        public string Flow { get; set; } = "";

        /// <summary>
        /// نام اشتراک / ایمیل کاربر
        /// </summary>
        [JsonPropertyName("email")]
        public string Email { get; set; }

        /// <summary>
        /// محدودیت تعداد IP (0 = نامحدود)
        /// </summary>
        [JsonPropertyName("limitIp")]
        public int LimitIp { get; set; } = 0;

        /// <summary>
        /// محدودیت کل ترافیک به بایت (0 = نامحدود)
        /// </summary>
        [JsonPropertyName("totalGB")]
        public long TotalGB { get; set; } = 0;

        /// <summary>
        /// زمان انقضا (یونیکس میلی‌ثانیه)
        /// </summary>
        [JsonPropertyName("expiryTime")]
        public long ExpiryTime { get; set; } = 0;

        /// <summary>
        /// وضعیت فعال بودن
        /// </summary>
        [JsonPropertyName("enable")]
        public bool Enable { get; set; } = true;

        /// <summary>
        /// شناسه تلگرام (اختیاری)
        /// </summary>
        [JsonPropertyName("tgId")]
        public string TgId { get; set; } = "";

        /// <summary>
        /// شناسه اشتراک (معمولا رندوم)
        /// </summary>
        [JsonPropertyName("subId")]
        public string SubId { get; set; } = GenerateRandomSubId();

        /// <summary>
        /// توضیحات اضافی (اختیاری)
        /// </summary>
        [JsonPropertyName("comment")]
        public string Comment { get; set; } = "";

        /// <summary>
        /// تولید شناسه اشتراک تصادفی
        /// </summary>
        private static string GenerateRandomSubId(int length = 16)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
