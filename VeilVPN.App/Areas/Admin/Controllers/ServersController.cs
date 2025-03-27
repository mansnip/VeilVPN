
using Application.API;
using Application.Services.Interfaces;
using Domain.Entities.VPN;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using VeilVPN.App.Controllers;

namespace VeilVPN.App.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ServersController : Controller
    {
        private readonly IServerVPNService _serverVPNService;

        public ServersController(IServerVPNService serverVPNService)
        {
            _serverVPNService = serverVPNService;
        }

        public async Task<IActionResult> Index()
        {
            var servers = await _serverVPNService.GetAllServersAsync();
            return View(servers);
        }

        #region Create

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VPNServer server)
        {
            if (!ModelState.IsValid)
            {
                return View(server);
            }

            await _serverVPNService.CreateServerAsync(server);
            return this.RedirectWithSuccess("سرور VPN جدید با موفقیت ثبت شد", nameof(Index));
        }

        #endregion

        #region Edit

        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return this.RedirectWithError("شناسه سرور نامعتبر است", nameof(Index));
            }

            var server = await _serverVPNService.GetServerByIdAsync(id);
            if (server == null)
            {
                return this.RedirectWithError("سرور مورد نظر یافت نشد", nameof(Index));
            }

            return View(server);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(VPNServer server)
        {
            if (!ModelState.IsValid)
            {
                return View(server);
            }

            var existingServer = await _serverVPNService.GetServerByIdAsync(server.Id);
            if (existingServer == null)
            {
                return this.RedirectWithError("سرور مورد نظر یافت نشد", nameof(Index));
            }

            // اگر رمز عبور جدیدی وارد نشده، از رمز قبلی استفاده کن
            if (string.IsNullOrEmpty(server.ApiPassword))
            {
                server.ApiPassword = existingServer.ApiPassword;
            }
            server.Id = existingServer.Id;
            if (!await _serverVPNService.UpdateServerAsync(server))
            {
                return this.RedirectWithError("خطا در بروزرسانی اطلاعات سرور", nameof(Index));
            }

            return this.RedirectWithSuccess("اطلاعات سرور VPN با موفقیت بروزرسانی شد", nameof(Index));
        }

        #endregion

        #region Details

        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return this.RedirectWithError("شناسه سرور نامعتبر است", nameof(Index));
            }

            var server = await _serverVPNService.GetServerByIdAsync(id);
            if (server == null)
            {
                return this.RedirectWithError("سرور مورد نظر یافت نشد", nameof(Index));
            }

            return View(server);
        }

        #endregion

        #region Delete

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return Json(new { success = false, message = "شناسه سرور نامعتبر است" });
            }

            var result = await _serverVPNService.DeleteServerAsync(id);
            if (result)
            {
                return Json(new { success = true });
            }
            else
            {
                return Json(new { success = false, message = "خطا در حذف سرور" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMultiple(List<string> ids)
        {
            if (ids == null || !ids.Any())
            {
                return Json(new { success = false, message = "هیچ سروری انتخاب نشده است" });
            }

            var result = await _serverVPNService.DeleteMultipleServersAsync(ids);
            if (result)
            {
                return Json(new { success = true });
            }
            else
            {
                return Json(new { success = false, message = "خطا در حذف سرورها" });
            }
        }

        #endregion

        #region GetServerName

        [HttpGet]
        public async Task<IActionResult> GetServerName(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                    return Json(new { success = false, message = "شناسه سرور نامعتبر است" });

                var server = await _serverVPNService.GetServerByIdAsync(id);
                if (server == null)
                    return Json(new { success = false, message = "سرور یافت نشد" });

                return Json(new { success = true, name = server.Name });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "خطایی در دریافت اطلاعات سرور رخ داد" });
            }
        }

        #endregion
    }
}