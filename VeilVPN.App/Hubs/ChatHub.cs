using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims; // برای دسترسی به UserId

namespace VeilVPN.Hubs
{
    [Authorize] // فقط کاربران لاگین کرده بتوانند به چت وصل شوند
    public class ChatHub : Hub
    {
        // برای نگهداری نقشه بین UserId و ConnectionId (راه حل ساده، برای Production بهتر است از Redis یا دیتابیس استفاده شود)
        private static readonly Dictionary<string, string> UserConnections = new Dictionary<string, string>();
        private static readonly List<string> AdminConnectionIds = new List<string>(); // Connection ID ادمین‌های آنلاین

        // وقتی یک کاربر (یا ادمین) وصل می‌شود
        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier; // UserId کاربر از Claims می‌آید (باید در لاگین تنظیم شده باشد)
            var connectionId = Context.ConnectionId;

            if (!string.IsNullOrEmpty(userId))
            {
                // ذخیره کانکشن کاربر
                lock (UserConnections)
                {
                    UserConnections[userId] = connectionId;
                }

                // اگر کاربر ادمین است، به لیست ادمین‌ها اضافه کن
                if (Context.User.IsInRole("Admin")) // فرض بر اینکه Role ادمین "Admin" است
                {
                    lock (AdminConnectionIds)
                    {
                        if (!AdminConnectionIds.Contains(connectionId))
                            AdminConnectionIds.Add(connectionId);
                    }
                    // TODO: شاید بخواهید لیست چت‌های فعال کاربران را به ادمین بفرستید
                    // await Clients.Client(connectionId).SendAsync("LoadActiveChats", GetActiveChatSummaries()); 
                }
                else
                {
                    // به کاربر خوش‌آمد بگویید یا تاریخچه چت قبلی‌اش را بفرستید
                    // await Clients.Client(connectionId).SendAsync("LoadChatHistory", GetUserChatHistory(userId));
                    // به ادمین(ها) اطلاع بده که کاربر آنلاین شد (اگر لازم است)
                    // await NotifyAdminsUserStatus(userId, true);
                }
            }

            await base.OnConnectedAsync();
        }

        // وقتی یک کاربر (یا ادمین) قطع می‌شود
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;
            var connectionId = Context.ConnectionId;

            if (!string.IsNullOrEmpty(userId))
            {
                lock (UserConnections)
                {
                    // فقط اگر ConnectionId فعلی با UserId مپ شده بود حذف کن
                    if (UserConnections.TryGetValue(userId, out var storedConnectionId) && storedConnectionId == connectionId)
                    {
                        UserConnections.Remove(userId);
                    }
                }
            }

            lock (AdminConnectionIds)
            {
                AdminConnectionIds.Remove(connectionId); // اگر ادمین بود، از لیست ادمین‌ها حذف کن
            }

            // TODO: به ادمین(ها) اطلاع بده که کاربر آفلاین شد (اگر لازم است)
            // if (!Context.User.IsInRole("Admin") && !string.IsNullOrEmpty(userId))
            //    await NotifyAdminsUserStatus(userId, false);

            await base.OnDisconnectedAsync(exception);
        }

        // متد ارسال پیام (کلاینت این متد را صدا می‌زند)
        public async Task SendMessage(string message)
        {
            var senderUserId = Context.UserIdentifier;
            var senderConnectionId = Context.ConnectionId;
            var senderName = Context.User?.Identity?.Name ?? "کاربر"; // نام کاربر

            if (string.IsNullOrWhiteSpace(message) || string.IsNullOrEmpty(senderUserId)) return;

            // 1. ذخیره پیام در دیتابیس (بسیار مهم)
            //    از MediatR برای ارسال Command ذخیره استفاده کنید
            //    var command = new SendChatMessageCommand { SenderUserId = senderUserId, Message = message, SentAt = DateTime.UtcNow };
            //    var messageDto = await _mediator.Send(command); // فرض کنید DTO پیام ذخیره شده برمی‌گرداند

            // TODO: ********** پیاده‌سازی ذخیره پیام در دیتابیس **********
            // مثال ساده بدون ذخیره سازی (فقط برای نمایش عملکرد):
            var messageDto = new
            {
                SenderUserId = senderUserId,
                SenderName = senderName,
                Message = message,
                Timestamp = DateTime.UtcNow,
                IsSender = true // برای نمایش متفاوت پیام فرستنده در UI خودش
            };
            var messageForRecipientDto = new
            {
                SenderUserId = senderUserId,
                SenderName = senderName,
                Message = message,
                Timestamp = DateTime.UtcNow,
                IsSender = false // پیام دریافتی برای گیرنده
            };


            // 2. ارسال پیام به گیرنده (ادمین یا کاربر)
            if (Context.User.IsInRole("Admin"))
            {
                // ادمین به کاربر خاصی پیام می‌فرستد
                // TODO: باید مشخص کنید ادمین در حال چت با کدام کاربر است (مثلاً از طریق پارامتر ورودی recipientUserId)
                string targetUserId = "USER_ID_TO_SEND_TO"; // این باید داینامیک باشد
                string? targetConnectionId;
                lock (UserConnections)
                {
                    UserConnections.TryGetValue(targetUserId, out targetConnectionId);
                }
                if (targetConnectionId != null)
                {
                    // ارسال به ConnectionId کاربر خاص
                    await Clients.Client(targetConnectionId).SendAsync("ReceiveMessage", messageForRecipientDto);
                }
            }
            else // کاربر عادی پیام می‌فرستد
            {
                // ارسال به همه ادمین‌های آنلاین
                List<string> adminIdsToSend;
                lock (AdminConnectionIds)
                {
                    adminIdsToSend = new List<string>(AdminConnectionIds);
                }
                if (adminIdsToSend.Any())
                {
                    await Clients.Clients(adminIdsToSend).SendAsync("ReceiveMessage", messageForRecipientDto);
                    // همچنین می‌توانید یک نوتیفیکیشن جداگانه برای ادمین‌ها بفرستید که کاربر X پیام داده
                    await Clients.Clients(adminIdsToSend).SendAsync("NotifyNewMessage", senderUserId, senderName);
                }
                // TODO: اگر هیچ ادمینی آنلاین نبود، شاید پیام "پشتیبانان آنلاین نیستند" به کاربر نمایش داده شود یا پیام در صف قرار گیرد.
            }

            // 3. ارسال پیام به خود فرستنده (برای نمایش در UI خودش)
            //    (به جز کانکشنی که پیام را فرستاده، اگر کاربر با چند تب باز است)
            // await Clients.Client(senderConnectionId).SendAsync("ReceiveMessage", messageDto); 
            // یا بهتر است UI فرستنده بلافاصله پیام را اضافه کند و منتظر پاسخ سرور نباشد.
            // اما برای تایید دریافت توسط سرور و گرفتن Timestamp دقیق سرور، می‌توان پیام ذخیره شده را برگرداند.
            await Clients.Client(senderConnectionId).SendAsync("MessageSentConfirmation", messageDto);


        }

        // TODO: متدهای دیگر مورد نیاز:
        // - LoadChatHistory(string? otherUserId = null): برای بارگذاری تاریخچه چت کاربر فعلی با ادمین (یا کاربر دیگر اگر چت کاربر-کاربر هم دارید)
        // - MarkMessagesAsRead(string conversationId): وقتی کاربر یا ادمین چتی را باز می‌کند، پیام‌هایش خوانده شوند.
        // - GetActiveChats(): برای ادمین، لیست کاربرانی که منتظر پاسخ هستند یا چت فعال دارند.
        // - NotifyAdminsUserStatus(string userId, bool isOnline): (در بالا اشاره شد)
    }
}