using Domain.Entities.Account;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
