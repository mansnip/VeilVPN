using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class ChatMessage : BaseEntity
    {
        public string SenderUserId { get; set; } = null!; // ID کاربری که پیام را فرستاده
        public string? SenderName { get; set; } // نام فرستنده (برای نمایش راحت‌تر)
        public string? RecipientUserId { get; set; } // ID گیرنده (می‌تواند Null باشد اگر پیام به گروه ادمین ارسال می‌شود)
        public string Content { get; set; } = null!; // متن پیام
        public DateTime Timestamp { get; set; } // زمان ارسال (بهتر است UTC باشد)
        public bool IsRead { get; set; } // آیا توسط گیرنده خوانده شده؟
    }
}
