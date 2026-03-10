using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class ClaimRepository : IClaimRepository
    {
        private readonly InsuranceDbContext _context;

        public ClaimRepository(InsuranceDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(InsuranceClaim claim) =>
            await _context.Claims.AddAsync(claim);

        public async Task<InsuranceClaim?> GetByIdAsync(int id) =>
            await _context.Claims.FindAsync(id);

        public async Task<InsuranceClaim?> GetByIdWithDetailsAsync(int id) =>
            await _context.Claims
                .Include(c => c.PolicyAssignment)
                    .ThenInclude(p => p!.Plan)
                .Include(c => c.PolicyMember)
                .Include(c => c.ClaimsOfficer)
                .Include(c => c.Documents)
                .FirstOrDefaultAsync(c => c.Id == id);

        public async Task<IEnumerable<InsuranceClaim>> GetAllAsync() =>
            await _context.Claims
                .Include(c => c.PolicyAssignment)
                    .ThenInclude(p => p!.Plan)
                .Include(c => c.PolicyMember)
                .Include(c => c.ClaimsOfficer)
                .OrderByDescending(c => c.FiledDate)
                .ToListAsync();

        public async Task<IEnumerable<InsuranceClaim>> GetByPolicyIdAsync(
            int policyId) =>
            await _context.Claims
                .Include(c => c.PolicyMember)
                .Include(c => c.ClaimsOfficer)
                .Include(c => c.Documents)
                .Where(c => c.PolicyAssignmentId == policyId)
                .ToListAsync();

        public async Task<IEnumerable<InsuranceClaim>> GetByClaimsOfficerIdAsync(
            int officerId) =>
            await _context.Claims
                .Include(c => c.PolicyAssignment)
                    .ThenInclude(p => p!.Plan)
                .Include(c => c.PolicyMember)
                .Include(c => c.Documents)
                .Where(c => c.ClaimsOfficerId == officerId)
                .OrderByDescending(c => c.FiledDate)
                .ToListAsync();

        public async Task<IEnumerable<InsuranceClaim>> GetByCustomerIdAsync(
            int customerId) =>
            await _context.Claims
                .Include(c => c.PolicyAssignment)
                    .ThenInclude(p => p!.Plan)
                .Include(c => c.PolicyMember)
                .Include(c => c.Documents)
                .Where(c => c.PolicyAssignment!.CustomerId == customerId)
                .OrderByDescending(c => c.FiledDate)
                .ToListAsync();

        public async Task<bool> HasActiveclaimAsync(int policyMemberId) =>
            await _context.Claims
                .AnyAsync(c => c.PolicyMemberId == policyMemberId &&
                               c.Status != ClaimStatus.Rejected &&
                               c.Status != ClaimStatus.Settled);

        public void Update(InsuranceClaim claim) =>
            _context.Claims.Update(claim);

        public async Task SaveChangesAsync() =>
            await _context.SaveChangesAsync();
    }
}