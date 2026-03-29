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
        Task<ClaimResponseDto> FileClaimAsync(int customerId, FileClaimDto dto);                    
        Task<ClaimResponseDto> ResubmitClaimAsync(int claimId, int customerId, ResubmitClaimDto dto);

        Task<IEnumerable<ClaimResponseDto>> GetMyClaimsAsync(int customerId);                                      

        Task<IEnumerable<ClaimResponseDto>> GetAllClaimsAsync(); 

        Task<IEnumerable<ClaimResponseDto>> GetMyAssignedClaimsAsync(int officerId);                                       
        Task<ClaimResponseDto> GetClaimByIdAsync(int id);

        Task<(byte[] fileBytes, string fileName, string contentType)> DownloadClaimDocumentAsync(int claimId, int documentId, int userId, string role);

        Task AssignClaimsOfficerAsync(int claimId, AssignClaimsOfficerDto dto);         

        Task<ClaimResponseDto> ProcessClaimAsync(int claimId, int officerId, ProcessClaimDto dto);    

        Task ProcessMaturityClaimsAsync();                      
    }
}