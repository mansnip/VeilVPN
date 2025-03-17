using Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace VeilVPN.App.Areas.UserPanel.Components.Buy
{
    public class CustomizeSubscriptionViewComponent : ViewComponent
    {
        private IUserService _userService;
        public CustomizeSubscriptionViewComponent(IUserService userService)
        {
            _userService = userService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            return View("CustomizeSubscription");
        }


    }
}
