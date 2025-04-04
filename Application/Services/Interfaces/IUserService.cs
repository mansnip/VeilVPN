using Domain.Entities.Account;


namespace Application.Services.Interfaces
{
    public interface IUserService
    {
        // user in database
        Task AddUser(User user);
        Task UpdateUser(User user);
        Task DeleteUser(User user);
        Task<User> GetUserById(string id);
        Task<User> GetUserByEmail(string email);
        Task<List<User>> GetAllUsers();
        Task<bool> IsExistUserByEmail(string email);
        Task<bool> IsExistUserById(string id);

        // *** متد جدید برای چک کردن نقش کاربر ***
        Task<bool> IsUserInRoleAsync(string userId, string roleName);

        // *** متد جدید برای گرفتن کاربران با نقش خاص ***
        Task<List<User>> GetUsersInRoleAsync(string roleName);
    }
}
