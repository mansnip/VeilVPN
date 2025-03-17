using Domain.ViewModels.UserPanel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Interfaces
{
    // سرویس
    public interface ISubscriptionService
    {
        Task<decimal> CalculatePriceAsync(int traffic, int duration);
        Task<bool> CreateSubscriptionAsync(SubscriptionModel model);
        // سایر متدهای مرتبط با اشتراک
    }

    public class SubscriptionService : ISubscriptionService
    {
        public Task<decimal> CalculatePriceAsync(int traffic, int duration)
        {
            // منطق محاسبه قیمت
            decimal trafficCost = traffic * 2500;
            decimal durationCost = duration * 1500;

            decimal finalPrice = trafficCost + durationCost;

            // اعمال تخفیف
            decimal discount = 0;

            if (duration >= 300 && duration <= 365)
                discount = 0.20m;
            else if (duration >= 180 && duration < 300)
                discount = 0.15m;
            else if (duration >= 90 && duration < 180)
                discount = 0.10m;

            finalPrice = finalPrice * (1 - discount);
            finalPrice = Math.Round(finalPrice / 1000) * 1000;

            return Task.FromResult(finalPrice);
        }

        public Task<bool> CreateSubscriptionAsync(SubscriptionModel model)
        {
            // منطق ایجاد اشتراک...
            // ذخیره در دیتابیس و غیره

            return Task.FromResult(true); // موفقیت‌آمیز بودن عملیات
        }
    }

}
