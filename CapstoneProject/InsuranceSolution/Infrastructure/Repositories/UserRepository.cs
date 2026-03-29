using Application.Interfaces;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly InsuranceDbContext _context;
        public UserRepository(InsuranceDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByEmailAsync(string email) =>
            await _context.Users.FirstOrDefaultAsync(x => x.Email == email);

        public async Task AddAsync(User user) =>
            await _context.Users.AddAsync(user);

        public async Task SaveChangesAsync() =>
            await _context.SaveChangesAsync();

        public async Task<User?> GetByIdAsync(int id) =>
            await _context.Users.FindAsync(id);

        public async Task<IEnumerable<User>> GetByIdsAsync(IEnumerable<int> ids) =>
            await _context.Users
                .Where(u => ids.Contains(u.Id))
                .ToListAsync();

        public async Task<IEnumerable<User>> GetAllAsync() =>
            await _context.Users.ToListAsync();

        public async Task<IEnumerable<User>> GetByRoleAsync(UserRole role) =>
            await _context.Users
                .Where(u => u.Role == role)
                .ToListAsync();

        public void Update(User user) =>
            _context.Users.Update(user);

        public void Delete(User user) =>
            _context.Users.Remove(user);
        public async Task UpdateResetTokenAsync(int userId, string token, DateTime expiry)
        {
            await _context.Users
                .Where(u => u.Id == userId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(u => u.ResetToken, token)
                    .SetProperty(u => u.ResetTokenExpiry, expiry));
        }

        public async Task<User?> GetUserByResetTokenAsync(string token)
            => await _context.Users
                .FirstOrDefaultAsync(u => u.ResetToken == token);
    }
}
