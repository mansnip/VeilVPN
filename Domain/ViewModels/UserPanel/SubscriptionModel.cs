using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.ViewModels.UserPanel
{
    public class SubscriptionModel
    {
        [Display(Name = "میزان ترافیک")]
        [Required(ErrorMessage = "{0} را باید وارد نمایید.")]
        [Range(10, 500)]
        public int Traffic { get; set; } = 30;

        [Display(Name = "مدت زمان")]
        [Required(ErrorMessage = "{0} را باید وارد نمایید.")]
        [Range(15, 365)]
        public int Duration { get; set; } = 30;
        [Display(Name = "نام")]
        [MaxLength(20, ErrorMessage = "{0} را باید وارد نمایید.")]
        public string? RemarkName { get; set; }

        // پراپرتی‌های جدید برای تمدید
        public bool IsRenewal { get; set; } = false;
        public string RenewalSubscriptionId { get; set; }
        public string RenewalSubscriptionName { get; set; }
    }
}
