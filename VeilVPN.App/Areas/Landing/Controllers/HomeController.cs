using Microsoft.AspNetCore.Mvc;

namespace VeilVPN.App.Areas.Landing.Controllers
{
    [Area("Landing")]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
