using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace InsuranceAPI.InterfaceAdapters.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class KycController : ControllerBase
    {
        private readonly IKycService _kycService;

        public KycController(IKycService kycService)
        {
            _kycService = kycService;
        }

        [HttpPost("customer")]
        public async Task<IActionResult> ProcessCustomerKyc([FromForm] ProcessKycDto dto)
        {
            if (dto.File == null || dto.File.Length == 0)
                return BadRequest("No file uploaded.");

            var result = await _kycService.ProcessCustomerKycAsync(dto);
            return Ok(result);
        }

        [HttpPost("member")]
        public async Task<IActionResult> ProcessMemberKyc([FromForm] ProcessKycDto dto)
        {
            if (dto.File == null || dto.File.Length == 0)
                return BadRequest("No file uploaded.");

            var result = await _kycService.ProcessMemberKycAsync(dto);
            return Ok(result);
        }

        [HttpPost("verify-death-certificate")]
        public async Task<IActionResult> VerifyDeathCertificate([FromForm] DeathCertificateKycDto dto)
        {
            if (dto.File == null || dto.File.Length == 0)
                return BadRequest("No file uploaded.");

            var result = await _kycService.VerifyDeathCertificateAsync(dto.File, dto.CertificateNumber, dto.DateOfDeath, dto.DeceasedName);
            return Ok(result);
        }

        [HttpPost("verify-nominee")]
        public async Task<IActionResult> VerifyNominee([FromForm] NomineeVerificationDto dto)
        {
            if (dto.File == null || dto.File.Length == 0)
                return BadRequest("No file uploaded.");

            var result = await _kycService.VerifyNomineeIdentityAsync(dto.File, dto.ExpectedName);
            return Ok(result);
        }
    }
}
