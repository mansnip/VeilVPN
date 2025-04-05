using DataLayer.Context;
using Domain.Entities;
using Domain.ViewModels.UserPanel.Tutorials;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace VeilVPN.App.Areas.UserPanel.Controllers
{
    [Area("UserPanel")] // اگر در بخش ادمین است
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
            // 1. واکشی داده‌های خام لازم از دیتابیس
            var tutorialData = await _context.Tutorials
                                         .Where(t => t.Id == id && t.IsPublished)
                                         .Select(t => new // انتخاب به یک نوع ناشناس
                                         {
                                             t.Id,
                                             t.Title,
                                             t.Content,
                                             t.CoverImagePath,
                                             t.Category,
                                             t.Tags, // رشته خام تگ‌ها
                                             t.CreatedAt,
                                             t.DownloadLink1, // لینک‌های خام
                                             t.DownloadLinkText1,
                                             t.DownloadLink2,
                                             t.DownloadLinkText2
                                             // سایر فیلدهای لینک دانلود اگر وجود دارد
                                         })
                                         .FirstOrDefaultAsync();

            // بررسی اینکه آیا آموزشی پیدا شده است
            if (tutorialData == null)
            {
                return NotFound(); // یا نمایش یک صفحه "یافت نشد" مناسب
            }

            // 2. ساخت ViewModel نهایی در حافظه (پس از واکشی از دیتابیس)
            var viewModel = new TutorialDetailsViewModel
            {
                Id = tutorialData.Id,
                Title = tutorialData.Title,
                Content = tutorialData.Content, // محتوای HTML
                CoverImagePath = tutorialData.CoverImagePath ?? "/assets/images/blog/overview-placeholder.jpg", // تصویر پیش‌فرض
                Category = tutorialData.Category,
                CreatedAt = tutorialData.CreatedAt,

                // پردازش رشته تگ‌ها در حافظه
                Tags = !string.IsNullOrEmpty(tutorialData.Tags)
                       ? tutorialData.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
                       : new List<string>(), // ایجاد لیست خالی اگر تگ وجود ندارد

                // پردازش و ساخت لیست لینک‌های دانلود در حافظه
                DownloadLinks = new List<DownloadLinkViewModel>() // شروع با لیست خالی
            };

            // اضافه کردن لینک‌ها به صورت شرطی
            if (!string.IsNullOrEmpty(tutorialData.DownloadLink1) && !string.IsNullOrEmpty(tutorialData.DownloadLinkText1))
            {
                viewModel.DownloadLinks.Add(new DownloadLinkViewModel { Url = tutorialData.DownloadLink1, Text = tutorialData.DownloadLinkText1 });
            }
            if (!string.IsNullOrEmpty(tutorialData.DownloadLink2) && !string.IsNullOrEmpty(tutorialData.DownloadLinkText2))
            {
                viewModel.DownloadLinks.Add(new DownloadLinkViewModel { Url = tutorialData.DownloadLink2, Text = tutorialData.DownloadLinkText2 });
            }
            // در صورت نیاز لینک‌های بعدی را هم به همین شکل اضافه کنید...
            // مثال برای لینک 3:
            // if (!string.IsNullOrEmpty(tutorialData.DownloadLink3) && !string.IsNullOrEmpty(tutorialData.DownloadLinkText3))
            // {
            //     viewModel.DownloadLinks.Add(new DownloadLinkViewModel { Url = tutorialData.DownloadLink3, Text = tutorialData.DownloadLinkText3 });
            // }


            // ارسال ViewModel ساخته شده به View
            return View(viewModel);
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
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Create(TutorialCreateViewModel model)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        string? uniqueFileName = null;
        //        if (model.CoverImageFile != null)
        //        {
        //            // 1. مسیر ذخیره سازی عکس‌ها
        //            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "tutorials");
        //            // 2. اطمینان از وجود پوشه
        //            if (!Directory.Exists(uploadsFolder))
        //            {
        //                Directory.CreateDirectory(uploadsFolder);
        //            }
        //            // 3. ایجاد نام منحصر به فرد برای فایل
        //            uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(model.CoverImageFile.FileName);
        //            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

        //            // 4. ذخیره فایل روی سرور
        //            try
        //            {
        //                using (var fileStream = new FileStream(filePath, FileMode.Create))
        //                {
        //                    await model.CoverImageFile.CopyToAsync(fileStream);
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                // لاگ کردن خطا و نمایش پیام مناسب به کاربر
        //                ModelState.AddModelError("", $"خطا در آپلود تصویر: {ex.Message}");
        //                return View(model); // بازگشت به فرم با خطا
        //            }
        //        }

        //        // مپ کردن ViewModel به Entity
        //        var tutorial = new Tutorial
        //        {
        //            Title = model.Title,
        //            ShortDescription = model.ShortDescription,
        //            Content = model.Content, // محتوای HTML از ویرایشگر
        //            CoverImagePath = uniqueFileName != null ? $"/images/tutorials/{uniqueFileName}" : null, // ذخیره مسیر نسبی
        //            Category = model.Category,
        //            Tags = model.TagsString, // ذخیره رشته تگ‌ها (می‌توانید پردازش بیشتری انجام دهید)
        //            IsPublished = model.IsPublished,
        //            DownloadLink1 = model.DownloadLink1,
        //            DownloadLinkText1 = model.DownloadLinkText1,
        //            DownloadLink2 = model.DownloadLink2,
        //            DownloadLinkText2 = model.DownloadLinkText2,
        //            CreatedAt = DateTime.UtcNow
        //        };

        //        _context.Add(tutorial);
        //        await _context.SaveChangesAsync();
        //        // TempData["SuccessMessage"] = "آموزش با موفقیت ایجاد شد."; // پیام موفقیت آمیز (اختیاری)
        //        return RedirectToAction(nameof(Index)); // ریدایرکت به لیست آموزش‌ها
        //    }

        //    // اگر ModelState معتبر نبود، فرم را با خطاها نمایش بده
        //    return View(model);
        //}

        // --- TODO: اکشن‌های Edit(GET & POST) و Delete(POST) را مشابه Create پیاده‌سازی کنید ---
        // Edit(GET) : خواندن داده از دیتابیس، مپ کردن به EditViewModel، نمایش فرم
        // Edit(POST) : ولیدیشن، آپلود عکس(در صورت تغییر)، آپدیت داده در دیتابیس، ریدایرکت
        // Delete(POST): پیدا کردن رکورد، حذف از دیتابیس، ریدایرکت

    }
}
