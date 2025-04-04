using DataLayer.Context;
using Domain.Entities;
using Domain.ViewModels.UserPanel.Tutorials;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace VeilVPN.App.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")] // اگر می‌خواهید فقط کاربران لاگین شده ببینند
    [Area("Admin")] // اگر در بخش ادمین است
    public class TutorialsController : Controller
    {
        private readonly VeilVpnDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment; // برای آپلود عکس

        public TutorialsController(VeilVpnDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // --- نمایش لیست آموزش‌ها (Grid View) ---
        // GET: /Tutorials or /Admin/Tutorials
        public async Task<IActionResult> Index()
        {
            // فقط آموزش‌های منتشر شده را نمایش بده
            var tutorials = await _context.Tutorials
                                          .Where(t => t.IsPublished)
                                          .OrderByDescending(t => t.CreatedAt) // جدیدترین‌ها اول
                                          .Select(t => new TutorialGridViewModel // از ViewModel استفاده می‌کنیم
                                          {
                                              Id = t.Id,
                                              Title = t.Title,
                                              ShortDescription = t.ShortDescription,
                                              CoverImagePath = t.CoverImagePath ?? "/assets/images/blog/img-placeholder.jpg", // تصویر پیش‌فرض اگر نداشت
                                              Category = t.Category
                                          })
                                          .ToListAsync();
            return View(tutorials);
        }

        // --- نمایش جزئیات یک آموزش ---
        // GET: /Tutorials/Details/5 or /Admin/Tutorials/Details/5
        public async Task<IActionResult> Details(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tutorial = await _context.Tutorials
                                         .Where(t => t.Id == id && t.IsPublished) // فقط منتشر شده‌ها قابل مشاهده باشند
                                         .Select(t => new TutorialDetailsViewModel // استفاده از ViewModel
                                         {
                                             Id = t.Id,
                                             Title = t.Title,
                                             Content = t.Content, // محتوای HTML
                                             CoverImagePath = t.CoverImagePath ?? "/assets/images/blog/overview-placeholder.jpg", // تصویر پیش‌فرض
                                             Category = t.Category,
                                             Tags = !string.IsNullOrEmpty(t.Tags) ? t.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList() : new List<string>(),
                                             CreatedAt = t.CreatedAt,
                                             // ایجاد لیست لینک‌های دانلود
                                             DownloadLinks = new List<DownloadLinkViewModel>
                                             {
                                             // فقط لینک‌هایی که هم آدرس و هم متن دارند را اضافه کن
                                             !string.IsNullOrEmpty(t.DownloadLink1) && !string.IsNullOrEmpty(t.DownloadLinkText1)
                                                ? new DownloadLinkViewModel { Url = t.DownloadLink1, Text = t.DownloadLinkText1 } : null,
                                             !string.IsNullOrEmpty(t.DownloadLink2) && !string.IsNullOrEmpty(t.DownloadLinkText2)
                                                ? new DownloadLinkViewModel { Url = t.DownloadLink2, Text = t.DownloadLinkText2 } : null
                                                 // در صورت نیاز لینک‌های بعدی را هم اضافه کنید
                                             }.Where(link => link != null).ToList() // حذف موارد null
                                         })
                                         .FirstOrDefaultAsync();

            if (tutorial == null)
            {
                // اگر آموزش پیدا نشد یا منتشر نشده بود
                return NotFound();
            }

            return View(tutorial);
        }

        // --- نمایش فرم ایجاد آموزش ---
        // GET: /Tutorials/Create or /Admin/Tutorials/Create
        // [Authorize(Roles = "Admin")] // فقط ادمین بتواند ایجاد کند
        public IActionResult Create()
        {
            return View(new TutorialCreateViewModel()); // ارسال ViewModel خالی به ویو
        }

        // --- پردازش فرم ایجاد آموزش ---
        // POST: /Tutorials/Create or /Admin/Tutorials/Create
        // [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TutorialCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                string? uniqueFileName = null;
                if (model.CoverImageFile != null)
                {
                    // 1. مسیر ذخیره سازی عکس‌ها
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "tutorials");
                    // 2. اطمینان از وجود پوشه
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }
                    // 3. ایجاد نام منحصر به فرد برای فایل
                    uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(model.CoverImageFile.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    // 4. ذخیره فایل روی سرور
                    try
                    {
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await model.CoverImageFile.CopyToAsync(fileStream);
                        }
                    }
                    catch (Exception ex)
                    {
                        // لاگ کردن خطا و نمایش پیام مناسب به کاربر
                        ModelState.AddModelError("", $"خطا در آپلود تصویر: {ex.Message}");
                        return View(model); // بازگشت به فرم با خطا
                    }
                }

                // مپ کردن ViewModel به Entity
                var tutorial = new Tutorial
                {
                    Title = model.Title,
                    ShortDescription = model.ShortDescription,
                    Content = model.Content, // محتوای HTML از ویرایشگر
                    CoverImagePath = uniqueFileName != null ? $"/images/tutorials/{uniqueFileName}" : null, // ذخیره مسیر نسبی
                    Category = model.Category,
                    Tags = model.TagsString, // ذخیره رشته تگ‌ها (می‌توانید پردازش بیشتری انجام دهید)
                    IsPublished = model.IsPublished,
                    DownloadLink1 = model.DownloadLink1,
                    DownloadLinkText1 = model.DownloadLinkText1,
                    DownloadLink2 = model.DownloadLink2,
                    DownloadLinkText2 = model.DownloadLinkText2,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Add(tutorial);
                await _context.SaveChangesAsync();
                // TempData["SuccessMessage"] = "آموزش با موفقیت ایجاد شد."; // پیام موفقیت آمیز (اختیاری)
                return RedirectToAction(nameof(Index)); // ریدایرکت به لیست آموزش‌ها
            }

            // اگر ModelState معتبر نبود، فرم را با خطاها نمایش بده
            return View(model);
        }

        // --- TODO: اکشن‌های Edit (GET & POST) و Delete (POST) را مشابه Create پیاده‌سازی کنید ---
        // Edit(GET): خواندن داده از دیتابیس، مپ کردن به EditViewModel، نمایش فرم
        // Edit(POST): ولیدیشن، آپلود عکس (در صورت تغییر)، آپدیت داده در دیتابیس، ریدایرکت
        // Delete(POST): پیدا کردن رکورد، حذف از دیتابیس، ریدایرکت

    }
}
