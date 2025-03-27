using Application.Security;
using Domain.Entities.Account;
using Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeilVPN.App.Controllers;

namespace VeilVPN.App.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly IUserRepository _userRepository;
        private readonly PasswordHasher _passwordHasher;

        public UsersController(IUserRepository userRepository, PasswordHasher passwordHasher)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _userRepository.GetAllUsers();
            return View(users);
        }

        #region Create

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(User user)
        {
            if (ModelState.IsValid)
            {
                // بررسی وجود ایمیل تکراری
                var existingUser = await _userRepository.GetUserByEmail(user.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "این ایمیل قبلاً ثبت شده است");
                    return View(user);
                }

                // هش کردن رمز عبور
                user.Password = _passwordHasher.HashPassword(user.Password);

                // تنظیم نقش کاربر بر اساس IsAdmin
                if (user.IsAdmin)
                {
                    user.Role = "Admin";
                }

                await _userRepository.AddUser(user);
                return this.RedirectWithSuccess("کاربر جدید با موفقیت ایجاد شد", nameof(Index));
            }
            return View(user);
        }

        #endregion

        #region Edit

        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return this.RedirectWithError("شناسه کاربر نامعتبر است", nameof(Index));
            }

            var user = await _userRepository.GetUserById(id);
            if (user == null)
            {
                return this.RedirectWithError("کاربر مورد نظر یافت نشد", nameof(Index));
            }

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(User user, string? NewPassword)
        {
            // اگر رمز عبور جدید خالی باشد، خطای اعتبارسنجی Password را نادیده بگیر
            ModelState.Remove("Password");

            if (ModelState.IsValid)
            {
                var existingUser = await _userRepository.GetUserById(user.Id);
                if (existingUser == null)
                {
                    return this.RedirectWithError("کاربر مورد نظر یافت نشد", nameof(Index));
                }

                // بررسی تغییر ایمیل و وجود ایمیل تکراری
                if (existingUser.Email != user.Email)
                {
                    var emailExists = await _userRepository.GetUserByEmail(user.Email);
                    if (emailExists != null)
                    {
                        ModelState.AddModelError("Email", "این ایمیل قبلاً ثبت شده است");
                        return View(user);
                    }
                    else
                    {
                        existingUser.Email = user.Email;
                    }
                }

                // بررسی تغییر رمز عبور
                if (!string.IsNullOrEmpty(NewPassword))
                {
                    existingUser.Password = _passwordHasher.HashPassword(NewPassword);
                }

                // تنظیم نقش کاربر بر اساس IsAdmin
                if (user.IsAdmin)
                {
                    existingUser.Role = "Admin";
                }
                else if (user.Role == "Admin" && !user.IsAdmin)
                {
                    existingUser.IsAdmin = true;
                }

                if (user.Role != "Admin")
                {
                    existingUser.IsAdmin = false;
                }

                existingUser.PhoneNumber = user.PhoneNumber;
                existingUser.IsActive = user.IsActive;

                await _userRepository.UpdateUser(existingUser);
                return this.RedirectWithSuccess("اطلاعات کاربر با موفقیت به‌روزرسانی شد", nameof(Index));
            }
            return View(user);
        }

        #endregion

        #region Delete

        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return this.RedirectWithError("شناسه کاربر نامعتبر است", nameof(Index));
            }

            var user = await _userRepository.GetUserById(id);
            if (user == null)
            {
                return this.RedirectWithError("کاربر مورد نظر یافت نشد", nameof(Index));
            }

            // حذف نرم
            user.IsDelete = true;
            await _userRepository.UpdateUser(user);

            return this.RedirectWithSuccess("کاربر با موفقیت حذف شد", nameof(Index));
        }

        #endregion
    }
}