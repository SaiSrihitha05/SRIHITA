using Application.DTOs;
using Application.Services;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InsuranceAPI.InterfaceAdapters.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ClaimsController : ControllerBase
    {
        private readonly IClaimService _claimService;

        public ClaimsController(IClaimService claimService)
        {
            _claimService = claimService;
        }

        // ── Customer ──────────────────────────────────────────

        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> FileClaim(
            [FromForm] FileClaimDto dto)
        {
            var customerId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var result = await _claimService.FileClaimAsync(customerId, dto);
            return CreatedAtAction(
                nameof(GetClaimById), new { id = result.Id }, result);
        }

        [HttpGet("my-claims")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetMyClaims()
        {
            var customerId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var result = await _claimService.GetMyClaimsAsync(customerId);
            return Ok(result);
        }

        // ── ClaimsOfficer ─────────────────────────────────────

        [HttpGet("my-assigned-claims")]
        [Authorize(Roles = "ClaimsOfficer")]
        public async Task<IActionResult> GetMyAssignedClaims()
        {
            var officerId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var result = await _claimService
                .GetMyAssignedClaimsAsync(officerId);
            return Ok(result);
        }

        [HttpPatch("{id}/process")]
        [Authorize(Roles = "ClaimsOfficer")]
        public async Task<IActionResult> ProcessClaim(
            int id, [FromBody] ProcessClaimDto dto)
        {
            var officerId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var result = await _claimService
                .ProcessClaimAsync(id, officerId, dto);
            return Ok(result);
        }

        // ── Admin ─────────────────────────────────────────────

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllClaims()
        {
            var result = await _claimService.GetAllClaimsAsync();
            return Ok(result);
        }

        [HttpPatch("{id}/assign-officer")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignClaimsOfficer(
            int id, [FromBody] AssignClaimsOfficerDto dto)
        {
            await _claimService.AssignClaimsOfficerAsync(id, dto);
            return Ok(new { message = "Claims officer assigned successfully" });
        }

        // ── Shared ────────────────────────────────────────────

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Customer,ClaimsOfficer")]
        public async Task<IActionResult> GetClaimById(int id)
        {
            var result = await _claimService.GetClaimByIdAsync(id);
            return Ok(result);
        }
    }
}