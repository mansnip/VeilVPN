using Domain.Entities.Account;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.VPN
{
    /// <summary>
    /// مدل اشتراک VPN
    /// </summary>
    public class VPNSubscription : BaseEntity
    {
        #region اطلاعات اصلی

        /// <summary>
        /// لینک اتصال به VPN
        /// </summary>
        [Required(ErrorMessage = "لینک اتصال الزامی است")]
        [Display(Name = "لینک اتصال")]
        public string ConnectionUrl { get; set; }

        /// <summary>
        /// نام اشتراک (ایمیل/نام کاربری)
        /// </summary>
        [Required(ErrorMessage = "نام اشتراک الزامی است")]
        [Display(Name = "نام اشتراک")]
        [MaxLength(100, ErrorMessage = "نام اشتراک نمی‌تواند بیشتر از 100 کاراکتر باشد")]
        public string SubscriptionName { get; set; }

        /// <summary>
        /// وضعیت فعال بودن اشتراک
        /// </summary>
        [Display(Name = "فعال")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// شناسه Inbound در سرور VPN
        /// </summary>
        [Display(Name = "شناسه Inbound")]
        public int InboundId { get; set; }

        /// <summary>
        /// مقدار Reset (همیشه 0)
        /// </summary>
        [Display(Name = "Reset")]
        public int Reset { get; set; } = 0;

        #endregion

        #region آمار مصرف

        /// <summary>
        /// میزان دانلود به بایت
        /// </summary>
        [Display(Name = "دانلود (بایت)")]
        public long DownloadBytes { get; set; } = 0;

        /// <summary>
        /// میزان آپلود به بایت
        /// </summary>
        [Display(Name = "آپلود (بایت)")]
        public long UploadBytes { get; set; } = 0;

        /// <summary>
        /// مجموع دانلود و آپلود (بایت)
        /// </summary>
        [Display(Name = "مجموع مصرف (بایت)")]
        [NotMapped]
        public long TotalUsageBytes => DownloadBytes + UploadBytes;

        #endregion

        #region تاریخ‌ها

        /// <summary>
        /// تاریخ خرید (تاریخ یونیکس به میلی‌ثانیه)
        /// </summary>
        [Display(Name = "تاریخ خرید (Unix)")]
        public long PurchaseDateUnix { get; set; }

        /// <summary>
        /// تاریخ انقضا (تاریخ یونیکس به میلی‌ثانیه)
        /// </summary>
        [Display(Name = "تاریخ انقضا (Unix)")]
        public long ExpiryDateUnix { get; set; }

        /// <summary>
        /// تاریخ خرید
        /// </summary>
        [Display(Name = "تاریخ خرید")]
        [NotMapped]
        public DateTime PurchaseDate
        {
            get => DateTimeOffset.FromUnixTimeMilliseconds(PurchaseDateUnix).DateTime;
            set => PurchaseDateUnix = new DateTimeOffset(value).ToUnixTimeMilliseconds();
        }

        /// <summary>
        /// تاریخ انقضا
        /// </summary>
        [Display(Name = "تاریخ انقضا")]
        [NotMapped]
        public DateTime ExpiryDate
        {
            get => DateTimeOffset.FromUnixTimeMilliseconds(ExpiryDateUnix).DateTime;
            set => ExpiryDateUnix = new DateTimeOffset(value).ToUnixTimeMilliseconds();
        }

        /// <summary>
        /// آیا منقضی شده است
        /// </summary>
        [Display(Name = "منقضی شده")]
        [NotMapped]
        public bool IsExpired => DateTime.UtcNow > ExpiryDate;

        /// <summary>
        /// زمان باقی‌مانده تا انقضا (به روز)
        /// </summary>
        [Display(Name = "روزهای باقی‌مانده")]
        [NotMapped]
        public int RemainingDays => IsExpired ? 0 : (int)(ExpiryDate - DateTime.UtcNow).TotalDays;

        #endregion

        #region روابط

        /// <summary>
        /// شناسه سرور
        /// </summary>
        [Required(ErrorMessage = "سرور VPN الزامی است")]
        [Display(Name = "سرور VPN")]
        public string VPNServerId { get; set; }

        /// <summary>
        /// سرور VPN مربوطه
        /// </summary>
        [ForeignKey("VPNServerId")]
        public virtual VPNServer VPNServer { get; set; }

        /// <summary>
        /// شناسه کاربر
        /// </summary>
        [Required(ErrorMessage = "کاربر الزامی است")]
        [Display(Name = "کاربر")]
        public string UserId { get; set; }

        /// <summary>
        /// کاربر مربوطه
        /// </summary>
        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        #endregion

        #region متدهای کمکی

        /// <summary>
        /// تبدیل حجم مصرف به فرمت خوانا
        /// </summary>
        /// <param name="bytes">حجم به بایت</param>
        /// <returns>مقدار فرمت شده</returns>
        public static string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB", "PB" };
            int counter = 0;
            decimal number = bytes;

            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }

            return $"{number:n2} {suffixes[counter]}";
        }

        /// <summary>
        /// دریافت حجم دانلود به فرمت خوانا
        /// </summary>
        [NotMapped]
        [Display(Name = "دانلود")]
        public string DownloadFormatted => FormatBytes(DownloadBytes);

        /// <summary>
        /// دریافت حجم آپلود به فرمت خوانا
        /// </summary>
        [NotMapped]
        [Display(Name = "آپلود")]
        public string UploadFormatted => FormatBytes(UploadBytes);

        /// <summary>
        /// دریافت مجموع مصرف به فرمت خوانا
        /// </summary>
        [NotMapped]
        [Display(Name = "مجموع مصرف")]
        public string TotalUsageFormatted => FormatBytes(TotalUsageBytes);

        #endregion
    }
}
