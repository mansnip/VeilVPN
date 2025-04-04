using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.DTOs.Chat
{
    public class ChatContactDto
    {
        public string Id { get; set; } // تغییر نام از UserId
        public string Name { get; set; }
        public string? Avatar { get; set; }
        public string Status { get; set; } // "online" یا "offline"
        public int UnreadCount { get; set; }
        public string? LastMessage { get; set; }
        public DateTime? LastMessageTime { get; set; }
        public DateTime? LastSeen { get; set; } // این در JS فعلی استفاده نمی‌شود اما نگه داشتن آن خوب است
        public bool IsTyping { get; set; } // این در JS فعلی استفاده نمی‌شود اما نگه داشتن آن خوب است
        public string Type { get; set; } = "user"; // اضافه شد: "user" یا "channel"
    }
}
