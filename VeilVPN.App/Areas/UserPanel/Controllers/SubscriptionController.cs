using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Services.Interfaces;
using Domain.ViewModels.UserPanel;
using Domain.DTOs.VPN;
using System.Security.Claims;
using Domain.DTOs.Subscription;

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


        [HttpGet]
        public async Task<IActionResult> GetLiveUsageForAllSubscriptions()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Json(new { success = false, message = "کاربر یافت نشد." });
            }

            try
            {
                // *** فراخوانی متد موجود شما که اطلاعات زنده را می‌گیرد ***
                List<SubscriptionViewModel> subscriptions = await _subscriptionService.GetUserSubscriptionsAsync(userId);

                // تبدیل به DTO کوچک‌تر برای ارسال به کلاینت
                var liveUsageData = subscriptions.Select(sub => new LiveUsageUpdateViewModel
                {
                    Id = sub.Id,
                    RemainingTraffic = sub.HasVpnConnection ? sub.VpnRemainingTraffic : sub.RemainingTraffic, // استفاده از دیتای سرور اگر موجود بود
                    // محاسبه مصرف بر اساس دیتای زنده سرور (اگر موجود بود)
                    UsedTraffic = sub.HasVpnConnection && sub.Traffic > 0 ? Math.Max(0, sub.Traffic - sub.VpnRemainingTraffic) : Math.Max(0, sub.Traffic - sub.RemainingTraffic),
                    UsagePercentage = sub.HasVpnConnection && sub.Traffic > 0 ? sub.VpnUsagePercentage : (sub.Traffic > 0 ? (int)Math.Max(0, Math.Min(100, ((double)(sub.Traffic - sub.RemainingTraffic) / sub.Traffic) * 100)) : 0),
                    StatusText = sub.StatusText, // وضعیت محاسبه‌شده در ویومدل
                    StatusClass = sub.StatusClass, // کلاس وضعیت محاسبه‌شده
                    IsVpnActive = sub.IsVpnActive,
                    VpnRemainingDays = sub.VpnRemainingDays,
                    HasVpnConnection = sub.HasVpnConnection,
                    Traffic = sub.Traffic, // ارسال ترافیک کل برای بررسی نامحدود بودن در JS
                    // محاسبه شرط نمایش دکمه تمدید (همان منطقی که در MySubscriptions.cshtml استفاده کردید)
                    ShowRenewButton = (!sub.VpnRemainingDays.HasValue || sub.VpnRemainingDays <= 10 || sub.StatusText == "منقضی شده" || (sub.VpnUsagePercentage >= 80 && sub.Traffic > 0)) && sub.StatusText != "درحال آماده‌سازی"

                }).ToList();

                return Json(new { success = true, data = liveUsageData });
            }
            catch (Exception ex)
            {
                // لاگ کردن خطا
                // _logger.LogError(ex, "Error fetching live usage for all subscriptions for user {UserId}", userId);
                Console.WriteLine($"Error fetching live usage for all subscriptions for user {userId}: {ex.Message}"); // برای دیباگ
                return Json(new { success = false, message = "خطا در دریافت اطلاعات بروز اشتراک‌ها." });
            }
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