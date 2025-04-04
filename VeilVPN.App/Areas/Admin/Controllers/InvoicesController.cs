using Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeilVPN.App.Controllers;

namespace VeilVPN.App.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")] // اطمینان از دسترسی فقط ادمین
    public class InvoicesController : Controller
    {
        private readonly IInvoiceService _invoiceService; // تزریق سرویس

        public InvoicesController(IInvoiceService invoiceService) // سازنده برای تزریق وابستگی
        {
            _invoiceService = invoiceService;
        }

        // تغییر اکشن به async و استفاده از سرویس
        public async Task<IActionResult> Index()
        {
            var invoices = await _invoiceService.GetAllInvoicesForAdminAsync(); // دریافت لیست فاکتورها
            return View(invoices); // ارسال لیست به View
        }

        [HttpGet]
        public async Task<IActionResult> ShowInvoice(string id)
        {
            try
            {
                // دریافت فاکتور از دیتابیس با استفاده از شناسه
                var invoiceViewModel = await _invoiceService.GetByIdAsync(id);

                if (invoiceViewModel == null)
                {
                    return this.RedirectWithError("فاکتور مورد نظر یافت نشد", "Index");
                }
                ViewBag.RemarkName = invoiceViewModel.RemarkName;
                return View(invoiceViewModel);
            }
            catch (Exception ex)
            {
                // لاگ کردن خطا
                return this.RedirectWithError("خطایی در بازیابی اطلاعات فاکتور رخ داده است", "Index");
            }
        }

        // ---- اکشن‌های دیگر (مانند Details, Edit Status, Delete) را در آینده اضافه کنید ----

        // مثال برای اکشن مشاهده جزئیات (نیاز به View جداگانه دارد)
        // public async Task<IActionResult> Details(string id)
        // {
        //     if (string.IsNullOrEmpty(id))
        //     {
        //         return NotFound();
        //     }
        //     // از سرویس GetByIdAsync استفاده کنید (مطمئن شوید اطلاعات کافی برای ادمین برمی‌گرداند)
        //     var invoiceViewModel = await _invoiceService.GetByIdAsync(id); // یا متد مخصوص ادمین
        //     if (invoiceViewModel == null)
        //     {
        //         return NotFound();
        //     }
        //     return View(invoiceViewModel); // ارسال به View Details.cshtml
        // }

        // مثال برای اکشن تغییر وضعیت (نیاز به View یا فرم جداگانه دارد)
        // [HttpPost]
        // [ValidateAntiForgeryToken]
        // public async Task<IActionResult> UpdateStatus(string id, string newStatus)
        // {
        //     if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(newStatus))
        //     {
        //         return BadRequest(); // یا بازگشت به صفحه با خطا
        //     }
        //
        //     bool success = await _invoiceService.UpdateInvoiceStatusAsync(id, newStatus);
        //
        //     if (success)
        //     {
        //         // ارسال پیام موفقیت آمیز (مثلاً با TempData)
        //         TempData["SuccessMessage"] = "وضعیت فاکتور با موفقیت به‌روز شد.";
        //     }
        //     else
        //     {
        //         // ارسال پیام خطا
        //         TempData["ErrorMessage"] = "خطا در به‌روزرسانی وضعیت فاکتور.";
        //     }
        //
        //     return RedirectToAction(nameof(Index)); // یا بازگشت به صفحه جزئیات
        // }
    }
}
