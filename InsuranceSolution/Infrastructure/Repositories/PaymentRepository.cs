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
    public class PaymentRepository : IPaymentRepository
    {
        private readonly InsuranceDbContext _context;

        public PaymentRepository(InsuranceDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Payment payment) =>
            await _context.Payments.AddAsync(payment);

        public async Task<Payment?> GetByIdAsync(int id) =>
            await _context.Payments
                .Include(p => p.PolicyAssignment)
                .FirstOrDefaultAsync(p => p.Id == id);

        public async Task<IEnumerable<Payment>> GetByPolicyIdAsync(int policyId) =>
            await _context.Payments
                .Include(p => p.PolicyAssignment)
                .Where(p => p.PolicyAssignmentId == policyId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

        public async Task<IEnumerable<Payment>> GetByCustomerIdAsync(int customerId) =>
            await _context.Payments
                .Include(p => p.PolicyAssignment)
                .Where(p => p.PolicyAssignment!.CustomerId == customerId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

        public async Task<IEnumerable<Payment>> GetAllAsync() =>
            await _context.Payments
                .Include(p => p.PolicyAssignment)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

        public async Task<string> GenerateInvoiceNumberAsync()
        {
            var count = await _context.Payments.CountAsync();
            return $"INV{DateTime.UtcNow.Year}{(count + 1):D6}";
        }
        public async Task<bool> HasAnyCompletedPaymentAsync(int policyAssignmentId) =>
            await _context.Payments
        .AnyAsync(p => p.PolicyAssignmentId == policyAssignmentId &&
                       p.Status == PaymentStatus.Completed);
        public async Task SaveChangesAsync() =>
            await _context.SaveChangesAsync();
    }
}