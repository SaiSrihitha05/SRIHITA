using Application.DTOs;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Application.Interfaces
{
    public interface IKycService
    {
        Task<KycResponseDto> ProcessCustomerKycAsync(ProcessKycDto dto);
        Task<KycResponseDto> ProcessMemberKycAsync(ProcessKycDto dto);
        Task<KycResponseDto> VerifyDeathCertificateAsync(IFormFile file, string certificateNumber, string? dateOfDeath = null, string? deceasedName = null, string? placeOfDeath = null);
        Task<KycResponseDto> VerifyNomineeIdentityAsync(IFormFile file, string expectedName);
    }
}
