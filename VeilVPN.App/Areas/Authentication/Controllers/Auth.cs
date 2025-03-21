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
using VeilVPN.App.Filters;

namespace VeilVPN.App.Areas.Authentication.Controllers
{
    [Area("Authentication")]
    [RedirectIfAuthenticated]
    public class Auth : Controller
    {
        private readonly IUserService _userService;
        private readonly PasswordHasher _passwordHasher;

        public Auth(IUserService userService, PasswordHasher passwordHasher)
        {
            _userService = userService;
            _passwordHasher = passwordHasher;
        }

        #region SignUp

        public IActionResult SignUp()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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
                await _userService.AddUser(new Domain.Entities.Account.User
                {
                    Email = model.Email,
                    Password = _passwordHasher.HashPassword(model.Password)
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
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SignIn(SignInViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if user exists
                var user = _userService.GetUserByEmail(model.Email).Result;
                if (user == null)
                {
                    this.ShowToast("ایمیل یا رمز عبور اشتباه است", "error");
                    return View(model);
                }
                // Check password
                if (!_passwordHasher.VerifyPassword(model.Password,user.Password))
                {
                    this.ShowToast("ایمیل یا رمز عبور اشتباه است", "error");
                    return View(model);
                }
                // Check if user is active
                if (!user.IsActive)
                {
                    this.ShowToast("حساب کاربری شما غیر فعال شده است!", "error");
                    return View(model);
                }

                // Create Login Cookie
                var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Id.ToString()),
            new Claim(ClaimTypes.Role, user.Role) // اضافه کردن نقش کاربر
        };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);
                var properties = new AuthenticationProperties { IsPersistent = model.IsRememberMe };

                HttpContext.SignInAsync(principal, properties);

                // Check if user is admin
                if (user.IsAdmin || user.Role == "Admin")
                {
                    this.ShowToast("ورود به پنل مدیریت", "success");
                    // Redirect to dashboard
                    return RedirectToAction("Index", "Home", new { area = "Admin" });
                }

                this.ShowToast("ورود موفقیت آمیز", "success");
                // Redirect to dashboard
                return RedirectToAction("Index", "Panel", new { area = "UserPanel" });
            }
            return View(model);
        }
        #endregion

        #region AccessDenied

        public IActionResult AccessDenied()
        {
            return View();
        }

        #endregion
    }
}
