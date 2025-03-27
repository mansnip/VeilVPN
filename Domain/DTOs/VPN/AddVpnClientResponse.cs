using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Domain.DTOs.VPN
{
    /// <summary>
    /// پاسخ درخواست افزودن کلاینت VPN
    /// </summary>
    public class AddVpnClientResponse
    {
        /// <summary>
        /// وضعیت عملیات (success یا error)
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; }

        /// <summary>
        /// بررسی موفقیت آمیز بودن عملیات
        /// </summary>
        [JsonIgnore]
        public bool IsSuccess => Status?.ToLower() == "success";

        /// <summary>
        /// پیام خطا در صورت وجود
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; set; }
    }
}
