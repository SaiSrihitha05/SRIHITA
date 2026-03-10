using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly InsuranceDbContext _context;

        public NotificationRepository(InsuranceDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Notification notification) =>
            await _context.Notifications.AddAsync(notification);

        public async Task<IEnumerable<Notification>> GetByUserIdAsync(int userId) =>
            await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

        public async Task<Notification?> GetByIdAsync(int id) =>
            await _context.Notifications.FindAsync(id);

        public async Task SaveChangesAsync() =>
            await _context.SaveChangesAsync();
    }
}