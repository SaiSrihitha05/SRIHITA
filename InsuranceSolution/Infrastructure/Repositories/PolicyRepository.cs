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
    public class PolicyRepository : IPolicyRepository
    {
        private readonly InsuranceDbContext _context;

        public PolicyRepository(InsuranceDbContext context)
        {
            _context = context;
        }

        public async Task<PolicyAssignment?> GetByIdAsync(int id) =>
            await _context.PolicyAssignments.FindAsync(id);

        public async Task<PolicyAssignment?> GetByIdWithDetailsAsync(int id) =>
            await _context.PolicyAssignments
                .Include(p => p.Customer)
                .Include(p => p.Agent)
                .Include(p => p.Plan)
                .Include(p => p.PolicyMembers)
                .Include(p => p.PolicyNominees)
                .Include(p => p.Documents)
                .FirstOrDefaultAsync(p => p.Id == id);

        public async Task<IEnumerable<PolicyAssignment>> GetAllAsync() =>
            await _context.PolicyAssignments
                .Include(p => p.Customer)
                .Include(p => p.Agent)
                .Include(p => p.PolicyMembers)
                .Include(p => p.PolicyNominees)
                .Include(p => p.Documents)
                .Include(p => p.Plan)
                .ToListAsync();

        public async Task<IEnumerable<PolicyAssignment>> GetByCustomerIdAsync(
            int customerId) =>
            await _context.PolicyAssignments
                .Include(p => p.Plan)
                .Include(p => p.Agent)
                .Include(p => p.PolicyMembers)
                .Include(p => p.PolicyNominees)
                .Include(p => p.Documents)
                .Where(p => p.CustomerId == customerId)
                .ToListAsync();

        public async Task<IEnumerable<PolicyAssignment>> GetByAgentIdAsync(
            int agentId) =>
            await _context.PolicyAssignments
                .Include(p => p.Customer)
                .Include(p => p.Plan)
                .Include(p => p.PolicyMembers)
                .Include(p => p.PolicyNominees)
                .Include(p => p.Documents)
                .Where(p => p.AgentId == agentId)
                .ToListAsync();

        public async Task<string> GeneratePolicyNumberAsync()
        {
            var count = await _context.PolicyAssignments.CountAsync();
            return $"POL{DateTime.UtcNow.Year}{(count + 1):D5}";
        }

        public async Task AddAsync(PolicyAssignment policy) =>
            await _context.PolicyAssignments.AddAsync(policy);

        public void Update(PolicyAssignment policy) =>
            _context.PolicyAssignments.Update(policy);

        public async Task SaveChangesAsync() =>
            await _context.SaveChangesAsync();
        public async Task<bool> HasActivePoliciesByCustomerAsync(int customerId) =>
            await _context.PolicyAssignments
        .AnyAsync(p => p.CustomerId == customerId &&
                       p.Status == PolicyStatus.Active);

        public async Task<bool> HasActivePoliciesByAgentAsync(int agentId) =>
            await _context.PolicyAssignments
                .AnyAsync(p => p.AgentId == agentId &&
                               p.Status == PolicyStatus.Active);
        public async Task<IEnumerable<PolicyAssignment>> GetPoliciesDueSoonAsync(int daysAhead)
        {
            var targetDate = DateTime.UtcNow.Date.AddDays(daysAhead);
            return await _context.PolicyAssignments
                .Include(p => p.Plan)
                .Where(p => p.Status == PolicyStatus.Active &&
                            p.NextDueDate.Date <= targetDate &&
                            p.NextDueDate.Date >= DateTime.UtcNow.Date)
                .ToListAsync();
        }
        public async Task<IEnumerable<PolicyAssignment>> GetMaturedPoliciesAsync() =>
            await _context.PolicyAssignments
                .Include(p => p.Plan)
                .Include(p => p.Customer)
                .Include(p => p.PolicyMembers)
                .Where(p => p.Status == PolicyStatus.Active &&
                            p.EndDate.Date <= DateTime.UtcNow.Date)
                .ToListAsync();
        public void Delete(PolicyAssignment policy)
    => _context.PolicyAssignments.Remove(policy);
    }
}