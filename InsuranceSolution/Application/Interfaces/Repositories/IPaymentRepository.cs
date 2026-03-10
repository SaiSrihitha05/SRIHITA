using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;

namespace Application.Interfaces.Repositories
{
    public interface IPaymentRepository
    {
        Task AddAsync(Payment payment);
        Task<Payment?> GetByIdAsync(int id);
        Task<IEnumerable<Payment>> GetByPolicyIdAsync(int policyId);
        Task<IEnumerable<Payment>> GetByCustomerIdAsync(int customerId);
        Task<IEnumerable<Payment>> GetAllAsync();
        Task<string> GenerateInvoiceNumberAsync();
        Task<bool> HasAnyCompletedPaymentAsync(int policyAssignmentId);
        Task SaveChangesAsync();
    }
}