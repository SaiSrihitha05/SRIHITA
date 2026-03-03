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
        Task<IEnumerable<PolicyResponseDto>> GetAllPoliciesAsync();           // Admin
        Task<IEnumerable<PolicyResponseDto>> GetMyPoliciesAsync(int customerId); // Customer
        Task<IEnumerable<PolicyResponseDto>> GetAgentPoliciesAsync(int agentId); // Agent
        Task UpdatePolicyStatusAsync(int id, UpdatePolicyStatusDto dto);      // Agent
        Task AssignAgentAsync(int id, AssignAgentDto dto);                    // Admin
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

        Task<IEnumerable<PolicyResponseDto>> GetMyDraftsAsync(int customerId);

        Task DeleteDraftAsync(int policyId, int customerId);
    }
}