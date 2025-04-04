using DataLayer.Context;
using Domain.Entities.Account;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;


namespace DataLayer.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly VeilVpnDbContext _context;

        public UserRepository(VeilVpnDbContext context)
        {
            _context = context;
        }

        public async Task AddUser(User user)
        {
            _context.Add(user);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteUser(User user)
        {
            User user1 = _context.Users.Find(user.Id)!;
            user1.IsDelete = true;
            _context.Users.Update(user1);
            await _context.SaveChangesAsync();
        }

        public async Task<List<User>> GetAllUsers()
        {
            return await _context.Users.ToListAsync();
        }

        public async Task<User> GetUserByEmail(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email)!;
        }

        public async Task<User> GetUserById(string id)
        {
            return await _context.Users.SingleOrDefaultAsync(a=>a.Id == id)!;
        }

        public async Task<bool> IsExistUserByEmail(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }

        public async Task<bool> IsExistUserById(string id)
        {
            return await (_context.Users.AnyAsync(u => u.Id == id));
        }

        public async Task UpdateUser(User user)
        {
            _context.Update(user);
            await _context.SaveChangesAsync();
        }

        // *** پیاده‌سازی متد جدید ***
        public async Task<List<User>> GetUsersInRoleAsync(string roleName)
        {
            // فرض: پراپرتی نقش در User اسمش Role هست و از نوع string
            // اگر ساختار نقش‌ها فرق داره (مثلا جدول جدا یا Claims)، این کوئری باید تغییر کنه
            // مهم: به بزرگی و کوچکی حروف نقش توجه کن (شاید نیاز به ToLower() باشه)
            return await _context.Users
                .Where(u => u.Role == roleName && !u.IsDelete) // نقش رو چک کن و حذف نشده‌ها رو بیار
                .ToListAsync();
        }
    }
}
