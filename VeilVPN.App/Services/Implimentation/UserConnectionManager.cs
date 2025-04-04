using System.Collections.Concurrent;
using VeilVPN.App.Services.Interfaces;

namespace VeilVPN.App.Services.Implimentation
{
    /// <summary>
    /// پیاده‌سازی پیش‌فرض IUserConnectionManager با استفاده از ConcurrentDictionary.
    /// </summary>
    public class UserConnectionManager : IUserConnectionManager
    {
        // UserId -> Set of ConnectionIds (using ConcurrentDictionary as a Concurrent Set)
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _userConnections = new();

        // ConnectionId -> UserId
        private readonly ConcurrentDictionary<string, string> _connectionUsers = new();

        /// <summary>
        /// یک اتصال جدید برای یک کاربر مشخص را ثبت می‌کند.
        /// </summary>
        public Task AddConnection(string userId, string connectionId)
        {
            // نگاشت ConnectionId به UserId
            _connectionUsers[connectionId] = userId;

            // دریافت یا ایجاد مجموعه اتصالات برای کاربر
            var userConnectionSet = _userConnections.GetOrAdd(userId, _ => new ConcurrentDictionary<string, byte>());

            // اضافه کردن ConnectionId به مجموعه اتصالات کاربر (مقدار byte اهمیتی ندارد)
            userConnectionSet.TryAdd(connectionId, 0);

            Console.WriteLine($"---> Connection Added: User '{userId}', Connection '{connectionId}'. Total online users: {_userConnections.Count}"); // لاگ برای دیباگ

            return Task.CompletedTask;
        }

        /// <summary>
        /// یک اتصال را بر اساس شناسه اتصال حذف می‌کند.
        /// </summary>
        public Task<string?> RemoveConnection(string connectionId)
        {
            string? userId = null;
            // ابتدا نگاشت ConnectionId به UserId را حذف کن و UserId را بگیر
            if (_connectionUsers.TryRemove(connectionId, out userId))
            {
                // اگر UserId معتبر بود، سعی کن مجموعه اتصالاتش را پیدا کنی
                if (_userConnections.TryGetValue(userId, out var userConnectionSet))
                {
                    // ConnectionId را از مجموعه اتصالات کاربر حذف کن
                    userConnectionSet.TryRemove(connectionId, out _);

                    // اگر مجموعه اتصالات کاربر خالی شد، خود کاربر را هم از دیکشنری اصلی حذف کن (برای جلوگیری از نشت حافظه)
                    if (userConnectionSet.IsEmpty)
                    {
                        _userConnections.TryRemove(userId, out _);
                        Console.WriteLine($"---> User Removed due to last connection: User '{userId}'. Total online users: {_userConnections.Count}"); // لاگ برای دیباگ
                    }
                    else
                    {
                        Console.WriteLine($"---> Connection Removed: User '{userId}', Connection '{connectionId}'. Remaining connections for user: {userConnectionSet.Count}. Total online users: {_userConnections.Count}"); // لاگ برای دیباگ
                    }
                }
            }
            else
            {
                Console.WriteLine($"---> RemoveConnection Failed: Connection '{connectionId}' not found."); // لاگ برای دیباگ
            }

            // UserId کاربری که اتصالش قطع شد را برگردان
            return Task.FromResult(userId);
        }

        /// <summary>
        /// بررسی می‌کند که آیا کاربر مشخصی حداقل یک اتصال فعال دارد یا خیر.
        /// </summary>
        public Task<bool> IsUserOnline(string userId)
        {
            // کاربر آنلاین است اگر در دیکشنری _userConnections وجود داشته باشد
            // و مجموعه اتصالاتش خالی نباشد.
            bool isOnline = _userConnections.TryGetValue(userId, out var userConnectionSet) && !userConnectionSet.IsEmpty;
            return Task.FromResult(isOnline);
        }

        /// <summary>
        /// تمام شناسه‌های اتصال فعال برای یک کاربر مشخص را برمی‌گرداند.
        /// </summary>
        public Task<IEnumerable<string>> GetConnectionIds(string userId)
        {
            if (_userConnections.TryGetValue(userId, out var userConnectionSet))
            {
                // یک کپی از کلیدها (ConnectionIds) برگردانده می‌شود تا thread-safe باشد
                return Task.FromResult<IEnumerable<string>>(userConnectionSet.Keys.ToList());
            }
            // اگر کاربر یافت نشد، لیست خالی برگردان
            return Task.FromResult(Enumerable.Empty<string>());
        }

        /// <summary>
        /// لیست شناسه‌های تمام کاربرانی که در حال حاضر آنلاین هستند را برمی‌گرداند.
        /// </summary>
        public Task<IEnumerable<string>> GetOnlineUserIds()
        {
            // یک کپی از کلیدهای دیکشنری اصلی (UserIds) برگردانده می‌شود
            return Task.FromResult<IEnumerable<string>>(_userConnections.Keys.ToList());
        }
    }
}
