using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Domain.ViewModels.UserPanel;
using Application.Services.Interfaces;
using System.Threading.Tasks;
using System;
using Domain.Entities;

namespace VeilVPN.App.Areas.UserPanel.Controllers
{
    [Area("UserPanel")]
    [Authorize]
    public class PanelController : Controller
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly IUserService _userService;
        private readonly IInvoiceService _invoiceService;

        public PanelController(ISubscriptionService subscriptionService, IUserService userService, IInvoiceService invoiceService)
        {
            _subscriptionService = subscriptionService;
            _userService = userService;
            _invoiceService = invoiceService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.SignOutAsync();
            return RedirectToAction("SignIn", "Auth", new { area = "Authentication" });
        }

        public IActionResult ChangePassword()
        {
            return View();
        }

        // Buy a new subscription
        public IActionResult BuySubscription()
        {
            return View();
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
                    TempData["ErrorMessage"] = "فاکتور مورد نظر یافت نشد.";
                    return RedirectToAction("Invoices");
                }

                return View(invoiceViewModel);
            }
            catch (Exception ex)
            {
                // لاگ کردن خطا
                TempData["ErrorMessage"] = "خطایی در بازیابی اطلاعات فاکتور رخ داده است.";
                return RedirectToAction("Invoices");
            }
        }


        #endregion

        #region ShowInvoicePreview

        [HttpGet]
        public async Task<IActionResult> ShowInvoicePreview(int traffic, int duration)
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
                    Subscription = new SubscriptionDetails
                    {
                        Traffic = traffic,
                        Duration = duration,
                        BasePrice = priceDetails.BasePrice,
                        DiscountPercent = priceDetails.DiscountPercent,
                        DiscountAmount = priceDetails.DiscountAmount,
                        FinalPrice = priceDetails.FinalPrice
                    }
                };

                // نمایش پیش‌فاکتور بدون ذخیره در دیتابیس
                ViewBag.IsPreview = true; // این مقدار را برای تشخیص پیش‌فاکتور در ویو اضافه می‌کنیم
                ViewBag.Traffic = traffic; // برای استفاده در فرم خرید
                ViewBag.Duration = duration; // برای استفاده در فرم خرید

                return View("ShowInvoice", invoiceViewModel);
            }
            catch (Exception ex)
            {
                // لاگ کردن خطا
                TempData["ErrorMessage"] = "خطایی در محاسبه قیمت رخ داده است. لطفا دوباره تلاش کنید.";
                return RedirectToAction("Index", "Subscription", new { area = "UserPanel" });
            }
        }

        #endregion

        #region CreateInvoice

        [HttpPost]
        public async Task<IActionResult> CreateInvoice(int traffic, int duration)
        {
            try
            {
                // محاسبه قیمت با جزئیات
                var priceDetails = await _subscriptionService.CalculateDetailedPrice(traffic, duration);

                // ذخیره فاکتور در دیتابیس
                var userId = User.Identity.Name;
                var invoice = await _invoiceService.CreateInvoiceAsync(userId, traffic, duration, priceDetails);

                // هدایت به صفحه نمایش فاکتور با استفاده از شناسه فاکتور
                return RedirectToAction("ShowInvoice", new { id = invoice.Id });
            }
            catch (Exception ex)
            {
                // لاگ کردن خطا
                TempData["ErrorMessage"] = "خطایی در ایجاد فاکتور رخ داده است. لطفا دوباره تلاش کنید.";
                return RedirectToAction("Index", "Subscription", new { area = "UserPanel" });
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
                    TempData["ErrorMessage"] = "فاکتور مورد نظر یافت نشد.";
                    return RedirectToAction("Invoices");
                }

                // بررسی دسترسی کاربر به این فاکتور
                if (invoice.UserId != User.Identity.Name)
                {
                    TempData["ErrorMessage"] = "شما اجازه دسترسی به این فاکتور را ندارید.";
                    return RedirectToAction("Invoices");
                }

                // بررسی وضعیت فاکتور - از PaymentStatus استفاده می‌کنیم
                if (invoice.PaymentStatus != "در انتظار پرداخت")
                {
                    TempData["ErrorMessage"] = "این فاکتور قابل پرداخت نیست.";
                    return RedirectToAction("ShowInvoice", new { id = invoiceId });
                }

                // در آینده: ارتباط با درگاه پرداخت
                // فعلاً: تغییر وضعیت فاکتور به پرداخت شده
                bool result = await _invoiceService.UpdateInvoiceStatusAsync(invoiceId, "پرداخت شده");

                if (result)
                {
                    // ایجاد اشتراک جدید برای کاربر
                    await _subscriptionService.CreateSubscriptionFromInvoiceAsync(invoiceId);

                    TempData["SuccessMessage"] = "پرداخت با موفقیت انجام شد و اشتراک شما فعال گردید.";
                    return RedirectToAction("ShowInvoice", new { id = invoiceId });
                }
                else
                {
                    TempData["ErrorMessage"] = "خطایی در پردازش پرداخت رخ داده است. لطفاً با پشتیبانی تماس بگیرید.";
                    return RedirectToAction("ShowInvoice", new { id = invoiceId });
                }
            }
            catch (Exception ex)
            {
                // لاگ کردن خطا
                TempData["ErrorMessage"] = "خطایی در پردازش پرداخت رخ داده است. لطفاً دوباره تلاش کنید.";
                return RedirectToAction("Invoices");
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
                    TempData["ErrorMessage"] = "فاکتور مورد نظر یافت نشد.";
                    return RedirectToAction("Invoices");
                }

                // بررسی دسترسی کاربر به این فاکتور
                if (invoice.UserId != User.Identity.Name)
                {
                    TempData["ErrorMessage"] = "شما اجازه دسترسی به این فاکتور را ندارید.";
                    return RedirectToAction("Invoices");
                }

                // بررسی وضعیت فاکتور - از PaymentStatus استفاده می‌کنیم
                if (invoice.PaymentStatus != "در انتظار پرداخت")
                {
                    TempData["ErrorMessage"] = "این فاکتور قابل لغو نیست.";
                    return RedirectToAction("ShowInvoice", new { id = invoiceId });
                }

                // تغییر وضعیت فاکتور به لغو شده
                bool result = await _invoiceService.UpdateInvoiceStatusAsync(invoiceId, "لغو شده");

                if (result)
                {
                    TempData["SuccessMessage"] = "فاکتور با موفقیت لغو شد.";
                    return RedirectToAction("Invoices");
                }
                else
                {
                    TempData["ErrorMessage"] = "خطایی در لغو فاکتور رخ داده است. لطفاً با پشتیبانی تماس بگیرید.";
                    return RedirectToAction("ShowInvoice", new { id = invoiceId });
                }
            }
            catch (Exception ex)
            {
                // لاگ کردن خطا
                TempData["ErrorMessage"] = "خطایی در لغو فاکتور رخ داده است. لطفاً دوباره تلاش کنید.";
                return RedirectToAction("Invoices");
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
