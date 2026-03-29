using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;

namespace Application.Interfaces.Repositories
{
    public interface IPolicyRepository
    {
        Task<PolicyAssignment?> GetByIdAsync(int id);
        Task<PolicyAssignment?> GetByIdWithDetailsAsync(int id);  // includes members, nominees, docs
        Task<PolicyAssignment?> GetByIdWithPlanAsync(int id);
        Task<IEnumerable<PolicyAssignment>> GetAllAsync();
        Task<IEnumerable<PolicyAssignment>> GetByCustomerIdAsync(int customerId);
        Task<IEnumerable<PolicyAssignment>> GetByAgentIdAsync(int agentId);
        Task<string> GeneratePolicyNumberAsync();
        Task<bool> HasActivePoliciesByCustomerAsync(int customerId);
        Task<bool> HasActivePoliciesByAgentAsync(int agentId);
        Task<IEnumerable<PolicyAssignment>> GetPoliciesDueSoonAsync(int daysAhead);
        Task<IEnumerable<PolicyAssignment>> GetMaturedPoliciesAsync();
        Task<IEnumerable<PolicyAssignment>> GetLapsedCandidatesAsync();
        Task AddAsync(PolicyAssignment policy);
        Task<PolicyAssignment?> GetByPolicyNumberAsync(string policyNumber);
        void Update(PolicyAssignment policy);
        Task UpdateAsync(PolicyAssignment policy);
        Task SaveChangesAsync();
        void Delete(PolicyAssignment policy);
    }
}