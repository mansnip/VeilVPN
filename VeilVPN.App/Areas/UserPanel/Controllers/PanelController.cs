using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Domain.ViewModels.UserPanel;
using Application.Services.Interfaces;
using VeilVPN.App.Controllers;
using System;
using System.Threading.Tasks;
using Domain.ViewModels.UserPanel.Dashboard;

namespace VeilVPN.App.Areas.UserPanel.Controllers
{
    [Area("UserPanel")]
    [Authorize]
    public class PanelController : Controller
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly IUserService _userService;
        private readonly IInvoiceService _invoiceService;
        private readonly IServerVPNService _serverVpnService;

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
                        DiscountPercent = priceDetails.DiscountPercent,
                        DiscountAmount = priceDetails.DiscountAmount,
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
    }
}