using Microsoft.AspNetCore.Mvc;

namespace VeilVPN.App.Areas.Authentication.Controllers
{
    [Area("Authentication")]
    public class Auth : Controller
    {
        public IActionResult SignUp()
        {
            return View();
        }
    }
}
