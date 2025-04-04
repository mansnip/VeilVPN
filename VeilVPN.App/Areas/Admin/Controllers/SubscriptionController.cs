using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Services.Interfaces;
using Domain.Interfaces;
using Application.API;
using VeilVPN.App.Controllers;
using Application.Services.Implementations;
using Domain.Entities.Account;

namespace VeilVPN.App.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class SubscriptionController : Controller
    {
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IUserRepository _userRepository;
        private readonly IServerVPNService _serverVPNService;
        private readonly ApiManager _apiManager;
        private readonly ILogger<SubscriptionController> _logger;
        private readonly ISubscriptionService _subscriptionService;


        public SubscriptionController(
            ISubscriptionRepository subscriptionRepository,
            IUserRepository userRepository,
            IServerVPNService serverVPNService,
            ApiManager apiManager,
            ILogger<SubscriptionController> logger,
            ISubscriptionService subscriptionService)
        {
            _subscriptionRepository = subscriptionRepository;
            _userRepository = userRepository;
            _serverVPNService = serverVPNService;
            _apiManager = apiManager;
            _logger = logger;
            _subscriptionService = subscriptionService;
        }

        public async Task<IActionResult> Index()
        {
            var subscriptions = await _subscriptionRepository.GetAllWithUserAsync();
            return View(subscriptions);
        }

        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                // از متد RedirectWithError یا معادل آن استفاده کنید
                return this.RedirectWithError("شناسه اشتراک نامعتبر است", nameof(Index));
            }

            // فراخوانی متد سرویس جدید
            var viewModel = await _subscriptionService.GetSubscriptionDetailsForAdminAsync(id);

            if (viewModel == null)
            {
                // مدیریت حالتی که اشتراک یافت نشد یا خطای API رخ داد
                return this.RedirectWithError("شناسه اشتراک نامعتبر است", nameof(Index));
            }

            // ارسال ViewModel به View
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetTraffic(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return this.RedirectWithError("شناسه اشتراک نامعتبر است", nameof(Index));
                }

                var subscription = await _subscriptionRepository.GetSubscriptionWithUserAndServerAsync(id);
                if (subscription == null)
                {
                    return this.RedirectWithError("اشتراک مورد نظر یافت نشد", nameof(Index));
                }

                // دریافت سرور مربوطه
                var server = await _serverVPNService.GetServerByIdAsync(subscription.VpnServerID);
                if (server == null)
                {
                    _logger.LogError($"سرور با شناسه {subscription.VpnServerID} یافت نشد.");
                    return this.RedirectWithError("سرور مربوط به اشتراک یافت نشد", nameof(Index));
                }

                // ریست کردن ترافیک مصرفی
                var resetResult = await _apiManager.ResetClientTraffic(server, subscription.VpnEmailName);
                if (!resetResult.Success)
                {
                    _logger.LogError($"خطا در ریست ترافیک: {resetResult.Message}");
                    return this.RedirectWithError($"خطا در ریست ترافیک: {resetResult.Message}", nameof(Details), new { id });
                }

                // به‌روزرسانی اطلاعات اشتراک در دیتابیس
                subscription.RemainingTraffic = subscription.Traffic;
                await _subscriptionRepository.UpdateAsync(subscription);

                return this.RedirectWithSuccess("ترافیک کاربر با موفقیت ریست شد", nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا هنگام ریست ترافیک");
                return this.RedirectWithError($"خطا در سیستم: {ex.Message}", nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSubscriptionStats(string id)
        {
            try
            {
                var subscription = await _subscriptionRepository.GetByIdAsync(id);
                if (subscription == null)
                    return Json(new { success = false, message = "اشتراک یافت نشد" });

                // محاسبه درصد مصرف ترافیک
                int trafficUsagePercent = 0;
                if (subscription.Traffic > 0)
                {
                    int usedTraffic = subscription.Traffic - subscription.RemainingTraffic;
                    trafficUsagePercent = (usedTraffic * 100) / subscription.Traffic;
                }

                // محاسبه درصد گذشت زمان
                int timeUsagePercent = 0;
                if (subscription.StartDate != DateTime.MinValue && subscription.EndDate != DateTime.MinValue)
                {
                    var totalDuration = (subscription.EndDate - subscription.StartDate).TotalDays;
                    var elapsedDuration = (DateTime.Now - subscription.StartDate).TotalDays;

                    if (totalDuration > 0)
                    {
                        timeUsagePercent = (int)((elapsedDuration * 100) / totalDuration);
                        timeUsagePercent = Math.Min(100, Math.Max(0, timeUsagePercent)); // اطمینان از اینکه بین 0 تا 100 باشد
                    }
                }

                return Json(new
                {
                    success = true,
                    trafficUsagePercent,
                    timeUsagePercent,
                    remainingDays = Math.Max(0, (subscription.EndDate - DateTime.Now).Days),
                    remainingTraffic = subscription.RemainingTraffic
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در دریافت آمار اشتراک");
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}