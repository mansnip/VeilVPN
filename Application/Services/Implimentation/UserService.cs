using Application.Services.Interfaces;
using Domain.Entities.Account;
using Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Implimentation
{
    public class UserService : IUserService
    {
        public readonly IUserRepository _UserReposytory;

        public UserService(IUserRepository userReposytory) { _UserReposytory = userReposytory; }

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
    }
}
