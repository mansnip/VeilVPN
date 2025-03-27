using System;

namespace Domain.DTOs.VPN
{
    public class SubscriptionViewModel
    {
        // اطلاعات پایه از اشتراک دیتابیس
        public string Id { get; set; }
        public int Traffic { get; set; }  // گیگابایت
        public int Duration { get; set; }  // روز
        public int RemainingTraffic { get; set; }  // گیگابایت
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public int DaysRemaining { get; set; }
        public int PercentTrafficUsed { get; set; }
        public string? RemarkName { get; set; }
        public string VpnId { get; set; }
        public int Port { get; set; }

        // اطلاعات اضافی از سرور VPN
        public bool IsVpnActive { get; set; }  // فعال بودن در سرور VPN
        public double VpnRemainingTraffic { get; set; }  // گیگابایت باقیمانده در سرور VPN
        public int? VpnRemainingDays { get; set; }  // روزهای باقیمانده در سرور VPN
        public int VpnUsagePercentage { get; set; }  // درصد استفاده شده در سرور VPN
        public bool HasVpnConnection { get; set; }  // آیا اطلاعات VPN موجود است
        public string VpnEmailName { get; set; }  // ایمیل کاربر در سرور VPN
        public string VpnServerName { get; set; }  // نام سرور VPN
        public string VpnServerUrl { get; set; }

        // وضعیت ترکیبی
        public string StatusText => GetStatusText();
        public string StatusClass => GetStatusClass();

        // پراپرتی جدید: لینک اتصال
        public string SubscriptionLink => GetSubscriptionLink();

        private string GetStatusText()
        {
            if (!HasVpnConnection)
                return "درحال آماده‌سازی";

            if (!IsActive)
                return "غیرفعال";

            if (!IsVpnActive)
                return "مشکل اتصال به سرور";

            if (VpnRemainingDays.HasValue && VpnRemainingDays.Value <= 0)
                return "منقضی شده";

            if (VpnRemainingTraffic <= 0 && Traffic > 0)
                return "اتمام ترافیک";

            return "فعال";
        }

        private string GetStatusClass()
        {
            if (!HasVpnConnection)
                return "bg-warning text-dark";

            if (!IsActive || !IsVpnActive)
                return "bg-danger";

            if ((VpnRemainingDays.HasValue && VpnRemainingDays.Value <= 0) ||
                (VpnRemainingTraffic <= 0 && Traffic > 0))
                return "bg-danger";

            if ((VpnRemainingDays.HasValue && VpnRemainingDays.Value <= 3) ||
                (VpnRemainingTraffic < 5 && Traffic > 0))
                return "bg-warning text-dark";

            return "bg-success";
        }

        private string GetSubscriptionLink()
        {
            // استخراج اطلاعات مورد نیاز از VpnServerUrl
            if (string.IsNullOrEmpty(VpnServerUrl))
                return string.Empty; // اگر لینک سرور موجود نباشد، مقدار خالی برمی‌گردد

            // استخراج "n1.seadata.ir:46421" از لینک کامل
            var serverInfo = ExtractServerInfo(VpnServerUrl);

            // اگر پورت مشخص‌شده است، جایگزین کنیم
            if (!string.IsNullOrEmpty(serverInfo) && Port > 0)
            {
                var colonIndex = serverInfo.IndexOf(':');
                if (colonIndex > -1)
                {
                    serverInfo = $"{serverInfo.Substring(0, colonIndex)}:{Port}";
                }
            }

            // ساخت لینک نهایی
            return $"vless://{VpnId}@{serverInfo}?type=tcp&path=%2F&host=www.speedtest.net&headerType=http&security=none#{RemarkName}";
        }

        private string ExtractServerInfo(string url)
        {
            try
            {
                // استخراج قسمت مورد نیاز بین "https://" و "/"
                var startIndex = url.IndexOf("https://", StringComparison.Ordinal) + "https://".Length;
                var endIndex = url.IndexOf('/', startIndex);
                return url.Substring(startIndex, endIndex - startIndex);
            }
            catch
            {
                return string.Empty; // اگر خطایی در استخراج پیش آمد، مقدار خالی برمی‌گردد
            }
        }
    }
}
