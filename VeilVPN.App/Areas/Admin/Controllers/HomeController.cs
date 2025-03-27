using Application.API;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Mvc;

namespace VeilVPN.App.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class HomeController : Controller
    {
        private readonly IServerVPNService _serverVPNService;
        private readonly ApiManager _apiManager;
        public HomeController(IServerVPNService serverVPNService, ApiManager apiManager)
        {
            _serverVPNService = serverVPNService;
            _apiManager = apiManager;
        }
        public IActionResult Index()
        {
            return View();
        }
    }
}
