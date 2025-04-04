using Domain.DTOs.VPN;
using Domain.Entities;
using Domain.ViewModels.UserPanel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Interfaces
{
    public interface ISubscriptionService
    {
        Task<int> CalculatePrice(int traffic, int duration);
        Task<SubscriptionPriceDetails> CalculateDetailedPrice(int traffic, int duration);
        double CalculateDiscount(int duration);

        // متد جدید
        Task<(bool success, string Message)> CreateSubscriptionFromInvoiceAsync(string invoiceId);
        Task<List<SubscriptionViewModel>> GetUserSubscriptionsAsync(string userId);
        Task<SubscriptionStatusViewModel> GetUserSubscriptionStatus(string userId);
        // متد جدید برای دریافت اشتراک فعال کاربر
        Task<SubscriptionnViewModel> GetUserActiveSubscriptionAsync(string userId);

        // متد جدید برای دریافت تاریخچه مصرف
        Task<List<UsageHistoryViewModel>> GetUsageHistoryAsync(string userId);
        Task<Subscription> GetSubscriptionById(string subscriptionId);

        Task<SubscriptionViewModel?> GetSubscriptionDetailsForAdminAsync(string subscriptionId); // <<< متد جدید

    }

}