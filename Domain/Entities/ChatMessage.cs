
using Domain.Entities.Account;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    public class ChatMessage
    {
        [Key]
        public Guid Id { get; set; } // یا int Id { get; set; } یا string Id { get; set; }

        [Required]
        public string SenderUserId { get; set; } = null!;
        public virtual User SenderUser { get; set; } = null!; // Navigation property به کاربر فرستنده

        [Required]
        public string SenderName { get; set; } = null!; // نام فرستنده برای نمایش سریع

        [Required]
        public string RecipientUserId { get; set; } = null!;
        public virtual User RecipientUser { get; set; } = null!; // Navigation property به کاربر گیرنده

        [Required]
        public string Content { get; set; } = null!;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;

        public bool IsDelete { get; set; } = false; // برای حذف منطقی

        public string SenderAvatar { get; set; } = "/assets/images/users/avatar-3.jpg";

        // ---=== پراپرتی‌های مربوط به ریپلای ===---

        /// <summary>
        /// کلید خارجی (Foreign Key) برای پیامی که به آن پاسخ داده شده است.
        /// Nullable است چون همه پیام‌ها ریپلای نیستند.
        /// </summary>
        public Guid? ReplyToMessageId { get; set; } // نوع باید با Id مطابقت داشته باشد و Nullable باشد

        /// <summary>
        /// پراپرتی ناوبری (Navigation Property) به پیامی که به آن پاسخ داده شده است.
        /// Entity Framework از این برای بارگذاری (Include) استفاده می‌کند.
        /// </summary>
        [ForeignKey("ReplyToMessageId")] // مشخص می‌کند که این پراپرتی به کدام کلید خارجی مرتبط است
        public virtual ChatMessage? ReplyToMessage { get; set; }

        /// <summary>
        /// (اختیاری ولی پیشنهادی) مجموعه‌ای از پیام‌هایی که به *این* پیام پاسخ داده‌اند.
        /// </summary>
        public virtual ICollection<ChatMessage> Replies { get; set; } = new List<ChatMessage>();

        // ---===================================---
    }
}
