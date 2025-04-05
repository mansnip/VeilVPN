using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Domain.ViewModels.UserPanel.Tutorials
{
    public class TutorialCreateViewModel
    {
        [Required(ErrorMessage = "لطفا عنوان آموزش را وارد کنید.")]
        [MaxLength(200)]
        [Display(Name = "عنوان آموزش")]
        public string Title { get; set; }

        [Required(ErrorMessage = "لطفا توضیحات کوتاه را وارد کنید.")]
        [MaxLength(500)]
        [DataType(DataType.MultilineText)]
        [Display(Name = "توضیحات کوتاه")]
        public string ShortDescription { get; set; }

        [Required(ErrorMessage = "لطفا محتوای کامل آموزش را وارد کنید.")]
        [Display(Name = "محتوای آموزش")]
        public string Content { get; set; } // این توسط CKEditor یا ویرایشگر دیگر پر می‌شود

        [Display(Name = "تصویر کاور")]
        public IFormFile? CoverImageFile { get; set; } // برای آپلود فایل

        [MaxLength(100)]
        [Display(Name = "دسته‌بندی")]
        public string? Category { get; set; }

        [Display(Name = "برچسب‌ها (با کاما جدا کنید)")]
        public string? TagsString { get; set; }

        [Display(Name = "انتشار")]
        public bool IsPublished { get; set; } = true;

        [Display(Name = "لینک دانلود 1")]
        [Url(ErrorMessage = "لطفا یک آدرس معتبر وارد کنید.")]
        public string? DownloadLink1 { get; set; }

        [Display(Name = "متن لینک دانلود 1")]
        [MaxLength(100)]
        public string? DownloadLinkText1 { get; set; }

        [Display(Name = "لینک دانلود 2")]
        [Url(ErrorMessage = "لطفا یک آدرس معتبر وارد کنید.")]
        public string? DownloadLink2 { get; set; }

        [Display(Name = "متن لینک دانلود 2")]
        [MaxLength(100)]
        public string? DownloadLinkText2 { get; set; }

        // ... می‌توانید لینک‌های بیشتری اضافه کنید
    }
}
