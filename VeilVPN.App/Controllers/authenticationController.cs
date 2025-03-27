using Application.Services.Interfaces;
using Domain.ViewModels.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VeilVPN.App.Filters;
using Application.Security;

namespace VeilVPN.App.Controllers
{
    // اصلاح نام کلاس به AuthenticationController
    public class AuthenticationController : Controller
    {
        private readonly IUserService _userService;
        private readonly PasswordHasher _passwordHasher;

        public AuthenticationController(IUserService userService, PasswordHasher passwordHasher)
        {
            _userService = userService;
            _passwordHasher = passwordHasher;
        }

        #region SignUp

        [RedirectIfAuthenticated]
        public IActionResult SignUp()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RedirectIfAuthenticated]
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

                // Redirect to login page with success message
                return this.RedirectWithSuccess("ثبت نام با موفقیت انجام شد. اکنون می‌توانید وارد شوید", "SignIn", "Authentication");
            }

            return View(model);
        }

        #endregion

        #region SignIn

        [RedirectIfAuthenticated]
        public IActionResult SignIn()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RedirectIfAuthenticated]
        public async Task<IActionResult> SignIn(SignInViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if user exists
                var user = await _userService.GetUserByEmail(model.Email);
                if (user == null)
                {
                    this.ShowError("ایمیل یا رمز عبور اشتباه است");
                    return View(model);
                }

                // Check password
                if (!_passwordHasher.VerifyPassword(model.Password, user.Password))
                {
                    this.ShowError("ایمیل یا رمز عبور اشتباه است");
                    return View(model);
                }

                // Check if user is active
                if (!user.IsActive)
                {
                    this.ShowError("حساب کاربری شما غیر فعال شده است");
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

                await HttpContext.SignInAsync(principal, properties);

                // Check if user is admin
                if (user.IsAdmin || user.Role == "Admin")
                {
                    this.ShowSuccess("ورود به پنل مدیریت");
                    // Redirect to dashboard
                    return RedirectToAction("Index", "Home", new { area = "Admin" });
                }

                this.ShowSuccess("خوش آمدید");
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

        #region SignOut

        [HttpGet]
        public async Task<IActionResult> SignOut()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            this.ShowSuccess("شما با موفقیت خارج شدید");
            return RedirectToAction("SignIn", "Authentication");
        }

        #endregion
    }
}
