using Application.DTOs;
using Application.Exceptions;
using Application.Services;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace InsuranceAPI.InterfaceAdapters.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PoliciesController : ControllerBase
    {
        private readonly IPolicyService _policyService;

        public PoliciesController(IPolicyService policyService)
        {
            _policyService = policyService;
        }

        //  Customer 
        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CreatePolicy([FromForm] CreatePolicyDto dto)
        {
            var memberList = JsonSerializer.Deserialize<List<PolicyMemberDto>>(
                dto.Members,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var nomineeList = JsonSerializer.Deserialize<List<PolicyNomineeDto>>(
                dto.Nominees,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (memberList == null || !memberList.Any())
                throw new BadRequestException("At least one policy member is required");

            if (nomineeList == null || !nomineeList.Any())
                throw new BadRequestException("At least one nominee is required");

            if (dto.IdentityProof == null)
                throw new BadRequestException("Customer identity proof is required");

            if (dto.IncomeProof == null)
                throw new BadRequestException("Customer income proof is required");

            var nonPrimaryMembers = memberList.Where(m => !m.IsPrimaryInsured).ToList();
            if (nonPrimaryMembers.Any() &&
                (dto.MemberDocuments == null || !dto.MemberDocuments.Any()))
                throw new BadRequestException(
                    "Identity proof required for all non-primary members");

            var customerDocs = new List<IFormFile>
    {
        dto.IdentityProof,
        dto.IncomeProof
    };

            var customerId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var result = await _policyService.CreatePolicyAsync(
                customerId,
                dto,
                memberList,
                nomineeList,
                customerDocs,
                dto.MemberDocuments ?? new List<IFormFile>());

            return CreatedAtAction(
                nameof(GetPolicyById), new { id = result.Id }, result);
        }
        [HttpGet("my-policies")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetMyPolicies()
        {
            var customerId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var result = await _policyService.GetMyPoliciesAsync(customerId);
            return Ok(result);
        }

        //  Agent 

        [HttpGet("my-assigned-policies")]
        [Authorize(Roles = "Agent")]
        public async Task<IActionResult> GetAgentPolicies()
        {
            var agentId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var result = await _policyService.GetAgentPoliciesAsync(agentId);
            return Ok(result);
        }

        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Agent")]
        public async Task<IActionResult> UpdatePolicyStatus(
            int id, [FromBody] UpdatePolicyStatusDto dto)
        {
            await _policyService.UpdatePolicyStatusAsync(id, dto);
            return Ok(new { message = "Policy status updated successfully" });
        }

        //  Admin 

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllPolicies()
        {
            var result = await _policyService.GetAllPoliciesAsync();
            return Ok(result);
        }

        [HttpPatch("{id}/assign-agent")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignAgent(
            int id, [FromBody] AssignAgentDto dto)
        {
            await _policyService.AssignAgentAsync(id, dto);
            return Ok(new { message = "Agent assigned successfully" });
        }

        [HttpPost("{id}/remind-expiry")]
        [Authorize(Roles = "Admin,Agent")]
        public async Task<IActionResult> SendExpiryReminder(int id)
        {
            await _policyService.SendExpiryReminderAsync(id);
            return Ok(new { message = "Expiry reminder sent successfully" });
        }

        //  Admin + Agent + Customer 

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Agent,Customer,ClaimsOfficer")]
        public async Task<IActionResult> GetPolicyById(int id)
        {
            var result = await _policyService.GetPolicyByIdAsync(id);
            return Ok(result);
        }
        [HttpGet("download-document/{documentId}")]
        [Authorize(Roles = "Admin,Customer,Agent,ClaimsOfficer")]
        public async Task<IActionResult> DownloadDocument(int documentId)
        {
            var userId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var role = User.FindFirst(ClaimTypes.Role)!.Value;

            var (fileBytes, fileName, contentType) =
                await _policyService.DownloadDocumentAsync(documentId, userId, role);

            return File(fileBytes, contentType, fileName);
        }
        [HttpPost("{id}/cancel")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CancelPolicy(int id)
        {
            var customerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            await _policyService.CancelPendingPolicyAsync(id, customerId);
            return Ok(new { message = "Policy cancelled successfully" });
        }

        [HttpGet("{id}/download-application")]
        [Authorize(Roles = "Admin,Customer,Agent")]
        public async Task<IActionResult> GetPolicyApplicationPdf(int id)
        {
            var customerId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var (fileBytes, fileName) =
                await _policyService.GeneratePolicyApplicationPdfAsync(id, customerId);

            return File(fileBytes, "application/pdf", fileName);
        }
        // Save new draft
        [HttpPost("draft")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> SaveDraft([FromBody] SaveDraftDto dto)
        {
            var customerId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _policyService.SaveDraftAsync(customerId, dto);
            return Ok(result);
        }

        // Update existing draft
        [HttpPut("draft/{id}")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> UpdateDraft(
            int id, [FromBody] SaveDraftDto dto)
        {
            var customerId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _policyService
                .UpdateDraftAsync(id, customerId, dto);
            return Ok(result);
        }

        // Submit draft as actual policy
        [HttpPost("draft/{id}/submit")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> SubmitDraft(int id)
        {
            var customerId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            // Reuse same multipart parsing as CreatePolicy
            var membersJson = Request.Form["members"];
            var nomineesJson = Request.Form["nominees"];
            var policyJson = Request.Form["policy"];

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };
            var dto = JsonSerializer.Deserialize<CreatePolicyDto>(policyJson!, options);
            var members = JsonSerializer.Deserialize<List<PolicyMemberDto>>(membersJson!, options);
            var nominees = JsonSerializer.Deserialize<List<PolicyNomineeDto>>(nomineesJson!, options);

            var customerDocs = Request.Form.Files
                .Where(f => f.Name == "CustomerDocuments").ToList();
            var memberDocs = Request.Form.Files
                .Where(f => f.Name == "MemberDocuments").ToList();

            var result = await _policyService.SubmitDraftAsync(
                id, customerId, dto!, members!, nominees!,
                customerDocs, memberDocs);

            return Ok(result);
        }

        // Get all drafts
        [HttpGet("my-drafts")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetMyDrafts()
        {
            var customerId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _policyService.GetMyDraftsAsync(customerId);
            return Ok(result);
        }

        // Delete draft
        [HttpDelete("draft/{id}")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> DeleteDraft(int id)
        {
            var customerId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            await _policyService.DeleteDraftAsync(id, customerId);
            return NoContent();
        }

        [HttpPost("replace-document/{documentId}")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> ReplaceDocument(int documentId, IFormFile file)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _policyService.ReplaceDocumentAsync(documentId, userId, file);
            return Ok(result);
        }

        [HttpGet("{policyId}/reinstatement-quote")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetReinstatementQuote(int policyId)
        {
            try
            {
                var quote = await _policyService.GetReinstatementQuoteAsync(policyId);
                return Ok(quote);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{policyId}/reinstate")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> ReinstatePolicy(int policyId, [FromBody] ReinstateRequestDto dto)
        {
            try
            {
                var policyNumber = await _policyService.ReinstatePolicyAsync(policyId, dto.PaymentReference);
                return Ok(new { message = $"Policy {policyNumber} reinstated successfully", policyNumber });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}