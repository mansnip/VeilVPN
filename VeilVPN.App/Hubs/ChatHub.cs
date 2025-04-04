using Application.Services.Interfaces;
using DataLayer.Context;
using Domain.DTOs.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VeilVPN.App.Services.Interfaces;
using static VeilVPN.Hubs.ChatHub; // برای IChatService

namespace VeilVPN.Hubs
{
    [Authorize]
    public class ChatHub : Hub<IChatClient> // Strongly-typed Hub
    {
        private readonly IChatService _chatService;
        private readonly VeilVpnDbContext _dbContext;
        private readonly IUserConnectionManager _userConnectionManager; // *** تزریق شد ***
        private readonly IUserService _userService;

        // --- دیکشنری‌های استاتیک حذف شدند ---
        // public static readonly ConcurrentDictionary<string, string> UserConnections = new ConcurrentDictionary<string, string>();
        // private static readonly ConcurrentDictionary<string, string> ConnectionRoles = new ConcurrentDictionary<string, string>();
        // private static readonly ConcurrentDictionary<string, bool> AdminConnectionIds = new ConcurrentDictionary<string, bool>();

        // تزریق سرویس‌ها و DbContext
        public ChatHub(IChatService chatService, VeilVpnDbContext dbContext, IUserConnectionManager userConnectionManager, IUserService userService) // *** userConnectionManager اضافه شد ***
        {
            _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _userConnectionManager = userConnectionManager ?? throw new ArgumentNullException(nameof(userConnectionManager)); // *** مقداردهی شد ***
            _userService = userService;
        }

        // اینترفیس برای کلاینت (متدهایی که سرور روی کلاینت فراخوانی می‌کند)
        public interface IChatClient
        {
            Task ReceiveMessage(ChatMessageDto message);
            Task LoadContacts(IEnumerable<ChatContactDto> contacts);
            Task LoadHistory(IEnumerable<ChatMessageDto> messages, string conversationId);
            Task MessageDeleted(string messageId, string conversationId);
            Task ReceiveStatusUpdate(string userId, bool isOnline, bool isTyping, string? statusText, DateTime? lastSeen); // lastSeen اضافه شد
            Task HubError(string errorMessage);
            Task MessageSentConfirmation(ChatMessageDto confirmedMessage);
            Task NotifyNewMessage(string senderUserId, string senderName);
        }


        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier; // از ClaimTypes.NameIdentifier می‌آید
            var connectionId = Context.ConnectionId;
            var userName = Context.User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? "کاربر";
            // نقش کاربر را مستقیما از Claims یا Context بگیرید
            bool isAdmin = Context.User?.IsInRole("Admin") ?? false; // فرض وجود نقش "Admin"

            if (string.IsNullOrEmpty(userId))
            {
                Console.WriteLine($"Connection attempt rejected: User ID is missing. ConnectionId: {connectionId}");
                Context.Abort();
                return;
            }

            // ثبت اتصال در سرویس مدیریت اتصال
            await _userConnectionManager.AddConnection(userId, connectionId);

            if (isAdmin)
            {
                Console.WriteLine($"Admin connected: {userName} ({userId}), Connection: {connectionId}");
                // TODO: ارسال لیست چت‌های فعال به ادمین؟ (LoadInitialContacts این کار را می‌کند)
            }
            else
            {
                Console.WriteLine($"User connected: {userName} ({userId}), Connection: {connectionId}");
                // به ادمین(های) آنلاین اطلاع بده کاربر وصل شد
                await NotifyAdminsUserStatus(userId, true, false, "آنلاین");
            }

            // به سایر کانکشن‌های همین کاربر اطلاع بده وضعیت آنلاین است
            var otherConnectionIds = (await _userConnectionManager.GetConnectionIds(userId))
                                     .Where(cid => cid != connectionId)
                                     .ToList();
            if (otherConnectionIds.Any())
            {
                await Clients.Clients(otherConnectionIds).ReceiveStatusUpdate(userId, true, false, "آنلاین", null);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var connectionId = Context.ConnectionId;
            var userName = Context.User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? "کاربر"; // برای لاگ

            // حذف اتصال و گرفتن UserId مربوطه از سرویس
            string? userId = await _userConnectionManager.RemoveConnection(connectionId);

            if (!string.IsNullOrEmpty(userId))
            {
                bool isAdmin = await IsUserAdmin(userId); // بررسی نقش از دیتابیس

                if (isAdmin)
                {
                    Console.WriteLine($"Admin disconnected: {userName} ({userId}), Connection: {connectionId}");
                }
                else
                {
                    Console.WriteLine($"User connection removed: {userName} ({userId}), Connection: {connectionId}");
                }

                // آیا کاربر کلا آفلاین شد؟
                bool stillOnline = await _userConnectionManager.IsUserOnline(userId);

                if (!stillOnline)
                {
                    Console.WriteLine($"User truly disconnected: {userName} ({userId}) is now offline.");
                    var lastSeenTime = DateTime.UtcNow; // زمان قطع اتصال نهایی
                    // اگر کاربر عادی بود و آفلاین شد، به ادمین‌ها اطلاع بده
                    if (!isAdmin)
                    {
                        await NotifyAdminsUserStatus(userId, false, false, "آفلاین", lastSeenTime); // ارسال وضعیت آفلاین با زمان
                    }
                    // TODO: آپدیت زمان آخرین بازدید در دیتابیس؟
                }
                // else: User still has other connections online.
            }
            else
            {
                Console.WriteLine($"Disconnected connection without associated user: {connectionId}");
            }


            if (exception != null)
            {
                Console.WriteLine($"Disconnection reason for {connectionId}: {exception.Message}");
            }

            await base.OnDisconnectedAsync(exception);
        }

        [HubMethodName("LoadInitialContacts")]
        public async Task LoadInitialContacts()
        {
            var userId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(userId)) { await Clients.Caller.HubError("شناسه کاربری نامعتبر است."); return; }

            // نقش کاربر را مستقیما بگیرید
            bool isAdmin = Context.User?.IsInRole("Admin") ?? false;

            try
            {
                // سرویس چت خودش وضعیت آنلاین بودن را با کمک UserConnectionManager بررسی می‌کند
                var contacts = await _chatService.GetInitialContactsForUserAsync(userId, isAdmin);
                await Clients.Caller.LoadContacts(contacts);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading initial contacts for {userId}: {ex.Message}\n{ex.StackTrace}");
                await Clients.Caller.HubError("خطا در بارگذاری لیست مخاطبین.");
            }
        }

        [HubMethodName("LoadChatHistory")]
        public async Task LoadChatHistory(string otherUserId)
        {
            var userId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(otherUserId)) { await Clients.Caller.HubError("شناسه‌های کاربری نامعتبر هستند."); return; }

            try
            {
                var history = await _chatService.GetChatHistoryAsync(userId, otherUserId);
                string conversationId = GenerateConversationId(userId, otherUserId);
                await Clients.Caller.LoadHistory(history, conversationId);
                // TODO: Mark messages as read for userId viewing history of otherUserId
                // await _chatService.MarkMessagesAsReadAsync(otherUserId, userId); // (Sender, Recipient)
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading chat history between {userId} and {otherUserId}: {ex.Message}\n{ex.StackTrace}");
                await Clients.Caller.HubError("خطا در بارگذاری تاریخچه گفتگو.");
            }
        }


        public async Task<ChatMessageDto?> SendMessage(string message, string recipientUserId, string? replyToMessageId = null)
        {
            var senderUserId = Context.UserIdentifier; // ID کاربر فرستنده از Context
            var senderConnectionId = Context.ConnectionId;
            // اسم کاربر رو هم بگیریم (فرض می‌کنیم در Claims هست)
            var senderName = Context.User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? "کاربر ناشناس";

            // --- بررسی‌های اولیه ---
            if (string.IsNullOrWhiteSpace(message))
            {
                await Clients.Caller.HubError("متن پیام نمی‌تواند خالی باشد.");
                return null; // بازگرداندن null در صورت خطا
            }
            if (string.IsNullOrEmpty(senderUserId))
            {
                // این حالت معمولا نباید رخ بده اگر هاب Authorize شده باشه
                await Clients.Caller.HubError("شناسه فرستنده نامعتبر است.");
                return null;
            }
            if (string.IsNullOrEmpty(recipientUserId))
            {
                await Clients.Caller.HubError("شناسه گیرنده مشخص نشده است.");
                return null;
            }

            // --- بررسی دسترسی (آیا کاربر حق ارسال پیام به این گیرنده را دارد؟) ---
            bool senderIsAdmin = Context.User?.IsInRole("Admin") ?? false;
            bool recipientIsAdmin = await IsUserInRoleAsync(recipientUserId, "Admin");

            // قانون: کاربر عادی فقط به ادمین می‌تواند پیام دهد
            if (!senderIsAdmin && !recipientIsAdmin)
            {
                await Clients.Caller.HubError("شما فقط می‌توانید به تیم پشتیبانی پیام ارسال کنید.");
                return null;
            }
            // قانون: ادمین نمی‌تواند به خودش پیام دهد؟ (اختیاری)
            // if (senderIsAdmin && senderUserId == recipientUserId) { ... }

            try
            {
                // 1. ذخیره پیام در دیتابیس توسط سرویس
                var savedMessageDto = await _chatService.SaveMessageAsync(senderUserId, senderName, recipientUserId, message, replyToMessageId);

                // بررسی نتیجه ذخیره‌سازی (ممکنه سرویس null برگردونه)
                if (savedMessageDto == null || string.IsNullOrEmpty(savedMessageDto.MessageId))
                {
                    Console.WriteLine($"Error: SaveMessageAsync returned null or invalid DTO for sender {senderUserId} to {recipientUserId}.");
                    await Clients.Caller.HubError("خطا در ذخیره‌سازی پیام در سرور.");
                    return null;
                }

                // 2. ارسال پیام به کلاینت گیرنده (اگر آنلاین است)
                var recipientConnectionIds = await _userConnectionManager.GetConnectionIds(recipientUserId);
                if (recipientConnectionIds.Any())
                {
                    // *** ساخت DTO جدید برای گیرنده با IsSender = false ***
                    var messageForRecipient = new ChatMessageDto
                    {
                        MessageId = savedMessageDto.MessageId,
                        SenderUserId = savedMessageDto.SenderUserId,
                        SenderName = savedMessageDto.SenderName,
                        RecipientUserId = savedMessageDto.RecipientUserId,
                        Content = savedMessageDto.Content,
                        Timestamp = savedMessageDto.Timestamp,
                        IsRead = false, // موقع ارسال اولیه، خوانده نشده است
                        IsSender = false, // *** مهم: برای گیرنده false است ***
                        ReplyToMessageId = savedMessageDto.ReplyToMessageId,
                        ReplyToText = savedMessageDto.ReplyToText,
                        SenderAvatar = savedMessageDto.SenderAvatar,
                        ReplyToSenderName = savedMessageDto.ReplyToSenderName
                    };
                    // ارسال به تمام کانکشن‌های فعال گیرنده
                    await Clients.Clients(recipientConnectionIds).ReceiveMessage(messageForRecipient);
                }
                else
                {
                    Console.WriteLine($"User {recipientUserId} is offline. Message saved but not delivered via SignalR.");
                    // اینجا می‌تونید منطق ارسال نوتیفیکیشن آفلاین (مثل ایمیل یا پوش نوتیفیکیشن) رو اضافه کنید
                }

                // 3. تنظیم DTO برای بازگشت به فرستنده (با IsSender = true)
                savedMessageDto.IsSender = true;

                // 4. ارسال نوتیفیکیشن به ادمین‌ها (اگر لازم است، مثل قبل)
                if (!senderIsAdmin && recipientIsAdmin)
                {
                    // به همه ادمین‌های آنلاین خبر بده که پیام جدیدی از کاربر عادی آمده
                    await NotifyAdminsAboutNewMessage(senderUserId, senderName, savedMessageDto.MessageId);
                }
                // (اختیاری) اگر ادمین به کاربر پیام داد، به سایر ادمین‌ها خبر بدهیم؟
                // if (senderIsAdmin && !recipientIsAdmin) { ... }


                // *** 5. بازگرداندن پیام ذخیره شده به کلاینت فرستنده (invoke کننده) ***
                return savedMessageDto;
            }
            catch (Exception ex)
            {
                // لاگ کردن خطای دقیق در سرور
                Console.WriteLine($"CRITICAL Error in SendMessage from {senderUserId} to {recipientUserId}. Message: {ex.Message}\nStackTrace: {ex.StackTrace}");
                // ارسال خطای عمومی به کلاینت
                await Clients.Caller.HubError("یک خطای پیش‌بینی نشده در سرور هنگام ارسال پیام رخ داد.");
                // بازگرداندن null به نشانه شکست عملیات
                return null;
            }
        }

        // متد کمکی برای بررسی نقش کاربر (با استفاده از UserManager)
        private async Task<bool> IsUserInRoleAsync(string userId, string roleName)
        {
            if (string.IsNullOrEmpty(userId)) return false;
            return await _userService.IsUserInRoleAsync(userId, roleName);
        }

        // متد کمکی برای ارسال نوتیفیکیشن به ادمین‌های آنلاین
        private async Task NotifyAdminsAboutNewMessage(string senderUserId, string senderName, string newMessageId)
        {
            // *** تغییر کرد: گرفتن ادمین‌ها از UserService ***
            var adminUsers = await _userService.GetUsersInRoleAsync("Admin");
            var adminConnectionIds = new List<string>();

            foreach (var admin in adminUsers)
            {
                // اطمینان از اینکه User entity پراپرتی Id داره
                if (admin != null && !string.IsNullOrEmpty(admin.Id))
                {
                    // به خود فرستنده (اگر ادمین بود) نوتیفیکیشن ندهیم؟ (اینجا فرستنده کاربر عادی است)
                    adminConnectionIds.AddRange(await _userConnectionManager.GetConnectionIds(admin.Id));
                }
            }

            if (adminConnectionIds.Any())
            {
                // *** این خط فعلا کامنت شد تا خطای زمان کامپایل رفع بشه ***
                // TODO: Define 'AdminReceiveNewMessageNotification' on the client-side (JavaScript)
                //       and uncomment the line below to enable real-time admin notifications.
                /*
                await Clients.Clients(adminConnectionIds.Distinct()).AdminReceiveNewMessageNotification(new {
                     fromUserId = senderUserId,
                     fromUserName = senderName,
                     messageId = newMessageId,
                     notificationText = $"پیام جدیدی از {senderName} دریافت شد."
                 });
                */
                Console.WriteLine($"Would notify admins on connections: {string.Join(",", adminConnectionIds.Distinct())}"); // لاگ موقت
            }
        }

        // متد برای حذف پیام
        [HubMethodName("DeleteMessage")]
        public async Task DeleteMessage(string messageId)
        {
            var userId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(messageId)) return;

            try
            {
                // سرویس باید منطق حذف و بررسی مالکیت را انجام دهد
                var result = await _chatService.DeleteMessageAsync(messageId, userId); // فرض کنید سرویس اطلاعات لازم را برمی‌گرداند

                if (result.Success)
                {
                    // به همه کلاینت‌های مرتبط (فرستنده و گیرنده) و ادمین‌ها اطلاع بده
                    string conversationId = GenerateConversationId(result.SenderUserId, result.RecipientUserId);
                    List<string> connectionsToNotify = new List<string>();

                    // اتصالات فرستنده و گیرنده
                    connectionsToNotify.AddRange(await _userConnectionManager.GetConnectionIds(result.SenderUserId));
                    connectionsToNotify.AddRange(await _userConnectionManager.GetConnectionIds(result.RecipientUserId));

                    // اتصالات ادمین‌ها
                    connectionsToNotify.AddRange(await GetOnlineAdminConnectionIds());

                    if (connectionsToNotify.Any())
                    {
                        await Clients.Clients(connectionsToNotify.Distinct()).MessageDeleted(messageId, conversationId);
                        Console.WriteLine($"Notified clients about deletion of message {messageId}");
                    }
                }
                else
                {
                    await Clients.Caller.HubError(result.ErrorMessage ?? "پیام یافت نشد یا شما مجاز به حذف آن نیستید.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting message {messageId} by user {userId}: {ex.Message}\n{ex.StackTrace}");
                await Clients.Caller.HubError("خطا در حذف پیام.");
            }
        }

        // متد برای به‌روزرسانی وضعیت تایپ کردن
        [HubMethodName("UpdateTypingStatus")]
        public async Task UpdateTypingStatus(string recipientUserId, bool isTyping)
        {
            var senderUserId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(senderUserId) || string.IsNullOrEmpty(recipientUserId)) return;

            bool senderIsAdmin = Context.User?.IsInRole("Admin") ?? false;

            // 1. ارسال وضعیت تایپ به گیرنده اصلی
            var recipientConnectionIds = await _userConnectionManager.GetConnectionIds(recipientUserId);
            if (recipientConnectionIds.Any())
            {
                string statusText = isTyping ? "در حال نوشتن..." : "آنلاین"; // متن وضعیت پایه
                await Clients.Clients(recipientConnectionIds).ReceiveStatusUpdate(senderUserId, true, isTyping, statusText, null); // isOnline=true چون در حال تایپ است
            }

            // 2. اگر کاربر به ادمین تایپ می‌کند، به همه ادمین‌ها اطلاع بده (به جز خود فرستنده اگر ادمین بود)
            if (!senderIsAdmin && await IsRecipientAdmin(recipientUserId))
            {
                var adminConnectionIds = await GetOnlineAdminConnectionIds();
                if (adminConnectionIds.Any())
                {
                    string statusTextForAdmin = isTyping ? "در حال نوشتن..." : null; // برای ادمین، وقتی تایپ تمام شد، null بفرستیم تا وضعیت قبلی (آنلاین/آفلاین) نمایش داده شود
                    await Clients.Clients(adminConnectionIds).ReceiveStatusUpdate(senderUserId, true, isTyping, statusTextForAdmin, null);
                }
            }
            // 3. اگر ادمین به کاربر تایپ می‌کند، به سایر ادمین‌ها هم اطلاع بده؟ (اختیاری)
            if (senderIsAdmin && !await IsRecipientAdmin(recipientUserId))
            {
                var otherAdminConnectionIds = (await GetOnlineAdminConnectionIds()).Where(cid => cid != Context.ConnectionId).ToList();
                if (otherAdminConnectionIds.Any())
                {
                    // می‌توانید یک وضعیت خاص مثل "ادمین در حال پاسخ..." بفرستید
                    // await Clients.Clients(otherAdminConnectionIds).ReceiveStatusUpdate(recipientUserId, true, isTyping, $"ادمین {senderUserId} در حال نوشتن...", null);
                }
            }
        }


        // ---=== متدهای کمکی ===---

        // اطلاع رسانی وضعیت آنلاین/آفلاین/تایپ یک کاربر به همه ادمین‌های آنلاین
        private async Task NotifyAdminsUserStatus(string targetUserId, bool isOnline, bool isTyping, string? statusText, DateTime? lastSeen = null)
        {
            var adminConnectionIds = await GetOnlineAdminConnectionIds();

            if (adminConnectionIds.Any())
            {
                await Clients.Clients(adminConnectionIds).ReceiveStatusUpdate(targetUserId, isOnline, isTyping, statusText, lastSeen);
                Console.WriteLine($"Notified {adminConnectionIds.Count()} admin(s) about user {targetUserId} status: Online={isOnline}, Typing={isTyping}, Text='{statusText}', LastSeen='{lastSeen}'");
            }
        }

        // اطلاع‌رسانی به ادمین‌ها درباره پیام جدید از یک کاربر
        private async Task NotifyAdminsAboutNewMessage(string senderUserId, string senderName)
        {
            var adminConnectionIds = await GetOnlineAdminConnectionIds();
            if (adminConnectionIds.Any())
            {
                await Clients.Clients(adminConnectionIds).NotifyNewMessage(senderUserId, senderName);
                Console.WriteLine($"Notified {adminConnectionIds.Count()} admin(s) about new message from {senderName} ({senderUserId})");
            }
        }


        // گرفتن لیست ConnectionId ادمین‌های آنلاین
        private async Task<List<string>> GetOnlineAdminConnectionIds()
        {
            List<string> adminConnectionIds = new List<string>();
            var onlineUserIds = await _userConnectionManager.GetOnlineUserIds();
            foreach (var userId in onlineUserIds)
            {
                if (await IsUserAdmin(userId)) // نقش را از دیتابیس چک کن
                {
                    adminConnectionIds.AddRange(await _userConnectionManager.GetConnectionIds(userId));
                }
            }
            return adminConnectionIds.Distinct().ToList();
        }


        // بررسی اینکه آیا یک کاربر ادمین است یا خیر (بر اساس دیتابیس)
        private async Task<bool> IsUserAdmin(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return false;
            // *** این نیاز به فیلد IsAdmin یا جدول نقش در انتیتی User شما دارد ***
            // کش کردن نتیجه این کوئری می‌تواند مفید باشد اگر تعداد ادمین‌ها کم و نقش‌ها ثابت است
            var user = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId && !u.IsDelete);
            return user?.IsAdmin ?? false; // فرض بر وجود فیلد IsAdmin
        }

        // برای متد SendMessage که گیرنده را چک می‌کند (معمولا همان IsUserAdmin است)
        private async Task<bool> IsRecipientAdmin(string recipientUserId)
        {
            return await IsUserAdmin(recipientUserId);
        }


        // ساخت شناسه یکتا و پایدار برای مکالمه بین دو کاربر
        private string GenerateConversationId(string userId1, string userId2)
        {
            if (string.IsNullOrEmpty(userId1) || string.IsNullOrEmpty(userId2))
                throw new ArgumentException("User IDs cannot be null or empty for generating conversation ID.");

            return string.CompareOrdinal(userId1, userId2) < 0
                ? $"{userId1}_{userId2}"
                : $"{userId2}_{userId1}";
        }

        // استخراج userId ها از conversationId (برای DeleteMessage)
        // این تابع دیگر مستقیما در هاب لازم نیست چون سرویس حذف باید این اطلاعات را برگرداند
        // private string[] ParseConversationId(string conversationId)
        // {
        //     return conversationId.Split('_');
        // }


    } // *** بستن کلاس ChatHub ***
}
