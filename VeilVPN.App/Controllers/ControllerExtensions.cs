using Microsoft.AspNetCore.Mvc;

namespace VeilVPN.App.Controllers
{
    public static class ControllerExtensions
    {
        public static void ShowToast(this Controller controller, string message, string type = "success")
        {
            controller.TempData["ToastMessage"] = message;
            controller.TempData["ToastType"] = type;
        }
    }
}
