using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Domain.DTOs.VPN
{
    /// <summary>
    /// مدل درخواست برای افزودن کلاینت به اینباند VPN
    /// </summary>
    public class AddVpnClientRequest
    {
        /// <summary>
        /// شناسه اینباند
        /// </summary>
        [JsonPropertyName("id")]
        public int InboundId { get; set; }

        /// <summary>
        /// تنظیمات کلاینت به صورت رشته JSON
        /// </summary>
        [JsonPropertyName("settings")]
        public string Settings { get; set; }

        /// <summary>
        /// سازنده با اطلاعات کلاینت جدید
        /// </summary>
        public AddVpnClientRequest(int inboundId, VpnClientSettings clientSettings)
        {
            InboundId = inboundId;

            // تبدیل تنظیمات به فرمت JSON مورد نیاز API
            var jsonSettings = System.Text.Json.JsonSerializer.Serialize(
                new { clients = new[] { clientSettings } },
                new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = false,
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                }
            );

            Settings = jsonSettings;
        }
    }
}
