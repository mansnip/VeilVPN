using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Domain.ViewModels.UserPanel;
using Application.Services.Interfaces;
using VeilVPN.App.Controllers;
using Domain.ViewModels.UserPanel.Dashboard;
using Domain.DTOs.VPN;
using System.Security.Claims;

namespace VeilVPN.App.Areas.UserPanel.Controllers
{
    [Area("UserPanel")]
    [Authorize]
    public class PanelController : Controller
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly IUserService _userService;
        private readonly IInvoiceService _invoiceService; // این از قبل هست
        private readonly IServerVPNService _serverVpnService;
        // IDiscountService نیازی نیست مستقیما اینجا تزریق شود چون از طریق InvoiceService استفاده می‌شود

        public PanelController(ISubscriptionService subscriptionService, IUserService userService, IInvoiceService invoiceService, IServerVPNService serverVPNService)
        {
            _subscriptionService = subscriptionService;
            _userService = userService;
            _invoiceService = invoiceService;
            _serverVpnService = serverVPNService;
        }

        // --- اکشن Index داشبورد ---
        public async Task<IActionResult> Index()
        {
            var userId = User.Identity.Name; // یا روش مطمئن‌تر برای گرفتن UserId
            // تلاش برای گرفتن نام کاربر (اگر Claim مناسب را تنظیم کرده‌اید)
            var userName = (await _userService.GetUserById(userId)).Email;

            // دریافت اشتراک فعال کاربر
            // نکته: فرض می‌کنیم GetUserActiveSubscriptionAsync یک مدل کامل از Subscription یا یک ViewModel اختصاصی برمی‌گرداند
            var activeSub = await _subscriptionService.GetUserActiveSubscriptionAsync(userId);

            // دریافت همه فاکتورهای کاربر برای محاسبه تعداد در انتظار و گرفتن اخیرها
            // نکته: فرض می‌کنیم GetUserInvoicesAsync لیستی از InvoiceViewModel برمی‌گرداند
            var allInvoices = await _invoiceService.GetUserInvoicesAsync(userId);

            var viewModel = new DashboardViewModel
            {
                UserName = userName,
                HasActiveSubscription = activeSub != null,
                PendingInvoicesCount = allInvoices?.Count(inv => inv.PaymentStatus == "در انتظار پرداخت") ?? 0,
                RecentInvoices = allInvoices?
                                    .OrderByDescending(inv => inv.InvoiceDate)
                                    .Take(3) // نمایش 3 فاکتور اخیر
                                    .Select(inv => new InvoiceSummaryViewModel
                                    {
                                        Id = inv.Id,
                                        InvoiceNumber = inv.InvoiceNumber,
                                        InvoiceDate = inv.InvoiceDate,
                                        FinalPrice = inv.TotalAmount, // استفاده از TotalAmount مطابق InvoiceViewModel
                                        Status = inv.PaymentStatus // استفاده از PaymentStatus مطابق InvoiceViewModel
                                    }).ToList() ?? new List<InvoiceSummaryViewModel>() // لیست خالی در صورت null بودن allInvoices
            };

            if (activeSub != null)
            {
                // --- محاسبه مقادیر مشتقه ---
                // اطمینان حاصل کنید که activeSub شامل پراپرتی‌های لازم است
                // (Id, RemarkName, EndDate, Traffic, Duration, RemainingTraffic)

                int totalTraffic = activeSub.Traffic; // حجم کل از مدل اشتراک
                int remainingTraffic = activeSub.RemainingTraffic; // حجم باقی‌مانده از مدل اشتراک
                int usedTraffic = totalTraffic - remainingTraffic;
                double usagePercentage = (totalTraffic > 0) ? Math.Round(((double)usedTraffic / totalTraffic) * 100, 1) : 0;
                int remainingDays = (activeSub.EndDate > DateTime.Now) ? (int)(activeSub.EndDate - DateTime.Now).TotalDays : 0;
                string remarkName = $"اشتراک {activeSub.Traffic}GB / {activeSub.Duration} روزه";

                viewModel.ActiveSubscription = new ActiveSubscriptionInfo
                {
                    Id = activeSub.Id,
                    RemarkName = remarkName,
                    ExpiryDate = activeSub.EndDate,
                    TotalTrafficGB = totalTraffic,
                    UsedTrafficGB = usedTraffic,
                    RemainingTrafficGB = remainingTraffic,
                    RemainingDays = remainingDays,
                    UsagePercentage = usagePercentage
                    // StartDate = activeSub.StartDate // در صورت نیاز
                };
            }

            // ارسال ViewModel به View
            return View(viewModel);
        }

        public IActionResult TestRoute()
        {
            // یک breakpoint اینجا بگذارید
            System.Diagnostics.Debug.WriteLine("TestRoute action hit!");
            return Ok("PanelController Route is working!");
        }

        [HttpGet("/UserPanel/Panel/GetSubscriptionStats/{subscriptionId}")]
        [Produces("application/json")]
        public async Task<IActionResult> GetSubscriptionStats(Guid subscriptionId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                // _logger.LogWarning("Unauthorized attempt to GetSubscriptionStats - UserId is null or empty.");
                return Unauthorized(new { success = false, message = "کاربر نامعتبر است. لطفا دوباره وارد شوید." });
            }

            // _logger.LogInformation("User {UserId} requesting stats for subscription {SubscriptionId}", userId, subscriptionId);

            try
            {
                // 1. فراخوانی تابع اصلی برای دریافت همه اشتراک‌ها با آمار به‌روز:
                // نکته: ایده‌آل‌تر این است که یک متد سرویس داشته باشید که فقط آمار یک اشتراک خاص را بگیرد.
                // GetUserSubscriptionByIdAsync(userId, subscriptionId)
                List<SubscriptionViewModel> allUserSubscriptions = await _subscriptionService.GetUserSubscriptionsAsync(userId);

                // 2. پیدا کردن اشتراک مورد نظر در لیست:
                var targetSubscriptionViewModel = allUserSubscriptions.FirstOrDefault(s => s.Id == subscriptionId.ToString());

                if (targetSubscriptionViewModel == null)
                {
                    // _logger.LogWarning("Subscription {SubscriptionId} not found for user {UserId} after fetching all subscriptions.", subscriptionId, userId);
                    return NotFound(new { success = false, message = "اشتراک مورد نظر یافت نشد. ممکن است حذف شده باشد یا متعلق به شما نباشد." });
                }

                // 3. **مهم:** ساخت مدل ویو جدید یا anonymous type برای خروجی JSON با نام‌های مورد انتظار جاوا اسکریپت
                var statsForJs = new
                {
                    // پراپرتی‌های مورد نیاز جاوا اسکریپت (camelCase در JSON نهایی):
                    usagePercentage = targetSubscriptionViewModel.VpnUsagePercentage,
                    totalTrafficGB = targetSubscriptionViewModel.Traffic, // فرض می‌کنیم Traffic کل ترافیک به GB است
                    remainingTrafficGB = targetSubscriptionViewModel.VpnRemainingTraffic, // فرض می‌کنیم VpnRemainingTraffic باقی‌مانده به GB است
                                                                                          // محاسبه ترافیک مصرف شده:
                    usedTrafficGB = Math.Round(targetSubscriptionViewModel.Traffic - targetSubscriptionViewModel.VpnRemainingTraffic, 2), // گرد کردن برای دقت بهتر
                    remainingDays = targetSubscriptionViewModel.VpnRemainingDays, // از پراپرتی صحیح روزهای باقی‌مانده استفاده کنید

                    // سایر پراپرتی‌هایی که ممکن است لازم باشند (اختیاری):
                    id = targetSubscriptionViewModel.Id,
                    isActive = targetSubscriptionViewModel.IsActive,
                    statusText = targetSubscriptionViewModel.StatusText,
                    statusClass = targetSubscriptionViewModel.StatusClass,
                    endDate = targetSubscriptionViewModel.EndDate,
                    startDate = targetSubscriptionViewModel.StartDate,
                    remarkName = targetSubscriptionViewModel.RemarkName,
                    subscriptionLink = targetSubscriptionViewModel.SubscriptionLink,
                    hasVpnConnection = targetSubscriptionViewModel.HasVpnConnection,
                    isVpnActive = targetSubscriptionViewModel.IsVpnActive,
                    // ... هر پراپرتی دیگری که فکر می‌کنید ممکن است در جاوا اسکریپت لازم شود ...
                };

                // _logger.LogInformation("Successfully prepared updated stats for subscription {SubscriptionId} for user {UserId}", subscriptionId, userId);

                // 4. برگرداندن نتیجه موفقیت آمیز با آبجکت ساخته شده جدید:
                return Ok(new { success = true, message = "آمار اشتراک با موفقیت به‌روز شد.", stats = statsForJs });
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "Error occurred in GetSubscriptionStats for SubscriptionId {SubscriptionId} and UserId {UserId}.", subscriptionId, userId);
                return StatusCode(500, new { success = false, message = "خطای داخلی سرور هنگام دریافت آمار اشتراک رخ داد. جزئیات خطا در لاگ سرور ثبت شد." });
            }
        }



        public IActionResult Logout()
        {
            HttpContext.SignOutAsync();
            this.ShowSuccess("با موفقیت خارج شدید");
            return RedirectToAction("SignIn", "Auth", new { area = "Authentication" });
        }

        public IActionResult ChangePassword()
        {
            return View();
        }

        // خرید اشتراک جدید یا تمدید اشتراک موجود
        public async Task<IActionResult> BuySubscription(string subscriptionId = null)
        {
            // مدل پیش‌فرض
            var model = new SubscriptionModel
            {
                Traffic = 30,
                Duration = 30
            };

            // اگر شناسه اشتراک ارسال شده باشد، اطلاعات آن را برای تمدید بارگیری می‌کنیم
            if (!string.IsNullOrEmpty(subscriptionId))
            {
                // دریافت اشتراک با شناسه مورد نظر که متعلق به کاربر جاری باشد
                var userId = User.Identity.Name;
                var subscription = await _subscriptionService.GetSubscriptionById(subscriptionId);

                // بررسی اینکه آیا اشتراک پیدا شده و متعلق به همین کاربر است
                if (subscription != null && subscription.UserId == userId)
                {
                    // اطلاعات اشتراک را در مدل قرار می‌دهیم
                    model.Traffic = subscription.Traffic;
                    model.Duration = subscription.Duration;
                    model.RemarkName = subscription.RemarkName;
                    model.IsRenewal = true;
                    model.RenewalSubscriptionId = subscriptionId;
                    model.RenewalSubscriptionName = subscription.RemarkName;
                }
                else
                {
                    // اشتراک پیدا نشد یا متعلق به کاربر دیگری است
                    this.ShowError("اشتراک مورد نظر برای تمدید یافت نشد");
                }
            }

            return View(model);
        }

        #region Show Invoice

        [HttpGet]
        public async Task<IActionResult> ShowInvoice(string id)
        {
            try
            {
                // دریافت فاکتور از دیتابیس با استفاده از شناسه
                var invoiceViewModel = await _invoiceService.GetByIdAsync(id);

                if (invoiceViewModel == null && invoiceViewModel.UserId != User.Identity!.Name)
                {
                    return this.RedirectWithError("فاکتور مورد نظر یافت نشد", "Invoices");
                }
                ViewBag.RemarkName = invoiceViewModel.RemarkName;
                return View(invoiceViewModel);
            }
            catch (Exception ex)
            {
                // لاگ کردن خطا
                return this.RedirectWithError("خطایی در بازیابی اطلاعات فاکتور رخ داده است", "Invoices");
            }
        }

        #endregion

        #region ShowInvoicePreview

        [HttpGet]
        public async Task<IActionResult> ShowInvoicePreview(int traffic, int duration, string remarkName, string renewalId = null)
        {
            try
            {
                // فقط محاسبه قیمت با جزئیات - بدون ذخیره در دیتابیس
                var priceDetails = await _subscriptionService.CalculateDetailedPrice(traffic, duration);

                // ایجاد مدل برای نمایش پیش‌فاکتور
                var invoiceViewModel = new InvoiceViewModel
                {
                    InvoiceNumber = "پیش‌فاکتور", // اینجا شماره فاکتور واقعی ایجاد نمی‌شود
                    InvoiceDate = DateTime.Now,
                    PaymentStatus = "پیش نمایش", // وضعیت پرداخت برای پیش‌فاکتور
                    RemarkName = remarkName,
                    Subscription = new SubscriptionDetails
                    {
                        Traffic = traffic,
                        Duration = duration,
                        BasePrice = priceDetails.BasePrice,
                        PlanDiscountPercent = priceDetails.DiscountPercent,
                        PlanDiscountAmount = priceDetails.DiscountAmount,
                        FinalPrice = priceDetails.FinalPrice,
                    }
                };

                // نمایش پیش‌فاکتور بدون ذخیره در دیتابیس
                ViewBag.IsPreview = true; // این مقدار را برای تشخیص پیش‌فاکتور در ویو اضافه می‌کنیم
                ViewBag.Traffic = traffic; // برای استفاده در فرم خرید
                ViewBag.Duration = duration; // برای استفاده در فرم خرید
                ViewBag.RemarkName = remarkName;

                // اضافه کردن اطلاعات تمدید به ViewBag
                ViewBag.RenewalId = renewalId;

                // اگر renewalId داشتیم، اطلاعات اشتراک اصلی را دریافت و ذخیره می‌کنیم
                if (!string.IsNullOrEmpty(renewalId))
                {
                    var userId = User.Identity.Name; // دریافت آیدی کاربر جاری
                    var originalSubscription = await _subscriptionService.GetSubscriptionById(renewalId);

                    if (originalSubscription != null && originalSubscription.UserId == userId)
                    {
                        invoiceViewModel.IsRenewal = true;
                        invoiceViewModel.RenewalSubscriptionId = renewalId;
                        invoiceViewModel.RenewalSubscriptionName = originalSubscription.RemarkName;

                        // اضافه کردن یک بج نمایشی در پیش‌فاکتور برای تمدید
                        ViewBag.IsRenewal = true;
                        ViewBag.RenewalSubscriptionName = originalSubscription.RemarkName;
                    }
                }

                return View("ShowInvoice", invoiceViewModel);
            }
            catch (Exception ex)
            {
                // لاگ کردن خطا
                this.ShowError("خطایی در محاسبه قیمت رخ داده است. لطفا دوباره تلاش کنید");
                return RedirectToAction("Index", "Subscription", new { area = "UserPanel" });
            }
        }

        #endregion

        #region CreateInvoice

        [HttpPost]
        public async Task<IActionResult> CreateInvoice(int traffic, int duration, string remarkName, string renewalId = null)
        {
            try
            {
                // محاسبه قیمت با جزئیات
                var priceDetails = await _subscriptionService.CalculateDetailedPrice(traffic, duration);

                // ذخیره فاکتور در دیتابیس
                var userId = User.Identity.Name;

                // افزودن پارامتر renewalId به متد CreateInvoiceAsync
                var invoice = await _invoiceService.CreateInvoiceAsync(userId, traffic, duration, priceDetails, remarkName, renewalId);

                // هدایت به صفحه نمایش فاکتور با استفاده از شناسه فاکتور
                return RedirectToAction("ShowInvoice", new { id = invoice.Id });
            }
            catch (Exception ex)
            {
                // لاگ کردن خطا
                this.ShowError("خطایی در ایجاد فاکتور رخ داده است. لطفا دوباره تلاش کنید");
                if (renewalId == null)
                {
                    return RedirectToAction("BuySubscription");
                }
                return RedirectToAction("MySubscriptions", "Subscription", new { area = "UserPanel" });
            }
        }

        #endregion

        #region ProcessPayment

        [HttpPost]
        public async Task<IActionResult> ProcessPayment(string invoiceId)
        {
            try
            {
                // بررسی وجود فاکتور
                var invoice = await _invoiceService.GetByIdAsync(invoiceId);

                if (invoice == null)
                {
                    return this.RedirectWithError("فاکتور مورد نظر یافت نشد", "Invoices");
                }

                // بررسی دسترسی کاربر به این فاکتور
                if (invoice.UserId != User.Identity.Name)
                {
                    return this.RedirectWithError("شما اجازه دسترسی به این فاکتور را ندارید", "Invoices");
                }

                // در آینده: ارتباط با درگاه پرداخت
                if (invoice.PaymentStatus == "در انتظار پرداخت")
                {
                    string paymentSiteBaseUrl = "https://csgame.ir"; // Load from config
                    string callbackUrl = Url.Action("PaymentCallback", "Payment", new { area = "" }, Request.Scheme); // URL on RahaGozar

                    var redirectUrl = $"{paymentSiteBaseUrl}/Payment/Process?invoiceId={invoice.Id}&token={invoice.PaymentToken}&callbackUrl={Uri.EscapeDataString(callbackUrl)}";

                    // --- 4. Redirect user to Payment Site ---
                    return Redirect(redirectUrl);
                }

                // فعلاً: تغییر وضعیت فاکتور به پرداخت شده

                if (invoice.PaymentStatus == "پرداخت شده")
                {
                    if (invoice.IsComplate)
                    {
                        return this.RedirectWithError("این فاکتور قبلا پرداخت شده است", "ShowInvoice", new { id = invoiceId });
                    }

                    // ایجاد اشتراک جدید برای کاربر
                    var create = await _subscriptionService.CreateSubscriptionFromInvoiceAsync(invoiceId);
                    if (create.success)
                    {
                        var orgInvoice = await _invoiceService.GetOrginalInvoiceById(invoiceId);
                        orgInvoice.IsComplate = true;
                        await _invoiceService.UpdateInvoice(orgInvoice);
                        return this.RedirectWithSuccess("پرداخت با موفقیت انجام شد و اشتراک شما فعال گردید",
                            "ShowInvoice", new { id = invoiceId });
                    }
                    else
                    {
                        return this.RedirectWithError(create.Message, "ShowInvoice", new { id = invoiceId });
                    }

                }
                return this.RedirectWithError("خطایی در پردازش پرداخت رخ داده است. لطفاً با پشتیبانی تماس بگیرید",
                            "ShowInvoice", new { id = invoiceId });
            }
            catch (Exception ex)
            {
                // لاگ کردن خطا
                return this.RedirectWithError("خطایی در پردازش پرداخت رخ داده است. لطفاً دوباره تلاش کنید", "Invoices");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ProcessPaymentt(string invoiceId)
        {
            try
            {
                // بررسی وجود فاکتور
                var invoice = await _invoiceService.GetByIdAsync(invoiceId);

                if (invoice == null)
                {
                    return this.RedirectWithError("فاکتور مورد نظر یافت نشد", "Invoices");
                }

                // بررسی دسترسی کاربر به این فاکتور
                if (invoice.UserId != User.Identity.Name)
                {
                    return this.RedirectWithError("شما اجازه دسترسی به این فاکتور را ندارید", "Invoices");
                }

                // در آینده: ارتباط با درگاه پرداخت
                if (invoice.PaymentStatus == "در انتظار پرداخت")
                {
                    string paymentSiteBaseUrl = "https://localhost:32779"; // Load from config
                    string callbackUrl = Url.Action("PaymentCallback", "Payment", new { area = "" }, Request.Scheme); // URL on RahaGozar

                    var redirectUrl = $"{paymentSiteBaseUrl}/Payment/Process?invoiceId={invoice.Id}&token={invoice.PaymentToken}&callbackUrl={Uri.EscapeDataString(callbackUrl)}";

                    // --- 4. Redirect user to Payment Site ---
                    return Redirect(redirectUrl);
                }

                // فعلاً: تغییر وضعیت فاکتور به پرداخت شده

                if (invoice.PaymentStatus == "پرداخت شده")
                {
                    if (invoice.IsComplate)
                    {
                        return this.RedirectWithError("این فاکتور قبلا پرداخت شده است", "ShowInvoice", new { id = invoiceId });
                    }

                    // ایجاد اشتراک جدید برای کاربر
                    var create = await _subscriptionService.CreateSubscriptionFromInvoiceAsync(invoiceId);
                    if (create.success)
                    {
                        var orgInvoice = await _invoiceService.GetOrginalInvoiceById(invoiceId);
                        orgInvoice.IsComplate = true;
                        await _invoiceService.UpdateInvoice(orgInvoice);
                        return this.RedirectWithSuccess("پرداخت با موفقیت انجام شد و اشتراک شما فعال گردید",
                            "ShowInvoice", new { id = invoiceId });
                    }
                    else
                    {
                        return this.RedirectWithError(create.Message, "ShowInvoice", new { id = invoiceId });
                    }

                }
                return this.RedirectWithError("خطایی در پردازش پرداخت رخ داده است. لطفاً با پشتیبانی تماس بگیرید",
                            "ShowInvoice", new { id = invoiceId });
            }
            catch (Exception ex)
            {
                // لاگ کردن خطا
                return this.RedirectWithError("خطایی در پردازش پرداخت رخ داده است. لطفاً دوباره تلاش کنید", "Invoices");
            }
        }

        #endregion

        #region CancelInvoice

        [HttpPost]
        public async Task<IActionResult> CancelInvoice(string invoiceId)
        {
            try
            {
                // بررسی وجود فاکتور
                var invoice = await _invoiceService.GetByIdAsync(invoiceId);

                if (invoice == null)
                {
                    return this.RedirectWithError("فاکتور مورد نظر یافت نشد", "Invoices");
                }

                // بررسی دسترسی کاربر به این فاکتور
                if (invoice.UserId != User.Identity.Name)
                {
                    return this.RedirectWithError("شما اجازه دسترسی به این فاکتور را ندارید", "Invoices");
                }

                // بررسی وضعیت فاکتور - از PaymentStatus استفاده می‌کنیم
                if (invoice.PaymentStatus != "در انتظار پرداخت")
                {
                    return this.RedirectWithError("این فاکتور قابل لغو نیست",
                        "ShowInvoice", new { id = invoiceId });
                }

                // تغییر وضعیت فاکتور به لغو شده
                bool result = await _invoiceService.UpdateInvoiceStatusAsync(invoiceId, "لغو شده");

                if (result)
                {
                    return this.RedirectWithSuccess("فاکتور با موفقیت لغو شد", "Invoices");
                }
                else
                {
                    return this.RedirectWithError("خطایی در لغو فاکتور رخ داده است. لطفاً با پشتیبانی تماس بگیرید",
                        "ShowInvoice", new { id = invoiceId });
                }
            }
            catch (Exception ex)
            {
                // لاگ کردن خطا
                return this.RedirectWithError("خطایی در لغو فاکتور رخ داده است. لطفاً دوباره تلاش کنید", "Invoices");
            }
        }

        #endregion

        #region Invoices

        public async Task<IActionResult> Invoices(string result)
        {
            if (result != null)
            {
                this.ShowToast(result, "error");
            }
            var userId = User.Identity.Name; // باید یک extension method برای گرفتن ID کاربر از کلیم‌های Identity بنویسید
            var invoices = await _invoiceService.GetUserInvoicesAsync(userId);
            return View(invoices);
        }

        #endregion

        #region ApplyDiscountCode

        [HttpPost]
        [ValidateAntiForgeryToken] // مهم برای امنیت در برابر CSRF
        public async Task<IActionResult> ApplyDiscountCode(string invoiceId, string discountCode)
        {
            // 1. اعتبارسنجی ورودی اولیه
            if (string.IsNullOrWhiteSpace(invoiceId) || string.IsNullOrWhiteSpace(discountCode))
            {
                TempData["ErrorMessage"] = "شناسه فاکتور یا کد تخفیف نامعتبر است.";
                // بازگشت به صفحه فاکتور
                return RedirectToAction("ShowInvoice", new { id = invoiceId });
            }

            // 2. دریافت شناسه کاربر به صورت امن
            // نکته مهم: User.Identity.Name معمولا نام کاربری را برمی‌گرداند، نه شناسه.
            // از ClaimTypes.NameIdentifier استفاده کنید که شناسه یکتای کاربر را نگه می‌دارد.
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                // این اتفاق نباید بیفتد اگر کاربر Authorize شده باشد، اما بررسی آن خوب است
                TempData["ErrorMessage"] = "خطا در شناسایی کاربر.";
                return RedirectToAction("Invoices"); // یا به صفحه لاگین
            }

            try
            {
                // 3. فراخوانی سرویس فاکتور برای اعمال کد تخفیف
                // این سرویس شامل تمام منطق اعتبارسنجی کد، محاسبه تخفیف، آپدیت فاکتور و کد در یک تراکنش است.
                var result = await _invoiceService.ApplyDiscountCodeAsync(invoiceId, discountCode.Trim(), userId); // کد را Trim کنید

                // 4. مدیریت نتیجه بازگشتی از سرویس
                if (result.IsSuccess)
                {
                    TempData["SuccessMessage"] = result.Message; // مثلاً: "کد تخفیف با موفقیت روی فاکتور شما اعمال شد."
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message; // مثلاً: "کد تخفیف وارد شده نامعتبر است." یا "ظرفیت استفاده تمام شده."
                }

                // 5. ریدایرکت به صفحه نمایش فاکتور (ShowInvoice)
                // این باعث می‌شود صفحه با اطلاعات به‌روز شده فاکتور (قیمت جدید و ...) مجدداً لود شود و پیام TempData نمایش داده شود.
                return RedirectToAction("ShowInvoice", new { id = invoiceId });
            }
            catch (Exception ex)
            {
                // لاگ کردن خطا (ex) با استفاده از یک کتابخانه لاگ مثل Serilog یا NLog
                // Log.Error(ex, "An error occurred while applying discount code {DiscountCode} to invoice {InvoiceId} for user {UserId}", discountCode, invoiceId, userId);
                TempData["ErrorMessage"] = "خطای سیستمی هنگام اعمال کد تخفیف رخ داد. لطفاً با پشتیبانی تماس بگیرید.";
                // بازگشت به صفحه فاکتور حتی در صورت بروز خطای پیش‌بینی نشده
                return RedirectToAction("ShowInvoice", new { id = invoiceId });
            }
        }

        #endregion

    }
}