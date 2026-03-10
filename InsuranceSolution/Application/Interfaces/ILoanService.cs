using Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface ILoanService
    {
        Task<LoanResponseDto> ApplyForLoanAsync(int customerId, ApplyLoanDto dto);
        Task<LoanResponseDto> RepayLoanAsync(int customerId, RepayLoanDto dto);
        Task<IEnumerable<LoanResponseDto>> GetMyLoansAsync(int customerId);
        Task<LoanResponseDto?> GetLoanByIdAsync(int id);
        Task<decimal> GetOutstandingLoanAsync(int policyId);

        // Admin methods
        Task<IEnumerable<LoanResponseDto>> GetAllLoansAsync();
        Task<IEnumerable<LoanResponseDto>> GetLoansByPolicyAsync(int policyId);
    }
}
