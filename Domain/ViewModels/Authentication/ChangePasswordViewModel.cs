using System.ComponentModel.DataAnnotations;

namespace Domain.ViewModels.Authentication
{
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "لطفا رمز عبور فعلی خود را وارد کنید")]
        [DataType(DataType.Password)]
        [Display(Name = "رمز عبور فعلی")]
        public string OldPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "لطفا رمز عبور جدید را وارد کنید")]
        [DataType(DataType.Password)]
        [StringLength(100, ErrorMessage = "رمز عبور جدید باید حداقل {2} و حداکثر {1} کاراکتر باشد", MinimumLength = 6)]
        [Display(Name = "رمز عبور جدید")]
        public string NewPassword { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "تکرار رمز عبور جدید")]
        [Compare("NewPassword", ErrorMessage = "رمز عبور جدید و تکرار آن با هم مطابقت ندارند")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
