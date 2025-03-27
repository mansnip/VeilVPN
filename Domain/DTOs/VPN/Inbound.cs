using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Domain.DTOs.VPN
{
    /// <summary>
    /// کلاس مدل Inbound
    /// </summary>
    public class Inbound
    {
        /// <summary>
        /// شناسه Inbound
        /// </summary>
        [JsonPropertyName("id")]
        public int Id { get; set; }

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
        /// کل حجم مجاز
        /// </summary>
        [JsonPropertyName("total")]
        public long Total { get; set; }

        /// <summary>
        /// نام Inbound
        /// </summary>
        [JsonPropertyName("remark")]
        public string Remark { get; set; }

        /// <summary>
        /// وضعیت فعال بودن
        /// </summary>
        [JsonPropertyName("enable")]
        public bool Enable { get; set; }

        /// <summary>
        /// زمان انقضا (میلی‌ثانیه Unix)
        /// </summary>
        [JsonPropertyName("expiryTime")]
        public long ExpiryTime { get; set; }

        /// <summary>
        /// پورت
        /// </summary>
        [JsonPropertyName("port")]
        public int Port { get; set; }

        /// <summary>
        /// آمار کاربران
        /// </summary>
        [JsonPropertyName("clientStats")]
        public List<ClientStat> ClientStats { get; set; }

        /// <summary>
        /// پروتکل (فقط برای سازگاری)
        /// </summary>
        [JsonPropertyName("protocol")]
        public string Protocol { get; set; }

        /// <summary>
        /// تنظیمات (فقط برای سازگاری)
        /// </summary>
        [JsonPropertyName("settings")]
        public string Settings { get; set; }

        /// <summary>
        /// آدرس گوش دادن (فقط برای سازگاری)
        /// </summary>
        [JsonPropertyName("listen")]
        public string Listen { get; set; }
    }

}
