using Application.DTOs;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IPdfService
    {
        Task<byte[]> GeneratePolicyPdfAsync(PolicyResponseDto policy);
        Task<byte[]> GenerateClaimSettlementPdfAsync(ClaimResponseDto claim);
    }
}
