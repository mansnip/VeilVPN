using Microsoft.AspNetCore.Mvc;

namespace VeilVPN.App.Controllers
{
    public class BaseController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
