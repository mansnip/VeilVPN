using DataLayer.Context;
using Domain.Entities;
using Domain.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace VeilVPN.App.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")] // فقط ادمین‌ها دسترسی داشته باشند (نام رول را تنظیم کنید)
    public class DiscountCodeController : Controller
    {
        private readonly VeilVpnDbContext _context; // تزریق DbContext

        public DiscountCodeController(VeilVpnDbContext context)
        {
            _context = context;
        }

        // GET: Admin/DiscountCode
        public async Task<IActionResult> Index()
        {
            var discountCodes = await _context.DiscountCodes // نام DbSet شما
                                            .OrderByDescending(d => d.CreatedDate)
                                            .Select(d => new DiscountCodeViewModel // تبدیل به ViewModel برای نمایش
                                            {
                                                Id = d.Id,
                                                Code = d.Code,
                                                DiscountPercent = d.DiscountPercent,
                                                MaxUses = d.MaxUses,
                                                CurrentUses = d.CurrentUses,
                                                ExpiryDate = d.ExpiryDate,
                                                IsActive = d.IsActive,
                                                CreatedDate = d.CreatedDate
                                            })
                                            .ToListAsync();
            return View(discountCodes);
        }

        // GET: Admin/DiscountCode/Create
        public IActionResult Create()
        {
            // ایجاد یک ViewModel خالی با مقادیر پیش‌فرض
            var viewModel = new DiscountCodeViewModel
            {
                IsActive = true, // پیش‌فرض فعال باشد
                MaxUses = 1 // پیش‌فرض حداقل یکبار مصرف
            };
            return View(viewModel); // ارسال ViewModel به ویو
        }

        // POST: Admin/DiscountCode/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DiscountCodeViewModel viewModel)
        {
            // بررسی اینکه کدی با همین نام قبلا ثبت نشده باشد (بدون توجه به بزرگی و کوچکی حروف)
            bool codeExists = await _context.DiscountCodes.AnyAsync(d => d.Code.ToLower() == viewModel.Code.ToLower());
            if (codeExists)
            {
                ModelState.AddModelError("Code", "این کد تخفیف قبلاً ثبت شده است.");
            }

            if (ModelState.IsValid)
            {
                // تبدیل ViewModel به Entity
                var discountCode = new DiscountCode
                {
                    Id = Guid.NewGuid().ToString(), // ایجاد ID جدید
                    Code = viewModel.Code.Trim(), // حذف فضاهای خالی احتمالی اول و آخر
                    DiscountPercent = viewModel.DiscountPercent,
                    MaxUses = viewModel.MaxUses,
                    ExpiryDate = viewModel.ExpiryDate,
                    IsActive = viewModel.IsActive,
                    CreatedDate = DateTime.Now, // تاریخ ایجاد در لحظه ثبت
                    CurrentUses = 0 // استفاده اولیه صفر است
                };

                _context.Add(discountCode);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"کد تخفیف '{discountCode.Code}' با موفقیت ایجاد شد.";
                return RedirectToAction(nameof(Index));
            }

            // اگر مدل معتبر نبود، به همراه خطاها به ویو برگردان
            // اطمینان از ارسال تاریخ صحیح به flatpickr در صورت خطا
            ViewData["ExpiryDateValue"] = viewModel.ExpiryDate?.ToString("yyyy/MM/dd");
            return View(viewModel);
        }

        // GET: Admin/DiscountCode/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var discountCode = await _context.DiscountCodes.FindAsync(id);
            if (discountCode == null)
            {
                return NotFound();
            }

            // تبدیل Entity به ViewModel
            var viewModel = new DiscountCodeViewModel
            {
                Id = discountCode.Id,
                Code = discountCode.Code,
                DiscountPercent = discountCode.DiscountPercent,
                MaxUses = discountCode.MaxUses,
                CurrentUses = discountCode.CurrentUses, // نمایش تعداد استفاده شده
                ExpiryDate = discountCode.ExpiryDate,
                IsActive = discountCode.IsActive,
                CreatedDate = discountCode.CreatedDate // نمایش تاریخ ایجاد
            };

            // ارسال تاریخ به فرمت مناسب flatpickr
            ViewData["ExpiryDateValue"] = viewModel.ExpiryDate?.ToString("yyyy/MM/dd");

            return View(viewModel);
        }

        // POST: Admin/DiscountCode/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, DiscountCodeViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return NotFound();
            }

            // بررسی اینکه کد جدید با کد دیگری (غیر از خودش) تداخل نداشته باشد
            bool codeExists = await _context.DiscountCodes.AnyAsync(d => d.Id != viewModel.Id && d.Code.ToLower() == viewModel.Code.ToLower());
            if (codeExists)
            {
                ModelState.AddModelError("Code", "کد تخفیف دیگری با این نام وجود دارد.");
            }


            if (ModelState.IsValid)
            {
                try
                {
                    var discountCodeToUpdate = await _context.DiscountCodes.FindAsync(id);
                    if (discountCodeToUpdate == null)
                    {
                        return NotFound();
                    }

                    // به‌روزرسانی مقادیر Entity از ViewModel
                    discountCodeToUpdate.Code = viewModel.Code.Trim();
                    discountCodeToUpdate.DiscountPercent = viewModel.DiscountPercent;
                    discountCodeToUpdate.MaxUses = viewModel.MaxUses;
                    discountCodeToUpdate.ExpiryDate = viewModel.ExpiryDate;
                    discountCodeToUpdate.IsActive = viewModel.IsActive;
                    // CurrentUses و CreatedDate نباید از فرم ویرایش شوند

                    _context.Update(discountCodeToUpdate);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"کد تخفیف '{discountCodeToUpdate.Code}' با موفقیت ویرایش شد.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await DiscountCodeExists(viewModel.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "خطا در ذخیره تغییرات. لطفا دوباره تلاش کنید.";
                        // لاگ کردن خطا هم پیشنهاد می‌شود
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            // اگر مدل معتبر نبود، به همراه خطاها به ویو برگردان
            ViewData["ExpiryDateValue"] = viewModel.ExpiryDate?.ToString("yyyy/MM/dd");
            return View(viewModel);
        }

        // POST: Admin/DiscountCode/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var discountCode = await _context.DiscountCodes.FindAsync(id);
            if (discountCode == null)
            {
                TempData["ErrorMessage"] = "کد تخفیف یافت نشد.";
                return RedirectToAction(nameof(Index));
            }

            discountCode.IsActive = !discountCode.IsActive; // تغییر وضعیت
            _context.Update(discountCode);
            await _context.SaveChangesAsync();

            var status = discountCode.IsActive ? "فعال" : "غیرفعال";
            TempData["SuccessMessage"] = $"وضعیت کد تخفیف '{discountCode.Code}' به {status} تغییر یافت.";

            return RedirectToAction(nameof(Index));
        }


        // POST: Admin/DiscountCode/Delete/5 (اختیاری - حذف کامل)
        // هشدار: حذف کامل ممکن است باعث مشکل در نمایش فاکتورهای قبلی شود
        // پیشنهاد می‌شود به جای حذف، از ToggleStatus استفاده کنید
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var discountCode = await _context.DiscountCodes.FindAsync(id);
            if (discountCode == null)
            {
                TempData["ErrorMessage"] = "کد تخفیف یافت نشد.";
                return RedirectToAction(nameof(Index));
            }

            // بررسی اینکه آیا این کد قبلا استفاده شده یا خیر (اختیاری)
            // if(discountCode.CurrentUses > 0) {
            //     TempData["ErrorMessage"] = $"کد '{discountCode.Code}' قبلا استفاده شده و قابل حذف کامل نیست. می‌توانید آن را غیرفعال کنید.";
            //     return RedirectToAction(nameof(Index));
            // }

            try
            {
                _context.DiscountCodes.Remove(discountCode);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"کد تخفیف '{discountCode.Code}' با موفقیت حذف شد.";
            }
            catch (DbUpdateException ex) // خطاهای مربوط به محدودیت‌های دیتابیس (Foreign Key)
            {
                TempData["ErrorMessage"] = $"خطا در حذف کد تخفیف '{discountCode.Code}'. ممکن است این کد در فاکتورها استفاده شده باشد. ابتدا آن را غیرفعال کنید.";
                // لاگ کردن جزئیات خطا (ex)
                return RedirectToAction(nameof(Index));
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> DiscountCodeExists(string id)
        {
            return await _context.DiscountCodes.AnyAsync(e => e.Id == id);
        }
    }
}
