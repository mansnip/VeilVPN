using Microsoft.AspNetCore.Mvc;

namespace VeilVPN.App.Controllers
{
    public static class ControllerExtensions
    {
        /// <summary>
        /// نمایش پیام با نوع دلخواه
        /// </summary>
        /// <param name="controller">کنترلر جاری</param>
        /// <param name="message">متن پیام</param>
        /// <param name="type">نوع پیام (success, error, warning, info)</param>
        public static void ShowToast(this Controller controller, string message, string type = "success")
        {
            controller.TempData["ToastMessage"] = message;
            controller.TempData["ToastType"] = type;
        }

        /// <summary>
        /// نمایش پیام موفقیت
        /// </summary>
        /// <param name="controller">کنترلر جاری</param>
        /// <param name="message">متن پیام</param>
        public static void ShowSuccess(this Controller controller, string message)
        {
            controller.ShowToast(message, "success");
        }

        /// <summary>
        /// نمایش پیام خطا
        /// </summary>
        /// <param name="controller">کنترلر جاری</param>
        /// <param name="message">متن پیام</param>
        public static void ShowError(this Controller controller, string message)
        {
            controller.ShowToast(message, "error");
        }

        /// <summary>
        /// نمایش پیام هشدار
        /// </summary>
        /// <param name="controller">کنترلر جاری</param>
        /// <param name="message">متن پیام</param>
        public static void ShowWarning(this Controller controller, string message)
        {
            controller.ShowToast(message, "warning");
        }

        /// <summary>
        /// نمایش پیام اطلاع‌رسانی
        /// </summary>
        /// <param name="controller">کنترلر جاری</param>
        /// <param name="message">متن پیام</param>
        public static void ShowInfo(this Controller controller, string message)
        {
            controller.ShowToast(message, "info");
        }

        /// <summary>
        /// نمایش پیغام و هدایت به اکشن دیگر
        /// </summary>
        /// <param name="controller">کنترلر جاری</param>
        /// <param name="message">متن پیام</param>
        /// <param name="type">نوع پیام</param>
        /// <param name="actionName">نام اکشن</param>
        /// <param name="routeValues">مقادیر مسیر</param>
        /// <returns>نتیجه هدایت به اکشن</returns>
        public static IActionResult RedirectWithToast(this Controller controller, string message, string type, string actionName, object routeValues = null)
        {
            controller.ShowToast(message, type);
            return controller.RedirectToAction(actionName, routeValues);
        }

        /// <summary>
        /// نمایش پیغام موفقیت و هدایت به اکشن دیگر
        /// </summary>
        /// <param name="controller">کنترلر جاری</param>
        /// <param name="message">متن پیام</param>
        /// <param name="actionName">نام اکشن</param>
        /// <param name="routeValues">مقادیر مسیر</param>
        /// <returns>نتیجه هدایت به اکشن</returns>
        public static IActionResult RedirectWithSuccess(this Controller controller, string message, string actionName, object routeValues = null)
        {
            return controller.RedirectWithToast(message, "success", actionName, routeValues);
        }

        /// <summary>
        /// نمایش پیغام خطا و هدایت به اکشن دیگر
        /// </summary>
        /// <param name="controller">کنترلر جاری</param>
        /// <param name="message">متن پیام</param>
        /// <param name="actionName">نام اکشن</param>
        /// <param name="routeValues">مقادیر مسیر</param>
        /// <returns>نتیجه هدایت به اکشن</returns>
        public static IActionResult RedirectWithError(this Controller controller, string message, string actionName, object routeValues = null)
        {
            return controller.RedirectWithToast(message, "error", actionName, routeValues);
        }

        /// <summary>
        /// نمایش پیغام هشدار و هدایت به اکشن دیگر
        /// </summary>
        /// <param name="controller">کنترلر جاری</param>
        /// <param name="message">متن پیام</param>
        /// <param name="actionName">نام اکشن</param>
        /// <param name="routeValues">مقادیر مسیر</param>
        /// <returns>نتیجه هدایت به اکشن</returns>
        public static IActionResult RedirectWithWarning(this Controller controller, string message, string actionName, object routeValues = null)
        {
            return controller.RedirectWithToast(message, "warning", actionName, routeValues);
        }

        /// <summary>
        /// نمایش پیغام اطلاع‌رسانی و هدایت به اکشن دیگر
        /// </summary>
        /// <param name="controller">کنترلر جاری</param>
        /// <param name="message">متن پیام</param>
        /// <param name="actionName">نام اکشن</param>
        /// <param name="routeValues">مقادیر مسیر</param>
        /// <returns>نتیجه هدایت به اکشن</returns>
        public static IActionResult RedirectWithInfo(this Controller controller, string message, string actionName, object routeValues = null)
        {
            return controller.RedirectWithToast(message, "info", actionName, routeValues);
        }
    }
}