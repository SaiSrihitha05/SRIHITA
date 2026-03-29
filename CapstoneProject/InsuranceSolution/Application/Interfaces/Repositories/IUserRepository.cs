using Domain.Entities;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Application.Interfaces.Repositories
{

    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email);
        Task AddAsync(User user);
        Task SaveChangesAsync();
        Task<User?> GetByIdAsync(int id);
        Task<IEnumerable<User>> GetByIdsAsync(IEnumerable<int> ids);
        Task<IEnumerable<User>> GetAllAsync();
        Task<IEnumerable<User>> GetByRoleAsync(UserRole role);
        void Update(User user);
        void Delete(User user); 
        Task UpdateResetTokenAsync(int userId, string token, DateTime expiry);
        Task<User?> GetUserByResetTokenAsync(string token);
    }
}
