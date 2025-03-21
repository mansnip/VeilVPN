using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Services.Interfaces;
using Domain.ViewModels.UserPanel;
using System;
using System.Threading.Tasks;

namespace VeilVPN.App.Areas.UserPanel.Controllers
{
    [Area("UserPanel")]
    [Authorize]
    public class SubscriptionController : Controller
    {
        private readonly ISubscriptionService _subscriptionService;

        public SubscriptionController(ISubscriptionService subscriptionService)
        {
            _subscriptionService = subscriptionService;
        }

        // نمایش صفحه اصلی اشتراک
        public IActionResult Index()
        {
            return View();
        }

        // نمایش اشتراک‌های کاربر
        public async Task<IActionResult> MySubscriptions()
        {
            var userId = User.Identity.Name;
            var subscriptions = await _subscriptionService.GetUserSubscriptionsAsync(userId);
            return View(subscriptions);
        }

        // نمایش وضعیت اشتراک فعال
        public async Task<IActionResult> Status()
        {
            var userId = User.Identity.Name;
            var status = await _subscriptionService.GetUserActiveSubscriptionAsync(userId);
            return View(status);
        }

        [HttpGet]
        public async Task<IActionResult> GetUsageHistory()
        {
            var userId = User.Identity.Name;
            var usageHistory = await _subscriptionService.GetUsageHistoryAsync(userId);
            return Json(usageHistory);
        }

        // اکشن برای محاسبه قیمت (برای درخواست‌های Ajax)
        [HttpPost]
        public async Task<IActionResult> CalculatePrice([FromBody] CalculatePriceModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { error = "مقادیر ورودی نامعتبر است" });
            }

            try
            {
                // محاسبه قیمت با استفاده از سرویس
                int price = await _subscriptionService.CalculatePrice(model.Traffic, model.Duration);

                // محاسبه درصد تخفیف
                double discount = _subscriptionService.CalculateDiscount(model.Duration);

                return Json(new
                {
                    price = price,
                    discount = discount * 100, // تبدیل به درصد
                    success = true
                });
            }
            catch (Exception ex)
            {
                // ثبت خطا در لاگ
                return StatusCode(500, new { error = "خطا در محاسبه قیمت. لطفاً دوباره تلاش کنید" });
            }
        }

        // اکشن برای محاسبه جزئیات قیمت
        [HttpPost]
        public async Task<IActionResult> CalculateDetailedPrice([FromBody] CalculatePriceModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { error = "مقادیر ورودی نامعتبر است" });
            }

            try
            {
                var details = await _subscriptionService.CalculateDetailedPrice(model.Traffic, model.Duration);
                return Json(new
                {
                    basePrice = details.BasePrice,
                    discountPercent = details.DiscountPercent,
                    discountAmount = details.DiscountAmount,
                    finalPrice = details.FinalPrice,
                    success = true
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "خطا در محاسبه قیمت. لطفاً دوباره تلاش کنید" });
            }
        }
    }
}
