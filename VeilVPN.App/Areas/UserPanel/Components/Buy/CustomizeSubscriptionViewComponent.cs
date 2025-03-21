using Application.Services.Interfaces;
using Domain.ViewModels.UserPanel;
using Microsoft.AspNetCore.Mvc;

namespace VeilVPN.App.Areas.UserPanel.Components.Buy
{
    public class CustomizeSubscriptionViewComponent : ViewComponent
    {
        private readonly IUserService _userService;

        public CustomizeSubscriptionViewComponent(IUserService userService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        }

        public IViewComponentResult Invoke()
        {
            var model = new SubscriptionModel
            {
                Traffic = 30,
                Duration = 30
            };

            return View("CustomizeSubscription", model);
        }
    }
}
