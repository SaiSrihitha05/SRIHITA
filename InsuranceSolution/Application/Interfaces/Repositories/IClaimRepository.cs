using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;

namespace Application.Interfaces.Repositories
{
    public interface IClaimRepository
    {
        Task AddAsync(InsuranceClaim claim);
        Task<InsuranceClaim?> GetByIdAsync(int id);
        Task<InsuranceClaim?> GetByIdWithDetailsAsync(int id);
        Task<IEnumerable<InsuranceClaim>> GetAllAsync();
        Task<IEnumerable<InsuranceClaim>> GetByPolicyIdAsync(int policyId);
        Task<IEnumerable<InsuranceClaim>> GetByClaimsOfficerIdAsync(int officerId);
        Task<IEnumerable<InsuranceClaim>> GetByCustomerIdAsync(int customerId);
        Task<bool> HasActiveclaimAsync(int policyMemberId);
        void Update(InsuranceClaim claim);
        Task SaveChangesAsync();
    }
}