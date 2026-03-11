using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs;
using Microsoft.AspNetCore.Http;

namespace Application.Services
{
    public interface IPolicyService
    {
        Task<PolicyResponseDto> CreatePolicyAsync(
            int customerId,
            CreatePolicyDto dto,
            List<PolicyMemberDto> members,
            List<PolicyNomineeDto> nominees,
            List<IFormFile> customerDocuments,
            List<IFormFile> memberDocuments);
        Task<PolicyResponseDto> GetPolicyByIdAsync(int id);
        Task<IEnumerable<PolicyResponseDto>> GetAllPoliciesAsync();           
        Task<IEnumerable<PolicyResponseDto>> GetMyPoliciesAsync(int customerId); 
        Task<IEnumerable<PolicyResponseDto>> GetAgentPoliciesAsync(int agentId); 
        Task UpdatePolicyStatusAsync(int id, UpdatePolicyStatusDto dto);      
        Task AssignAgentAsync(int id, AssignAgentDto dto);                    
        Task<(byte[] fileBytes, string fileName, string contentType)>
    DownloadDocumentAsync(int documentId, int userId, string role);
        Task CancelPendingPolicyAsync(int policyId, int customerId);
        Task<PolicyResponseDto> SaveDraftAsync(
    int customerId, SaveDraftDto dto);

        Task<PolicyResponseDto> UpdateDraftAsync(
            int policyId, int customerId, SaveDraftDto dto);

        Task<PolicyResponseDto> SubmitDraftAsync(
            int policyId,
            int customerId,
            CreatePolicyDto dto,
            List<PolicyMemberDto> members,
            List<PolicyNomineeDto> nominees,
            List<IFormFile> customerDocuments,
            List<IFormFile> memberDocuments);

        decimal GetCurrentCoverage(Domain.Entities.PolicyAssignment policy, Domain.Entities.PolicyMember member);
        BonusCalculationResult GetBonusDetails(Domain.Entities.PolicyAssignment policy, Domain.Entities.PolicyMember member);

        Task<IEnumerable<PolicyResponseDto>> GetMyDraftsAsync(int customerId);

        Task DeleteDraftAsync(int policyId, int customerId);
    }
}