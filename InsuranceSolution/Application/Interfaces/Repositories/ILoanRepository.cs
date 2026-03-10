using Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces.Repositories
{
    public interface ILoanRepository
    {
        Task<PolicyLoan?> GetByIdAsync(int id);
        Task<IEnumerable<PolicyLoan>> GetAllAsync();
        Task<IEnumerable<PolicyLoan>> GetByCustomerIdAsync(int customerId);
        Task<IEnumerable<PolicyLoan>> GetByPolicyIdAsync(int policyId);
        Task<PolicyLoan?> GetActiveLoanByPolicyAsync(int policyId);
        Task<PolicyLoan> AddAsync(PolicyLoan loan);
        void Update(PolicyLoan loan);
        Task SaveChangesAsync();
    }
}
