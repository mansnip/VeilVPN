using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.VPN
{
    public class VPNServer : BaseEntity
    {
        [Required(ErrorMessage = "لطفاً نام سرور را وارد کنید.")]
        [Display(Name = "نام سرور")]
        [MaxLength(100, ErrorMessage = "نام سرور نمی‌تواند بیشتر از 100 کاراکتر باشد.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "لطفاً آدرس IP سرور را وارد کنید.")]
        [Display(Name = "آدرس IP")]
        [MaxLength(45, ErrorMessage = "آدرس IP نمی‌تواند بیشتر از 45 کاراکتر باشد.")]
        [RegularExpression(@"^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])$", ErrorMessage = "لطفاً یک آدرس IP معتبر وارد کنید.")]
        public string IpAddress { get; set; }

        [Required(ErrorMessage = "لطفاً آدرس API سرور را وارد کنید.")]
        [Display(Name = "آدرس API")]
        [MaxLength(255, ErrorMessage = "آدرس API نمی‌تواند بیشتر از 255 کاراکتر باشد.")]
        [Url(ErrorMessage = "لطفاً یک URL معتبر وارد کنید.")]
        public string ApiUrl { get; set; }

        [Required(ErrorMessage = "لطفاً نام کاربری API را وارد کنید.")]
        [Display(Name = "نام کاربری API")]
        [MaxLength(100, ErrorMessage = "نام کاربری API نمی‌تواند بیشتر از 100 کاراکتر باشد.")]
        public string ApiUsername { get; set; }

        [Display(Name = "رمز عبور API")]
        [MaxLength(100, ErrorMessage = "رمز عبور API نمی‌تواند بیشتر از 100 کاراکتر باشد.")]
        public string ApiPassword { get; set; }

        [Display(Name = "فعال")]
        public bool IsActive { get; set; } = true;

        [Required(ErrorMessage = "لطفاً حداکثر تعداد کاربران را وارد کنید.")]
        [Display(Name = "حداکثر تعداد کاربران")]
        [Range(1, 10000, ErrorMessage = "حداکثر تعداد کاربران باید بین 1 تا 10000 باشد.")]
        public int MaxUsers { get; set; }

        [Display(Name = "تعداد کاربران فعلی")]
        public int CurrentUsers { get; set; }

        [Required(ErrorMessage = "لطفاً کشور سرور را وارد کنید.")]
        [Display(Name = "کشور")]
        [MaxLength(50, ErrorMessage = "نام کشور نمی‌تواند بیشتر از 50 کاراکتر باشد.")]
        public string Location { get; set; }

        [Display(Name = "آیکون پرچم")]
        [MaxLength(255, ErrorMessage = "آدرس آیکون پرچم نمی‌تواند بیشتر از 255 کاراکتر باشد.")]
        public string Flag { get; set; }

        [Display(Name = "تاریخ بروزرسانی")]
        public DateTime? UpdatedAt { get; set; }
        public int InboundID { get; set; }

    }
}
