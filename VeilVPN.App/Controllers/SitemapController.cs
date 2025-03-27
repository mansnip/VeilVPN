using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Text;
using System.Xml;

namespace VeilVPN.App.Controllers
{
    public class SitemapController : Controller
    {
        private readonly SiteSettings _siteSettings;

        public SitemapController(IOptions<SiteSettings> siteSettings)
        {
            _siteSettings = siteSettings.Value;
        }

        [Route("sitemap.xml")]
        [ResponseCache(Duration = 86400)] // کش برای یک روز
        public IActionResult Index()
        {
            var sb = new StringBuilder();
            var xmlWriter = XmlWriter.Create(sb, new XmlWriterSettings
            {
                Indent = true,
                Encoding = Encoding.UTF8
            });

            xmlWriter.WriteStartDocument();
            xmlWriter.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");

            // صفحه اصلی
            WriteUrl(xmlWriter,
                     GetUrl("/"),
                     DateTime.Now.ToString("yyyy-MM-dd"),
                     "weekly",
                     "1.0");

            // صفحات اصلی
            WriteUrl(xmlWriter, GetUrl("/features"), DateTime.Now.AddDays(-5).ToString("yyyy-MM-dd"), "monthly", "0.8");
            WriteUrl(xmlWriter, GetUrl("/pricing"), DateTime.Now.AddDays(-5).ToString("yyyy-MM-dd"), "monthly", "0.9");
            WriteUrl(xmlWriter, GetUrl("/servers"), DateTime.Now.AddDays(-7).ToString("yyyy-MM-dd"), "weekly", "0.7");
            WriteUrl(xmlWriter, GetUrl("/download"), DateTime.Now.AddDays(-2).ToString("yyyy-MM-dd"), "monthly", "0.9");

            // صفحات کمکی
            WriteUrl(xmlWriter, GetUrl("/faq"), DateTime.Now.AddDays(-20).ToString("yyyy-MM-dd"), "monthly", "0.6");
            WriteUrl(xmlWriter, GetUrl("/support"), DateTime.Now.AddDays(-25).ToString("yyyy-MM-dd"), "monthly", "0.6");
            WriteUrl(xmlWriter, GetUrl("/terms"), DateTime.Now.AddDays(-60).ToString("yyyy-MM-dd"), "yearly", "0.4");
            WriteUrl(xmlWriter, GetUrl("/privacy"), DateTime.Now.AddDays(-60).ToString("yyyy-MM-dd"), "yearly", "0.4");

            // صفحات احراز هویت
            WriteUrl(xmlWriter, GetUrl("/authentication/signin"), DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd"), "monthly", "0.7");
            WriteUrl(xmlWriter, GetUrl("/authentication/signup"), DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd"), "monthly", "0.8");

            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndDocument();
            xmlWriter.Flush();
            xmlWriter.Close();

            return Content(sb.ToString(), "application/xml");
        }

        private string GetUrl(string relativeUrl)
        {
            return $"{_siteSettings.Domain}{relativeUrl}";
        }

        private void WriteUrl(XmlWriter xmlWriter, string url, string lastModified, string changeFrequency, string priority)
        {
            xmlWriter.WriteStartElement("url");
            xmlWriter.WriteElementString("loc", url);
            xmlWriter.WriteElementString("lastmod", lastModified);
            xmlWriter.WriteElementString("changefreq", changeFrequency);
            xmlWriter.WriteElementString("priority", priority);
            xmlWriter.WriteEndElement();
        }
    }
}