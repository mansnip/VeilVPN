using Application.Security;
using Application.Services.Interfaces;
using Domain.Entities.Account;
using Domain.ViewModels.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using VeilVPN.App.Controllers;

namespace VeilVPN.App.Areas.Authentication.Controllers
{
    [Area("Authentication")]
    public class Auth : Controller
    {
        private readonly IUserService _userService;

        public Auth(IUserService userService)
        {
            _userService = userService;
        }

        #region SignUp

        public IActionResult SignUp()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SignUp(SignUpViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if user exists
                if (await _userService.IsExistUserByEmail(model.Email))
                {
                    ModelState.AddModelError("Email", "این ایمیل قبلا ثبت شده است");
                    return View(model);
                }

                // Add user to database
                await _userService.AddUser(new User
                {
                    Email = model.Email,
                    Password = PasswordHelper.EncodePasswordMD5(model.Password)
                });

                // Redirect to login page
                return RedirectToAction("SignIn", "Auth");
            }

            return View(model);
        }

        #endregion

        #region SignIn

        public IActionResult SignIn()
        {

            this.ShowToast("ثبت‌نام با موفقیت انجام شد!", "success");

            return View();
        }


        [HttpPost]
        public IActionResult SignIn(SignInViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if user exists
                var user = _userService.GetUserByEmail(model.Email).Result;
                if (user == null)
                {
                    ModelState.AddModelError("Email", "ایمیل یا رمز عبور اشتباه است");
                    return View(model);
                }
                // Check password
                if (user.Password != PasswordHelper.EncodePasswordMD5(model.Password))
                {
                    ModelState.AddModelError("Email", "ایمیل یا رمز عبور اشتباه است");
                    return View(model);
                }
                // Check if user is active
                if (!user.IsActive)
                {
                    ModelState.AddModelError("Email", "حساب کاربری شما غیر فعال است");
                    return View(model);
                }

                // Create Login Cookie
                var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new Claim(ClaimTypes.Name, user.Id.ToString()),
                    };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);
                var properties = new AuthenticationProperties { IsPersistent = model.IsRememberMe };

                HttpContext.SignInAsync(principal, properties);

                // Check if user is admin
                if (user.IsAdmin)
                {
                    // Redirect to dashboard
                    return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                }

                // Redirect to dashboard
                return RedirectToAction("Index", "Dashboard", new { area = "User" });
            }
            return View(model);
        }
        #endregion
    }
}
