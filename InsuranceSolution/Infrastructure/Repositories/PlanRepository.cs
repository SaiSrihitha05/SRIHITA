using Application.DTOs;
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
    public class PlanRepository : IPlanRepository
    {
        private readonly InsuranceDbContext _context;

        public PlanRepository(InsuranceDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Plan>> GetAllAsync() =>
            await _context.Plans.ToListAsync();

        public async Task<IEnumerable<Plan>> GetAllActiveAsync() =>
            await _context.Plans
                .Where(p => p.IsActive)
                .ToListAsync();

        public async Task<Plan?> GetByIdAsync(int id) =>
            await _context.Plans.FindAsync(id);

        public async Task AddAsync(Plan plan) =>
            await _context.Plans.AddAsync(plan);

        public void Update(Plan plan) =>
            _context.Plans.Update(plan);

        public void Delete(Plan plan) =>
            _context.Plans.Remove(plan);

        public async Task SaveChangesAsync() =>
            await _context.SaveChangesAsync();
        public async Task<bool> ExistsByNameAsync(string planName) =>
    await _context.Plans
        .AnyAsync(p => p.PlanName.ToLower() == planName.ToLower());
        public async Task<IEnumerable<Plan>> GetFilteredAsync(PlanFilterDto filter)
        {
            var query = _context.Plans.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.PlanType))
                query = query.Where(p =>
                    p.PlanType.ToLower() == filter.PlanType.ToLower());

            if (filter.Age.HasValue)
                query = query.Where(p =>
                    p.MinAge <= filter.Age.Value &&
                    p.MaxAge >= filter.Age.Value);

            if (filter.CoverageAmount.HasValue)
                query = query.Where(p =>
                    p.MinCoverageAmount <= filter.CoverageAmount.Value &&
                    p.MaxCoverageAmount >= filter.CoverageAmount.Value);

            if (filter.TermYears.HasValue)
                query = query.Where(p =>
                    p.MinTermYears <= filter.TermYears.Value &&
                    p.MaxTermYears >= filter.TermYears.Value);

            if (filter.HasMaturityBenefit.HasValue)
                query = query.Where(p =>
                    p.HasMaturityBenefit == filter.HasMaturityBenefit.Value);

            if (filter.IsReturnOfPremium.HasValue)
                query = query.Where(p =>
                    p.IsReturnOfPremium == filter.IsReturnOfPremium.Value);

            return await query.ToListAsync();
        }
    }
}