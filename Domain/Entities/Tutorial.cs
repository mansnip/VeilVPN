using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Tutorial
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required(ErrorMessage = "لطفا عنوان آموزش را وارد کنید.")]
        [MaxLength(200)]
        public string Title { get; set; } // عنوان آموزش

        [Required(ErrorMessage = "لطفا توضیحات کوتاه را وارد کنید.")]
        [MaxLength(500)]
        public string ShortDescription { get; set; } // توضیح کوتاه برای نمایش در گرید

        [Required(ErrorMessage = "لطفا محتوای کامل آموزش را وارد کنید.")]
        public string Content { get; set; } // محتوای کامل آموزش (HTML از ویرایشگر)

        public string? CoverImagePath { get; set; } // مسیر عکس کاور (می‌تواند null باشد)

        [MaxLength(100)]
        public string? Category { get; set; } // دسته‌بندی (مثلا اندروید، iOS، ویندوز)

        public string? Tags { get; set; } // برچسب‌ها (مثلا v2rayng, outline, shadowsocks) - می‌تواند با کاما جدا شود

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // تاریخ ایجاد

        public bool IsPublished { get; set; } = true; // وضعیت انتشار

        // --- لینک‌های دانلود (اختیاری - می‌توانید بیشتر یا کمتر کنید) ---
        public string? DownloadLink1 { get; set; }
        public string? DownloadLinkText1 { get; set; }

        public string? DownloadLink2 { get; set; }
        public string? DownloadLinkText2 { get; set; }

        // در صورت نیاز می‌توانید لینک‌های بیشتری اضافه کنید
        // --- پایان لینک‌های دانلود ---
    }
}
