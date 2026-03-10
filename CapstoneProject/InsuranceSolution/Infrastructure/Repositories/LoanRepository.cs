using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class LoanRepository : ILoanRepository
    {
        private readonly InsuranceDbContext _context;

        public LoanRepository(InsuranceDbContext context)
        {
            _context = context;
        }

        public async Task<PolicyLoan?> GetByIdAsync(int id)
            => await _context.PolicyLoans
                .Include(l => l.PolicyAssignment)
                    .ThenInclude(p => p.Plan)
                .Include(l => l.Payments)
                .FirstOrDefaultAsync(l => l.Id == id);

        public async Task<IEnumerable<PolicyLoan>> GetAllAsync()
            => await _context.PolicyLoans
                .Include(l => l.PolicyAssignment)
                    .ThenInclude(p => p.Plan)
                .Include(l => l.PolicyAssignment)
                    .ThenInclude(p => p.Customer)
                .Include(l => l.Payments)
                .OrderByDescending(l => l.LoanDate)
                .ToListAsync();

        public async Task<IEnumerable<PolicyLoan>> GetByCustomerIdAsync(int customerId)
            => await _context.PolicyLoans
                .Include(l => l.PolicyAssignment)
                    .ThenInclude(p => p.Plan)
                .Include(l => l.Payments)
                .Where(l => l.CustomerId == customerId)
                .OrderByDescending(l => l.LoanDate)
                .ToListAsync();

        public async Task<IEnumerable<PolicyLoan>> GetByPolicyIdAsync(int policyId)
            => await _context.PolicyLoans
                .Include(l => l.Payments)
                .Where(l => l.PolicyAssignmentId == policyId)
                .ToListAsync();

        public async Task<PolicyLoan?> GetActiveLoanByPolicyAsync(int policyId)
            => await _context.PolicyLoans
                .FirstOrDefaultAsync(l =>
                    l.PolicyAssignmentId == policyId &&
                    l.Status == LoanStatus.Active);

        public async Task<PolicyLoan> AddAsync(PolicyLoan loan)
        {
            await _context.PolicyLoans.AddAsync(loan);
            return loan;
        }

        public void Update(PolicyLoan loan)
            => _context.PolicyLoans.Update(loan);

        public async Task SaveChangesAsync()
            => await _context.SaveChangesAsync();
    }
}
