using DataLayer.Context;
using Domain.Entities.Account;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
