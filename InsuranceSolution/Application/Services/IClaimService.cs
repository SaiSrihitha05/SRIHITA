using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs;

namespace Application.Services
{
    public interface IClaimService
    {
        Task<ClaimResponseDto> FileClaimAsync(int customerId, FileClaimDto dto);                    // Customer

        Task<IEnumerable<ClaimResponseDto>> GetMyClaimsAsync(int customerId);                                      // Customer

        Task<IEnumerable<ClaimResponseDto>> GetAllClaimsAsync(); // Admin

        Task<IEnumerable<ClaimResponseDto>> GetMyAssignedClaimsAsync(int officerId);                                       // ClaimsOfficer

        Task<ClaimResponseDto> GetClaimByIdAsync(int id);

        Task AssignClaimsOfficerAsync(int claimId, AssignClaimsOfficerDto dto);            // Admin

        Task<ClaimResponseDto> ProcessClaimAsync(int claimId, int officerId, ProcessClaimDto dto);    // ClaimsOfficer

        Task ProcessMaturityClaimsAsync();                        // Background
    }
}