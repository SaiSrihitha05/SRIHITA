using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class SystemConfigRepository : ISystemConfigRepository
    {
        private readonly InsuranceDbContext _context;

        public SystemConfigRepository(InsuranceDbContext context)
        {
            _context = context;
        }

        public async Task<SystemConfig> GetConfigAsync()
        {
            return await _context.SystemConfigs.FirstOrDefaultAsync() 
                ?? new SystemConfig { Id = 1 };
        }

        public void Update(SystemConfig config)
        {
            config.UpdatedAt = DateTime.UtcNow;
            _context.SystemConfigs.Update(config);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
