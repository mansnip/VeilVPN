using Application.Services.Interfaces;
using Domain.Entities.Account;
using Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

public class UserService : IUserService
{
    private readonly IUserRepository _UserReposytory;

    public UserService(IUserRepository userReposytory)
    {
        _UserReposytory = userReposytory;
    }




    // متدهای قبلی شما
    public async Task AddUser(User user)
    {
        await _UserReposytory.AddUser(user);
    }

    public async Task DeleteUser(User user)
    {
        await _UserReposytory.DeleteUser(user);
    }

    public async Task<List<User>> GetAllUsers()
    {
        return await _UserReposytory.GetAllUsers();
    }

    public async Task<User> GetUserByEmail(string email)
    {
        return await _UserReposytory.GetUserByEmail(email);
    }

    public async Task<User> GetUserById(string id)
    {
        return await _UserReposytory.GetUserById(id);
    }

    public async Task<bool> IsExistUserByEmail(string email)
    {
        return await _UserReposytory.IsExistUserByEmail(email);
    }

    public async Task<bool> IsExistUserById(string id)
    {
        return await _UserReposytory.IsExistUserById(id);
    }

    public async Task UpdateUser(User user)
    {
        await _UserReposytory.UpdateUser(user);
    }

    // *** پیاده‌سازی متد جدید چک کردن نقش ***
    public async Task<bool> IsUserInRoleAsync(string userId, string roleName)
    {
        var user = await _UserReposytory.GetUserById(userId);
        if (user == null)
        {
            return false; // کاربر وجود ندارد
        }

        // فرض: پراپرتی نقش در User اسمش Role هست و از نوع string
        // به بزرگی/کوچکی حروف دقت کنید
        // return user.Role == roleName; // حالت ساده
        return string.Equals(user.Role, roleName, StringComparison.OrdinalIgnoreCase); // مقایسه بدون حساسیت به حروف
    }

    // *** پیاده‌سازی متد جدید گرفتن کاربران با نقش ***
    public async Task<List<User>> GetUsersInRoleAsync(string roleName)
    {
        return await _UserReposytory.GetUsersInRoleAsync(roleName);
    }
}
