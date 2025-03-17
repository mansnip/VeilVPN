using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace VeilVPN.App.Filters
{
    public class RedirectIfAuthenticatedAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var user = context.HttpContext.User;

            // بررسی وضعیت لاگین کاربر
            if (user.Identity != null && user.Identity.IsAuthenticated)
            {
                // اگر کاربر لاگین است، او را به صفحه مورد نظر منتقل کنید
                context.Result = new RedirectToActionResult("Index", "Panel", new { area = "UserPanel" });
            }

            base.OnActionExecuting(context);
        }
    }
}