using DataLayer.Context;
using Domain.DTOs.Chat;
using Domain.Entities; // فرض وجود انتیتی ChatMessage و User
using Microsoft.EntityFrameworkCore;
using VeilVPN.App.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VeilVPN.App.Services.Implimentation
{
    public class ChatService : IChatService
    {
        private readonly VeilVpnDbContext _context;
        private readonly IUserConnectionManager _userConnectionManager;

        public ChatService(VeilVpnDbContext context, IUserConnectionManager userConnectionManager)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userConnectionManager = userConnectionManager ?? throw new ArgumentNullException(nameof(userConnectionManager));
        }

        // دریافت لیست مخاطبین اولیه (با وضعیت آنلاین)
        public async Task<IEnumerable<ChatContactDto>> GetInitialContactsForUserAsync(string userId, bool isAdmin)
        {
            var contacts = new List<ChatContactDto>();

            if (isAdmin)
            {
                // ادمین: لیست همه کاربران غیر ادمین
                var users = await _context.Users
                                          .Where(u => !u.IsAdmin && !u.IsDelete) // فرض وجود فیلد IsAdmin و IsDelete در User
                                          .OrderBy(u => u.Email) // یا هر فیلد نام دیگر
                                          .ToListAsync();

                foreach (var user in users)
                {
                    var lastMessage = await _context.ChatMessages
                                                    .Where(m => !m.IsDelete && ((m.SenderUserId == userId && m.RecipientUserId == user.Id) || (m.SenderUserId == user.Id && m.RecipientUserId == userId)))
                                                    .OrderByDescending(m => m.Timestamp)
                                                    .FirstOrDefaultAsync();

                    var unreadCount = await _context.ChatMessages
                                                  .CountAsync(m => !m.IsDelete && m.RecipientUserId == userId && m.SenderUserId == user.Id && !m.IsRead);

                    bool isOnline = await _userConnectionManager.IsUserOnline(user.Id);
                    // DateTime? lastSeen = user.LastSeen; // TODO: Uncomment if you have LastSeen property on User entity

                    contacts.Add(new ChatContactDto
                    {
                        Id = user.Id, // <<< تغییر نام از UserId به Id
                        Name = user.Email ?? $"کاربر {user.Id.Substring(0, 5)}", // استفاده از ایمیل یا نام کاربری
                        Avatar = "/assets/images/users/avatar-2.jpg", // <<< استفاده از آواتار کاربر یا پیش‌فرض
                        Status = isOnline ? "online" : "offline", // <<< استفاده از Status به جای IsOnline/StatusText
                        UnreadCount = unreadCount,
                        LastMessage = lastMessage?.Content, // نام پراپرتی محتوای پیام شما ممکن است متفاوت باشد
                        LastMessageTime = lastMessage?.Timestamp, // نام پراپرتی زمان ارسال پیام شما ممکن است متفاوت باشد
                                                                  // LastSeen = lastSeen, // این فیلد در DTO هست اما در buildContactHtml فعلا استفاده نمی‌شود
                        IsTyping = false, // مقدار اولیه برای isTyping
                        Type = "user" // <<< تعیین نوع مخاطب
                    });
                }
            }
            else
            {
                // کاربر عادی: فقط مخاطب "پشتیبانی" (اولین ادمین فعال)
                // TODO: در نظر بگیرید که شاید بهتر باشد کاربر عادی بتواند با ادمین‌های مختلف صحبت کند
                // یا یک حساب کاربری "پشتیبانی" جداگانه داشته باشید.
                var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.IsAdmin && !u.IsDelete);
                if (adminUser != null)
                {
                    var adminUserId = adminUser.Id;

                    var lastMessage = await _context.ChatMessages
                                                    .Where(m => !m.IsDelete && ((m.SenderUserId == userId && m.RecipientUserId == adminUserId) || (m.SenderUserId == adminUserId && m.RecipientUserId == userId)))
                                                    .OrderByDescending(m => m.Timestamp)
                                                    .FirstOrDefaultAsync();

                    var unreadCount = await _context.ChatMessages
                                                  .CountAsync(m => !m.IsDelete && m.RecipientUserId == userId && m.SenderUserId == adminUserId && !m.IsRead);

                    bool isOnline = await _userConnectionManager.IsUserOnline(adminUserId);
                    // DateTime? lastSeen = adminUser.LastSeen; // TODO: Uncomment if you have LastSeen

                    contacts.Add(new ChatContactDto
                    {
                        Id = adminUserId, // <<< تغییر نام از UserId به Id
                        Name = adminUser.Email ?? "پشتیبانی", // نام ادمین یا "پشتیبانی"
                        Avatar = "/assets/images/users/avatar-3.jpg", // <<< استفاده از آواتار ادمین یا پیش‌فرض
                        Status = isOnline ? "online" : "offline", // <<< استفاده از Status به جای IsOnline/StatusText
                        UnreadCount = unreadCount,
                        LastMessage = lastMessage?.Content,
                        LastMessageTime = lastMessage?.Timestamp,
                        // LastSeen = lastSeen,
                        IsTyping = false,
                        Type = "user" // <<< تعیین نوع مخاطب (حتی برای ادمین در لیست کاربر)
                    });
                }
                // else: No admin found, contact list remains empty for normal user.
            }

            // مرتب‌سازی نهایی: اول آنلاین‌ها، بعد بر اساس آخرین پیام
            return contacts.OrderByDescending(c => c.Status == "online") // <<< مرتب‌سازی بر اساس Status
                           .ThenByDescending(c => c.LastMessageTime ?? DateTime.MinValue)
                           .ToList();
        }

        // سایر متدهای ChatService...
        // مانند: GetChatHistoryAsync, SaveMessageAsync, DeleteMessageAsync, MarkMessagesAsReadAsync و غیره


        // دریافت تاریخچه چت بین دو کاربر
        public async Task<IEnumerable<ChatMessageDto>> GetChatHistoryAsync(string userId1, string userId2)
        {
            var messages = await _context.ChatMessages
                .Where(m => !m.IsDelete && // فقط پیام‌های حذف نشده
                            ((m.SenderUserId == userId1 && m.RecipientUserId == userId2) ||
                             (m.SenderUserId == userId2 && m.RecipientUserId == userId1)))
                .OrderBy(m => m.Timestamp)
                .Include(m => m.SenderUser) // *** تغییر 1: Include کردن اطلاعات فرستنده ***
                .Include(m => m.ReplyToMessage) // Include کردن پیام ریپلای شده
                    .ThenInclude(rm => rm != null ? rm.SenderUser : null) // *** بهبود: Include کردن فرستنده پیام ریپلای شده (اگر وجود داشت) ***
                .ToListAsync();

            // علامت‌گذاری پیام‌های خوانده نشده (اگر لازم است، اینجا یا بعد از Select انجام شود)
            // await MarkMessagesAsReadAsync(userId2, userId1); // این خط رو بررسی کنید که آیا قبل از Select لازمه یا بعدش

            // تبدیل به DTO
            return messages.Select(m => new ChatMessageDto
            {
                MessageId = m.Id.ToString(),
                SenderUserId = m.SenderUserId,
                // *** تغییر 3: خواندن نام از User مرتبط (حتما نام پراپرتی مثل UserName را چک کنید) ***
                SenderName = m.SenderUser?.Email ?? "کاربر ناشناس", // <- استفاده از SenderUser
                RecipientUserId = m.RecipientUserId,
                Content = m.Content,
                Timestamp = m.Timestamp,
                IsRead = m.IsRead,
                IsSender = m.SenderUserId == userId1,
                IsDeleted = m.IsDelete,
                ReplyToMessageId = m.ReplyToMessageId?.ToString(),
                ReplyToText = m.ReplyToMessage?.Content,
                // *** تغییر 4: خواندن نام فرستنده ریپلای از User مرتبط ***
                ReplyToSenderName = m.ReplyToMessage?.SenderUser?.Email ?? "کاربر ناشناس", // <- استفاده از SenderUser
                SenderAvatar = m.SenderAvatar,
            }).ToList();
        }

        // ذخیره پیام جدید در دیتابیس
        public async Task<ChatMessageDto> SaveMessageAsync(string senderUserId, string senderName, string recipientUserId, string content, string? replyToMessageId = null)
        {
            Guid? replyGuid = null;
            ChatMessage? repliedToMessage = null;
            if (!string.IsNullOrEmpty(replyToMessageId) && Guid.TryParse(replyToMessageId, out Guid parsedGuid))
            {
                replyGuid = parsedGuid;
                // برای نمایش در DTO، اطلاعات پیام اصلی را واکشی می‌کنیم
                repliedToMessage = await _context.ChatMessages
                                                 .AsNoTracking() // فقط برای خواندن نیاز داریم
                                                 .FirstOrDefaultAsync(m => m.Id == replyGuid && !m.IsDelete);
            }

            var message = new ChatMessage
            {
                Id = Guid.NewGuid(), // شناسه جدید برای پیام
                SenderUserId = senderUserId,
                SenderName = senderName,
                RecipientUserId = recipientUserId,
                Content = content,
                Timestamp = DateTime.UtcNow,
                IsRead = false, // پیام جدید خوانده نشده است
                IsDelete = false,
                ReplyToMessageId = replyGuid
            };

            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();

            // تبدیل به DTO برای ارسال به کلاینت‌ها
            return new ChatMessageDto
            {
                MessageId = message.Id.ToString(),
                SenderUserId = message.SenderUserId,
                SenderName = message.SenderName,
                RecipientUserId = message.RecipientUserId,
                Content = message.Content,
                Timestamp = message.Timestamp,
                IsRead = message.IsRead,
                IsSender = true, // از دید فرستنده، این پیام را او ارسال کرده
                ReplyToMessageId = message.ReplyToMessageId?.ToString(),
                ReplyToText = repliedToMessage?.Content, // متن پیام ریپلای شده
                ReplyToSenderName = repliedToMessage?.SenderName // نام فرستنده پیام ریپلای شده
            };
        }

        // حذف پیام (علامت‌گذاری به عنوان حذف شده)
        public async Task<DeleteMessageResult> DeleteMessageAsync(string messageId, string requestingUserId)
        {
            if (!Guid.TryParse(messageId, out Guid messageGuid))
            {
                return new DeleteMessageResult { Success = false, ErrorMessage = "شناسه پیام نامعتبر است." };
            }

            var message = await _context.ChatMessages.FirstOrDefaultAsync(m => m.Id == messageGuid && !m.IsDelete);

            if (message == null)
            {
                return new DeleteMessageResult { Success = false, ErrorMessage = "پیام یافت نشد." };
            }

            // فقط فرستنده می‌تواند پیام خود را حذف کند
            if (message.SenderUserId != requestingUserId)
            {
                return new DeleteMessageResult { Success = false, ErrorMessage = "شما مجاز به حذف این پیام نیستید." };
            }

            message.IsDelete = true;
            message.Content = "این پیام حذف شد"; // یا null اگر نمی‌خواهید متنی نمایش داده شود
            await _context.SaveChangesAsync();

            return new DeleteMessageResult
            {
                Success = true,
                SenderUserId = message.SenderUserId,
                RecipientUserId = message.RecipientUserId
            };
        }


        // علامت‌گذاری پیام‌ها به عنوان خوانده شده
        // Note: Logic might need adjustment. This marks messages FROM senderUserId TO readerUserId as read.
        public async Task MarkMessagesAsReadAsync(string senderUserId, string readerUserId)
        {
            var unreadMessages = await _context.ChatMessages
                .Where(m => m.RecipientUserId == readerUserId &&
                            m.SenderUserId == senderUserId &&
                            !m.IsRead &&
                            !m.IsDelete)
                .ToListAsync();

            if (unreadMessages.Any())
            {
                foreach (var message in unreadMessages)
                {
                    message.IsRead = true;
                }
                await _context.SaveChangesAsync();
                Console.WriteLine($"Marked {unreadMessages.Count} messages from {senderUserId} to {readerUserId} as read.");
            }
        }

        // (اختیاری) متد کمکی برای ایجاد شناسه مکالمه پایدار
        private string GenerateConversationId(string userId1, string userId2)
        {
            return string.CompareOrdinal(userId1, userId2) < 0
                ? $"{userId1}_{userId2}"
                : $"{userId2}_{userId1}";
        }

    } // *** End of ChatService class ***

    // کلاس کمکی برای نتیجه حذف پیام (برای ارسال اطلاعات به هاب)
    public class DeleteMessageResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string SenderUserId { get; set; } = string.Empty; // شناسه فرستنده پیام حذف شده
        public string RecipientUserId { get; set; } = string.Empty; // شناسه گیرنده پیام حذف شده
    }

} // *** End of namespace ***
