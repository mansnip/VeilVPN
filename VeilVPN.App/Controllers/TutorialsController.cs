using DataLayer.Context;
using Domain.ViewModels.UserPanel.Tutorials;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace VeilVPN.App.Controllers
{
    public class TutorialsController : Controller
    {
        private readonly VeilVpnDbContext _context; // تزریق وابستگی DbContext

        public TutorialsController(VeilVpnDbContext context)
        {
            _context = context;
        }

        // --- نمایش لیست عمومی آموزش‌ها ---
        // GET: /Tutorials
        public async Task<IActionResult> Index(string? category = null, string? tag = null) // پارامترهای فیلتر (اختیاری)
        {
            // --- 1. واکشی پایه آموزش‌های منتشر شده ---
            var tutorialsQuery = _context.Tutorials
                                         .Where(t => t.IsPublished);

            // --- 2. اعمال فیلتر (اختیاری) ---
            if (!string.IsNullOrEmpty(category))
            {
                tutorialsQuery = tutorialsQuery.Where(t => t.Category == category);
                ViewData["CurrentFilter"] = $"دسته: {category}"; // برای نمایش در View (اختیاری)
            }
            if (!string.IsNullOrEmpty(tag))
            {
                // جستجو بر اساس تگ نیازمند دقت است اگر تگ‌ها با کاما جدا شده‌اند
                tutorialsQuery = tutorialsQuery.Where(t => t.Tags != null && t.Tags.Contains(tag));
                ViewData["CurrentFilter"] = $"برچسب: {tag}"; // برای نمایش در View (اختیاری)
            }


            // --- 3. واکشی لیست اصلی آموزش‌ها برای نمایش ---
            var tutorialsList = await tutorialsQuery
                                          .OrderByDescending(t => t.CreatedAt)
                                          .Select(t => new TutorialGridViewModel // استفاده از ViewModel قبلی مناسب است
                                          {
                                              Id = t.Id,
                                              Title = t.Title,
                                              ShortDescription = t.ShortDescription,
                                              CoverImagePath = t.CoverImagePath ?? "/assets/images/blog/img-placeholder.jpg",
                                              Category = t.Category // همچنان می‌توانید دسته هر آیتم را نگه دارید
                                          })
                                          .ToListAsync(); // یا .ToPagedListAsync(pageNumber, pageSize) برای صفحه‌بندی

            // --- 4. واکشی داده‌های سایدبار ---

            // --- 4.1. دسته‌بندی‌ها ---
            // گروه‌بندی بر اساس نام دسته و شمارش تعداد هر کدام
            var categories = await _context.Tutorials
                .Where(t => t.IsPublished && !string.IsNullOrEmpty(t.Category)) // فقط دسته‌های معتبر
                .GroupBy(t => t.Category)
                .Select(g => new { CategoryName = g.Key, Count = g.Count() })
                .OrderBy(c => c.CategoryName) // مرتب‌سازی بر اساس نام
                .ToListAsync();
            ViewBag.Categories = categories; // ارسال به View از طریق ViewBag

            // --- 4.2. پست‌های محبوب (مثلاً 5 تای آخر) ---
            // تعریف "محبوب" در اینجا به معنی "جدیدترین" است، می‌توانید منطق دیگری پیاده کنید
            var popularTutorials = await _context.Tutorials
                .Where(t => t.IsPublished)
                .OrderByDescending(t => t.CreatedAt)
                .Take(5) // تعداد دلخواه
                .Select(t => new TutorialGridViewModel
                { // می‌توان از ViewModel دیگری هم استفاده کرد
                    Id = t.Id,
                    Title = t.Title,
                    CoverImagePath = t.CoverImagePath ?? "/assets/images/blog/img-placeholder.jpg",
                    // CreatedAt = t.CreatedAt // اگر نیاز به تاریخ دارید
                })
                .ToListAsync();
            ViewBag.PopularTutorials = popularTutorials;

            // --- 4.3. آرشیو (بر اساس سال و ماه - ساده شده) ---
            var archive = await _context.Tutorials
                .Where(t => t.IsPublished)
                .GroupBy(t => new { t.CreatedAt.Year }) // فقط بر اساس سال برای سادگی
                .Select(g => new { Year = g.Key.Year, Count = g.Count() })
                .OrderByDescending(a => a.Year)
                .ToListAsync();
            ViewBag.ArchiveData = archive;

            // --- 4.4. برچسب‌ها (Tags) ---
            // واکشی تمام رشته‌های تگ، سپس پردازش در C#
            var allTagsStrings = await _context.Tutorials
                .Where(t => t.IsPublished && !string.IsNullOrEmpty(t.Tags))
                .Select(t => t.Tags)
                .ToListAsync();

            var allTags = allTagsStrings
                .SelectMany(tagString => tagString.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) // شکستن رشته‌ها و ادغام
                .GroupBy(tag => tag) // گروه‌بندی بر اساس نام تگ
                .Select(g => new { TagName = g.Key, Count = g.Count() }) // شمارش هر تگ
                .OrderByDescending(t => t.Count) // مرتب‌سازی بر اساس تعداد (محبوب‌ترین‌ها اول)
                .Take(10) // نمایش تعداد محدودی تگ (اختیاری)
                .ToList();
            ViewBag.Tags = allTags;


            // --- 5. ارسال مدل اصلی (لیست آموزش‌ها) به View ---
            return View(tutorialsList);
        }

        // --- نمایش جزئیات یک آموزش (عمومی) ---
        // GET: /Tutorials/Details/{id}
        public async Task<IActionResult> Details(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // --- منطق واکشی جزئیات دقیقاً مشابه کد قبلی شماست ---
            var tutorialData = await _context.Tutorials
                                         .Where(t => t.Id == id && t.IsPublished)
                                         .Select(t => new
                                         {
                                             t.Id,
                                             t.Title,
                                             t.Content,
                                             t.CoverImagePath,
                                             t.Category,
                                             t.Tags,
                                             t.CreatedAt,
                                             t.DownloadLink1,
                                             t.DownloadLinkText1,
                                             t.DownloadLink2,
                                             t.DownloadLinkText2
                                             // ... سایر لینک‌ها
                                         })
                                         .FirstOrDefaultAsync();

            if (tutorialData == null)
            {
                return NotFound();
            }

            // --- ساخت ViewModel جزئیات (مشابه قبل) ---
            var viewModel = new TutorialDetailsViewModel
            {
                Id = tutorialData.Id,
                Title = tutorialData.Title,
                Content = tutorialData.Content,
                CoverImagePath = tutorialData.CoverImagePath ?? "/assets/images/blog/overview-placeholder.jpg",
                Category = tutorialData.Category,
                CreatedAt = tutorialData.CreatedAt,
                Tags = !string.IsNullOrEmpty(tutorialData.Tags)
                       ? tutorialData.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
                       : new List<string>(),
                DownloadLinks = new List<DownloadLinkViewModel>()
            };
            if (!string.IsNullOrEmpty(tutorialData.DownloadLink1) && !string.IsNullOrEmpty(tutorialData.DownloadLinkText1))
                viewModel.DownloadLinks.Add(new DownloadLinkViewModel { Url = tutorialData.DownloadLink1, Text = tutorialData.DownloadLinkText1 });
            if (!string.IsNullOrEmpty(tutorialData.DownloadLink2) && !string.IsNullOrEmpty(tutorialData.DownloadLinkText2))
                viewModel.DownloadLinks.Add(new DownloadLinkViewModel { Url = tutorialData.DownloadLink2, Text = tutorialData.DownloadLinkText2 });
            // ... اضافه کردن سایر لینک‌ها

            // !!! مهم: باید یک View به نام Details.cshtml در پوشه Views/Tutorials داشته باشید !!!
            return View(viewModel); // ارسال به View عمومی جزئیات
        }

        // !!! اکشن‌های Create, Edit, Delete باید در Controller پنل ادمین/کاربر باقی بمانند و Authorize داشته باشند !!!
    }
}
