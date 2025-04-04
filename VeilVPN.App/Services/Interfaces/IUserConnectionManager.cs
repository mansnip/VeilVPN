namespace VeilVPN.App.Services.Interfaces
{
    /// <summary>
    /// سرویسی برای مدیریت و رهگیری اتصالات SignalR کاربران.
    /// این سرویس باید به صورت Singleton رجیستر شود.
    /// </summary>
    public interface IUserConnectionManager
    {
        /// <summary>
        /// یک اتصال جدید برای یک کاربر مشخص را ثبت می‌کند.
        /// </summary>
        /// <param name="userId">شناسه کاربر.</param>
        /// <param name="connectionId">شناسه اتصال SignalR.</param>
        Task AddConnection(string userId, string connectionId);

        /// <summary>
        /// یک اتصال را بر اساس شناسه اتصال حذف می‌کند.
        /// </summary>
        /// <param name="connectionId">شناسه اتصال SignalR.</param>
        /// <returns>شناسه کاربری که اتصالش قطع شد، یا null اگر اتصال یافت نشد.</returns>
        Task<string?> RemoveConnection(string connectionId);

        /// <summary>
        /// بررسی می‌کند که آیا کاربر مشخصی حداقل یک اتصال فعال دارد یا خیر.
        /// </summary>
        /// <param name="userId">شناسه کاربر.</param>
        /// <returns>True اگر کاربر آنلاین است، در غیر این صورت False.</returns>
        Task<bool> IsUserOnline(string userId);

        /// <summary>
        /// تمام شناسه‌های اتصال فعال برای یک کاربر مشخص را برمی‌گرداند.
        /// </summary>
        /// <param name="userId">شناسه کاربر.</param>
        /// <returns>لیستی از شناسه‌های اتصال یا یک لیست خالی.</returns>
        Task<IEnumerable<string>> GetConnectionIds(string userId);

        /// <summary>
        /// لیست شناسه‌های تمام کاربرانی که در حال حاضر آنلاین هستند را برمی‌گرداند.
        /// </summary>
        /// <returns>لیستی از شناسه‌های کاربران آنلاین.</returns>
        Task<IEnumerable<string>> GetOnlineUserIds();
    }
}
