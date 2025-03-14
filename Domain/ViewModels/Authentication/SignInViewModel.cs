using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.ViewModels.Authentication
{
    public class SignInViewModel
    {
        [Display(Name = "ایمیل")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        [EmailAddress(ErrorMessage = "لطفا یک ایمیل معتبر وارد کنید")]
        [MaxLength(50, ErrorMessage = "حداکثر طول {1} کاراکتر است")]
        public string Email { get; set; }
        [Display(Name = "رمز عبور")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        [DataType(DataType.Password)]
        [MaxLength(50, ErrorMessage = "حداکثر طول {1} کاراکتر است")]
        public string Password { get; set; }
        [Display(Name = "مرا به خاطر بسپار")]
        public bool IsRememberMe { get; set; }
    }
}
