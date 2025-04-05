using DataLayer.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Text;
using System.Xml;

namespace VeilVPN.App.Controllers
{
    public class SitemapController : Controller
    {
        private readonly SiteSettings _siteSettings;
        private readonly VeilVpnDbContext _context;

        public SitemapController(IOptions<SiteSettings> siteSettings, VeilVpnDbContext context)
        {
            _siteSettings = siteSettings.Value;
            _context = context;
        }

        [Route("sitemap.xml")]
        [ResponseCache(Duration = 86400)]
        public async Task<IActionResult> Index()
        {
            var sb = new StringBuilder();
            await using (var xmlWriter = XmlWriter.Create(sb, new XmlWriterSettings
            {
                Indent = true,
                Encoding = Encoding.UTF8,
                Async = true
            }))
            {
                await xmlWriter.WriteStartDocumentAsync();
                await xmlWriter.WriteStartElementAsync(null, "urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");

                // --- صفحه اصلی Landing (که شامل همه بخش‌ها است) ---
                await WriteUrlAsync(xmlWriter, GetUrl("/"), DateTime.Now.ToString("yyyy-MM-dd"), "weekly", "1.0");

                // --- صفحات واقعا جداگانه ---
                // صفحات کمکی (این‌ها معمولا صفحات جداگانه‌ای هستند)
                await WriteUrlAsync(xmlWriter, GetUrl("/terms"), DateTime.Now.AddDays(-60).ToString("yyyy-MM-dd"), "yearly", "0.4");
                await WriteUrlAsync(xmlWriter, GetUrl("/privacy"), DateTime.Now.AddDays(-60).ToString("yyyy-MM-dd"), "yearly", "0.4");

                // صفحات احراز هویت (این‌ها هم قطعا جدا هستند)
                await WriteUrlAsync(xmlWriter, GetUrl("/authentication/signin"), DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd"), "monthly", "0.7");
                await WriteUrlAsync(xmlWriter, GetUrl("/authentication/signup"), DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd"), "monthly", "0.8");

                // --- بخش آموزش‌ها (که جداگانه هستند) ---
                string tutorialsIndexPath = "/UserPanel/Tutorials";
                await WriteUrlAsync(xmlWriter,
                                 GetUrl(tutorialsIndexPath),
                                 DateTime.Now.ToString("yyyy-MM-dd"),
                                 "weekly",
                                 "0.9");

                var publishedTutorials = await _context.Tutorials
                                                     .AsNoTracking()
                                                     .Where(t => t.IsPublished)
                                                     .Select(t => new { t.Id, t.CreatedAt }) // یا تاریخ آپدیت اگر دارید
                                                     .ToListAsync();

                foreach (var tutorial in publishedTutorials)
                {
                    string tutorialDetailsPath = $"{tutorialsIndexPath}/Details/{tutorial.Id}";
                    await WriteUrlAsync(xmlWriter,
                                     GetUrl(tutorialDetailsPath),
                                     tutorial.CreatedAt.ToString("yyyy-MM-dd"), // بهتر است تاریخ آپدیت باشد
                                     "monthly",
                                     "0.8");
                }
                // --- پایان بخش آموزش‌ها ---

                // --- سایر صفحات واقعا جداگانه (اگر دارید) ---
                // مثلا اگر صفحه /Contact جداست:
                // await WriteUrlAsync(xmlWriter, GetUrl("/contact"), DateTime.Now.AddDays(-10).ToString("yyyy-MM-dd"), "monthly", "0.5");

                await xmlWriter.WriteEndElementAsync(); // بستن urlset
                await xmlWriter.WriteEndDocumentAsync();
                await xmlWriter.FlushAsync();
            }

            return Content(sb.ToString(), "application/xml", Encoding.UTF8);
        }

        private string GetUrl(string relativeUrl)
        {
            var domain = _siteSettings.Domain.TrimEnd('/');
            var urlPath = relativeUrl.StartsWith("/") ? relativeUrl : "/" + relativeUrl;
            return $"{domain}{urlPath}";
        }

        private async Task WriteUrlAsync(XmlWriter xmlWriter, string url, string lastModified, string changeFrequency, string priority)
        {
            await xmlWriter.WriteStartElementAsync(null, "url", null);
            await xmlWriter.WriteElementStringAsync(null, "loc", null, url);
            await xmlWriter.WriteElementStringAsync(null, "lastmod", null, lastModified);
            await xmlWriter.WriteElementStringAsync(null, "changefreq", null, changeFrequency);
            await xmlWriter.WriteElementStringAsync(null, "priority", null, priority);
            await xmlWriter.WriteEndElementAsync();
        }
    }
}