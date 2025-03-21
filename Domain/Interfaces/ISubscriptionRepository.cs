using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface ISubscriptionRepository
    {
        Task<Subscription> GetByIdAsync(string id);
        Task<List<Subscription>> GetUserSubscriptionsAsync(string userId);
        Task<Subscription> CreateAsync(Subscription subscription);
        Task<bool> UpdateAsync(Subscription subscription);
        Task<bool> IsUserHasActiveSubscriptionAsync(string userId);
        Task<Subscription> GetUserActiveSubscriptionAsync(string userId);
        Task<Subscription> GetActiveSubscriptionByUserIdAsync(string userId);
    }
}