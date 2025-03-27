using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Domain.ViewModels.UserPanel;
using Application.Services.Interfaces;
using VeilVPN.App.Controllers;
using System;
using System.Threading.Tasks;

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

        public IActionResult Index()
        {
            return View();
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

                if (invoiceViewModel == null)
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

                // بررسی وضعیت فاکتور - از PaymentStatus استفاده می‌کنیم
                if (invoice.PaymentStatus != "در انتظار پرداخت")
                {
                    return this.RedirectWithError("این فاکتور قابل پرداخت نیست",
                        "ShowInvoice", new { id = invoiceId });
                }

                // در آینده: ارتباط با درگاه پرداخت

                // فعلاً: تغییر وضعیت فاکتور به پرداخت شده
                bool result = await _invoiceService.UpdateInvoiceStatusAsync(invoiceId, "پرداخت شده");

                if (result)
                {
                    // ایجاد اشتراک جدید برای کاربر
                    var create = await _subscriptionService.CreateSubscriptionFromInvoiceAsync(invoiceId);
                    if (create.success)
                    {
                        return this.RedirectWithSuccess("پرداخت با موفقیت انجام شد و اشتراک شما فعال گردید",
                            "ShowInvoice", new { id = invoiceId });
                    }
                    else
                    {
                        return this.RedirectWithError(create.Message, "ShowInvoice", new { id = invoiceId });
                    }
                }
                else
                {
                    return this.RedirectWithError("خطایی در پردازش پرداخت رخ داده است. لطفاً با پشتیبانی تماس بگیرید",
                        "ShowInvoice", new { id = invoiceId });
                }
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

        public async Task<IActionResult> Invoices()
        {
            var userId = User.Identity.Name; // باید یک extension method برای گرفتن ID کاربر از کلیم‌های Identity بنویسید
            var invoices = await _invoiceService.GetUserInvoicesAsync(userId);
            return View(invoices);
        }

        #endregion
    }
}