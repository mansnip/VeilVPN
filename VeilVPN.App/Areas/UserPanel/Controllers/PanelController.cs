using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace VeilVPN.App.Areas.UserPanel.Controllers
{
    [Area("UserPanel")]
    public class PanelController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.SignOutAsync();
            return RedirectToAction("SignIn", "Auth", new { area = "Authentication" });
        }

        public IActionResult ChangePassword()
        {
            return View();
        }

        // Buy a new subscription
        public IActionResult Buy()
        {
            return View();
        }
    }
}
