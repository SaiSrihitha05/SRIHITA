using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace InsuranceApi.InterfaceAdapters.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LoansController : ControllerBase
    {
        private readonly ILoanService _loanService;

        public LoansController(ILoanService loanService)
        {
            _loanService = loanService;
        }

        [HttpPost("apply")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> ApplyForLoan([FromBody] ApplyLoanDto dto)
        {
            var customerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _loanService.ApplyForLoanAsync(customerId, dto);
            return Ok(result);
        }

        [HttpPost("repay")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> RepayLoan([FromBody] RepayLoanDto dto)
        {
            var customerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _loanService.RepayLoanAsync(customerId, dto);
            return Ok(result);
        }

        [HttpGet("my-loans")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetMyLoans()
        {
            var customerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _loanService.GetMyLoansAsync(customerId);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetLoanById(int id)
        {
            var result = await _loanService.GetLoanByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpGet("outstanding/{policyId}")]
        public async Task<IActionResult> GetOutstandingLoan(int policyId)
        {
            var balance = await _loanService.GetOutstandingLoanAsync(policyId);
            return Ok(new { outstandingBalance = balance });
        }

        // Admin Endpoints
        [HttpGet("all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllLoans()
        {
            var result = await _loanService.GetAllLoansAsync();
            return Ok(result);
        }

        [HttpGet("by-policy/{policyId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetLoansByPolicy(int policyId)
        {
            var result = await _loanService.GetLoansByPolicyAsync(policyId);
            return Ok(result);
        }
    }
}
