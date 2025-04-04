using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.DTOs.Chat
{
    public class ChatMessageDto
    {
        public string MessageId { get; set; } = null!;
        public string SenderUserId { get; set; } = null!;
        public string SenderName { get; set; } = null!;
        public string RecipientUserId { get; set; } = null!;
        public string Content { get; set; } = null!;
        public DateTime Timestamp { get; set; }
        public bool IsRead { get; set; }
        public bool IsSender { get; set; } // True if the current user is the sender
        public bool IsDeleted { get; set; } // برای نمایش پیام حذف شده در کلاینت

        public string SenderAvatar { get; set; } = "/assets/images/users/avatar-3.jpg";

        // ---=== پراپرتی‌های مربوط به ریپلای ===---
        public string? ReplyToMessageId { get; set; } // شناسه پیام ریپلای شده (برای کلیک و اسکرول احتمالی)
        public string? ReplyToText { get; set; }      // متن خلاصه شده یا کامل پیام ریپلای شده
        public string? ReplyToSenderName { get; set; }// نام فرستنده پیام ریپلای شده
        // ---===================================---
    }
}
