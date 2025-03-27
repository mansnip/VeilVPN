using DataLayer.Context;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DataLayer.Repositories
{
    public class SubscriptionRepository : ISubscriptionRepository
    {
        private readonly VeilVpnDbContext _context;

        public SubscriptionRepository(VeilVpnDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }


        public async Task<Subscription> GetActiveSubscriptionByUserIdAsync(string userId)
        {
            var now = DateTime.Now;
            return await _context.Subscriptions
                .Include(s => s.Invoice)
                .Where(s => s.UserId == userId &&
                            s.IsActive &&
                            s.StartDate <= now &&
                            s.EndDate >= now &&
                            s.RemainingTraffic > 0)
                .OrderByDescending(s => s.EndDate)
                .FirstOrDefaultAsync();
        }

        public async Task<Subscription> GetByIdAsync(string id)
        {
            return await _context.Subscriptions
                .Include(s => s.User)
                .Include(s => s.Invoice)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<List<Subscription>> GetUserSubscriptionsAsync(string userId)
        {
            return await _context.Subscriptions
                .Include(s => s.Invoice)
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.StartDate)
                .ToListAsync();
        }

        public async Task<Subscription> CreateAsync(Subscription subscription)
        {
            if (subscription == null)
                throw new ArgumentNullException(nameof(subscription));

            await _context.Subscriptions.AddAsync(subscription);
            await _context.SaveChangesAsync();
            return subscription;
        }

        public async Task<bool> UpdateAsync(Subscription subscription)
        {
            try
            {
                _context.Subscriptions.Update(subscription);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> IsUserHasActiveSubscriptionAsync(string userId)
        {
            return await _context.Subscriptions
                .AnyAsync(s => s.UserId == userId &&
                              s.IsActive &&
                              s.EndDate > DateTime.Now &&
                              s.RemainingTraffic > 0);
        }

        public async Task<Subscription> GetUserActiveSubscriptionAsync(string userId)
        {
            return await _context.Subscriptions
                .Include(s => s.Invoice)
                .Where(s => s.UserId == userId &&
                           s.IsActive &&
                           s.EndDate > DateTime.Now)
                .OrderByDescending(s => s.EndDate)
                .FirstOrDefaultAsync();
        }

        public async Task<Subscription> GetSubscriptionById(string subscriptionId)
        {
            var xxx = await _context.Subscriptions.FirstOrDefaultAsync(a => a.Id == subscriptionId);
            if (xxx == null)
            {
                xxx = new Subscription();
            }
            return xxx;
        }

        // اضافه کردن متد جدید برای دریافت تمام اشتراک‌ها همراه با اطلاعات کاربر
        public async Task<List<Subscription>> GetAllWithUserAsync()
        {
            return await _context.Subscriptions
                .Include(s => s.User)
                .Include(s => s.Invoice)
                .OrderByDescending(s => s.StartDate)
                .ToListAsync();
        }

        // اضافه کردن متد جدید برای دریافت اشتراک با اطلاعات کاربر و سرور
        public async Task<Subscription> GetSubscriptionWithUserAndServerAsync(string id)
        {
            return await _context.Subscriptions
                .Include(s => s.User)
                .Include(s => s.Invoice)
                .FirstOrDefaultAsync(s => s.Id == id);
        }
    }
}