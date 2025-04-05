using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace VeilVPN.App.Controllers
{
    public class RobotsController : Controller
    {
        private readonly SiteSettings _siteSettings;

        public RobotsController(IOptions<SiteSettings> siteSettings)
        {
            _siteSettings = siteSettings.Value;
        }

        [Route("robots.txt")]
        [ResponseCache(Duration = 86400)] // کش برای یک روز
        public ContentResult RobotsTxt()
        {
            string content = "User-agent: *\n" +
                           "Allow: /\n" +
                           "Disallow: /authentication/admin/\n" +
                           "Disallow: /admin/\n" +
                           "Disallow: /api/\n" +
                           "Disallow: /private/\n\n" +
                           $"Sitemap: {_siteSettings.Domain}/sitemap.xml";

            return Content(content, "text/plain");
        }
    }
}