using DataLayer.Context;
using Domain.DTOs.Chat;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using VeilVPN.App.Services.Implimentation;

namespace VeilVPN.App.Services.Interfaces
{
    public interface IChatService
    {
        /// <summary>
        /// اینترفیس سرویس مدیریت عملیات چت، شامل دریافت مخاطبین، تاریخچه، ذخیره و حذف پیام‌ها.
        /// </summary>
        Task<IEnumerable<ChatContactDto>> GetInitialContactsForUserAsync(string userId, bool isAdmin);
        Task<IEnumerable<ChatMessageDto>> GetChatHistoryAsync(string userId1, string userId2);
        Task<ChatMessageDto> SaveMessageAsync(string senderUserId, string senderName, string recipientUserId, string message, string? replyToMessageId);
        Task<DeleteMessageResult> DeleteMessageAsync(string messageId, string requestingUserId);
        Task MarkMessagesAsReadAsync(string senderUserId, string readerUserId);
    }
}
