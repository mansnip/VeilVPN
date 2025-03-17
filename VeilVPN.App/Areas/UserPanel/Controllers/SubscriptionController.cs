using Application.Services.Interfaces;
using Domain.ViewModels.UserPanel;
using Microsoft.AspNetCore.Mvc;

namespace VeilVPN.App.Areas.UserPanel.Controllers
{
    public class SubscriptionController : Controller
    {
        private readonly ISubscriptionService _subscriptionService;

        public SubscriptionController(ISubscriptionService subscriptionService)
        {
            _subscriptionService = subscriptionService;
        }

        [HttpPost]
        public async Task<IActionResult> CalculatePrice([FromBody] SubscriptionModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var price = await _subscriptionService.CalculatePriceAsync(model.Traffic, model.Duration);
            return Json(new { price });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Purchase(SubscriptionModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // محاسبه قیمت
            var price = await _subscriptionService.CalculatePriceAsync(model.Traffic, model.Duration);

            // ایجاد اشتراک
            var success = await _subscriptionService.CreateSubscriptionAsync(model);

            if (success)
                return RedirectToAction("Success", new { price });
            else
                return View("Error");
        }
    }
}
