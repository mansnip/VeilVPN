using AngleSharp.Css.Values;
using Application.Services.Interfaces;
using Domain.DTOs.Expense;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace VeilVPN.App.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")] // فقط ادمین‌ها دسترسی داشته باشند
    public class ExpensesController : Controller
    {
        private readonly IExpenseService _expenseService;

        public ExpensesController(IExpenseService expenseService)
        {
            _expenseService = expenseService;
        }

        // GET: Expenses
        // نمایش لیست هزینه‌ها
        public async Task<IActionResult> Index()
        {
            // نمایش پیام‌های موفقیت یا خطا که از اکشن‌های دیگر (مثل Create, Edit, Delete) آمده‌اند
            ViewBag.SuccessMessage = TempData["SuccessMessage"];
            ViewBag.ErrorMessage = TempData["ErrorMessage"];

            var expenses = await _expenseService.GetAllExpensesAsync();
            return View(expenses); // ارسال لیست ViewModel به View
        }

        // GET: Expenses/Details/5
        // نمایش جزئیات یک هزینه
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return BadRequest("شناسه هزینه نامعتبر است.");
            }

            var expenseViewModel = await _expenseService.GetExpenseByIdAsync(id);
            if (expenseViewModel == null)
            {
                return NotFound("هزینه مورد نظر یافت نشد."); // یا بازگشت به لیست با پیام خطا
            }
            return View(expenseViewModel); // ارسال ViewModel به View
        }

        // GET: Expenses/Create
        // نمایش فرم ایجاد هزینه جدید
        public IActionResult Create()
        {
            // آماده‌سازی داده‌های لازم برای Dropdownها در فرم Create
            PopulateDropdowns();
            // ایجاد یک ViewModel خالی برای ارسال به View
            var model = new CreateEditExpenseViewModel
            {
                ExpenseDate = DateTime.Today // مقدار پیش‌فرض برای تاریخ
            };
            return View(model);
        }

        // POST: Expenses/Create
        // پردازش اطلاعات فرم ایجاد و ذخیره هزینه جدید
        [HttpPost]
        [ValidateAntiForgeryToken] // جلوگیری از حملات CSRF
        public async Task<IActionResult> Create(CreateEditExpenseViewModel viewModel)
        {
            // بررسی اعتبار مدل بر اساس Annotationهای ViewModel
            if (ModelState.IsValid)
            {
                // فراخوانی سرویس برای ایجاد هزینه
                var (success, errorMessage, createdId) = await _expenseService.CreateExpenseAsync(viewModel);

                if (success)
                {
                    // تنظیم پیام موفقیت برای نمایش در صفحه Index پس از Redirect
                    TempData["SuccessMessage"] = $"هزینه با شناسه {createdId} با موفقیت ثبت شد.";
                    return RedirectToAction(nameof(Index)); // بازگشت به لیست هزینه‌ها
                }
                else
                {
                    // افزودن خطای دریافتی از سرویس به ModelState برای نمایش در View
                    ModelState.AddModelError(string.Empty, errorMessage ?? "خطای ناشناخته در ثبت هزینه رخ داد.");
                }
            }

            // اگر ModelState نامعتبر بود یا خطایی در سرویس رخ داد:
            // داده‌های Dropdownها را مجدداً پر می‌کنیم
            PopulateDropdowns();
            // فرم را با داده‌های وارد شده توسط کاربر و پیام‌های خطا مجددا نمایش می‌دهیم
            return View(viewModel);
        }

        // GET: Expenses/Edit/5
        // نمایش فرم ویرایش هزینه
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return BadRequest("شناسه هزینه نامعتبر است.");
            }

            // دریافت ViewModel هزینه برای ویرایش از سرویس
            var expenseViewModel = await _expenseService.GetExpenseForEditAsync(id);
            if (expenseViewModel == null)
            {
                TempData["ErrorMessage"] = "هزینه مورد نظر برای ویرایش یافت نشد.";
                return RedirectToAction(nameof(Index)); // یا نمایش صفحه Not Found
                // return NotFound("هزینه مورد نظر برای ویرایش یافت نشد.");
            }

            // آماده‌سازی داده‌های لازم برای Dropdownها در فرم Edit
            PopulateDropdowns(expenseViewModel.Category, expenseViewModel.Frequency);
            return View(expenseViewModel); // ارسال ViewModel به View
        }

        // POST: Expenses/Edit/5
        // پردازش اطلاعات فرم ویرایش و بروزرسانی هزینه
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, CreateEditExpenseViewModel viewModel)
        {
            // اطمینان از اینکه Id ارسال شده در URL با Id موجود در ViewModel یکی است
            if (id != viewModel.Id)
            {
                TempData["ErrorMessage"] = "عدم تطابق شناسه هزینه.";
                return RedirectToAction(nameof(Index));
                // return BadRequest("عدم تطابق شناسه هزینه.");
            }

            if (ModelState.IsValid)
            {
                // فراخوانی سرویس برای بروزرسانی هزینه
                var (success, errorMessage) = await _expenseService.UpdateExpenseAsync(viewModel);

                if (success)
                {
                    TempData["SuccessMessage"] = $"هزینه با شناسه {viewModel.Id} با موفقیت بروزرسانی شد.";
                    return RedirectToAction(nameof(Index)); // بازگشت به لیست
                }
                else
                {
                    // افزودن خطای دریافتی از سرویس به ModelState
                    ModelState.AddModelError(string.Empty, errorMessage ?? "خطای ناشناخته در بروزرسانی هزینه رخ داد.");
                    // در صورت بروز خطا، به خصوص خطای "یافت نشد"، می‌توان کاربر را به Index هدایت کرد
                    if (errorMessage != null && errorMessage.Contains("یافت نشد"))
                    {
                        TempData["ErrorMessage"] = errorMessage;
                        return RedirectToAction(nameof(Index));
                    }
                }
            }

            // اگر ModelState نامعتبر بود یا خطایی در سرویس رخ داد:
            // داده‌های Dropdownها را مجدداً پر می‌کنیم
            PopulateDropdowns(viewModel.Category, viewModel.Frequency);
            // فرم ویرایش را با داده‌های وارد شده و پیام‌های خطا مجددا نمایش می‌دهیم
            return View(viewModel);
        }

        // GET: Expenses/Delete/5
        // نمایش صفحه تأیید حذف
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return BadRequest("شناسه هزینه نامعتبر است.");
            }

            // دریافت اطلاعات هزینه برای نمایش در صفحه تایید حذف
            var expenseViewModel = await _expenseService.GetExpenseByIdAsync(id);
            if (expenseViewModel == null)
            {
                TempData["ErrorMessage"] = "هزینه مورد نظر برای حذف یافت نشد.";
                return RedirectToAction(nameof(Index));
                // return NotFound("هزینه مورد نظر برای حذف یافت نشد.");
            }

            return View(expenseViewModel); // ارسال ViewModel به View تأیید حذف
        }

        // POST: Expenses/Delete/5
        // اجرای عملیات حذف پس از تایید کاربر
        [HttpPost, ActionName("DeleteConfirmed")] // ActionName برای مپ کردن از فرمی که به /Expenses/Delete/{id} پست می‌کند
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "شناسه هزینه نامعتبر است.";
                return RedirectToAction(nameof(Index));
                // return BadRequest("شناسه هزینه نامعتبر است.");
            }

            var (success, errorMessage) = await _expenseService.DeleteExpenseAsync(id);

            if (success)
            {
                TempData["SuccessMessage"] = "هزینه با موفقیت حذف شد.";
            }
            else
            {
                // حتی اگر خطا رخ دهد، معمولاً به لیست برمی‌گردیم و پیام خطا را نمایش می‌دهیم
                TempData["ErrorMessage"] = errorMessage ?? "خطایی در هنگام حذف هزینه رخ داد.";
            }

            return RedirectToAction(nameof(Index)); // بازگشت به لیست هزینه‌ها
        }

        // متد کمکی خصوصی برای پر کردن داده‌های Dropdownها
        private void PopulateDropdowns(object? selectedCategory = null, object? selectedFrequency = null)
        {
            var categories = Enum.GetValues(typeof(ExpenseCategory))
                                 .Cast<ExpenseCategory>()
                                 .Select(c => new SelectListItem
                                 {
                                     Value = Convert.ToInt32(c).ToString(), // تغییر در این خط
                                     Text = c.ToString() // TODO: از DisplayName یا منابع محلی‌سازی استفاده کنید
                                 });

            var frequencies = Enum.GetValues(typeof(ExpenseFrequency))
                                  .Cast<ExpenseFrequency>()
                                  .Select(f => new SelectListItem
                                  {
                                      Value = Convert.ToInt32(f).ToString(), // تغییر در این خط
                                      Text = f.ToString() // TODO: از DisplayName یا منابع محلی‌سازی استفاده کنید
                                  });

            ViewBag.Categories = new SelectList(categories, "Value", "Text", selectedCategory);
            ViewBag.Frequencies = new SelectList(frequencies, "Value", "Text", selectedFrequency);
        }
    }
}
