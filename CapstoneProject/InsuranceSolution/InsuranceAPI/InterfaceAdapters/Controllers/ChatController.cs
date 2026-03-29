using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace InsuranceAPI.InterfaceAdapters.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Chat([FromBody] ChatMessageDto chatMessage)
        {
            int? userId = null;
            bool isAuth = User.Identity?.IsAuthenticated == true;
            
            if (isAuth)
            {
                var idClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (idClaim != null && int.TryParse(idClaim.Value, out int id))
                {
                    userId = id;
                }
                
                Console.WriteLine($"[DEBUG] Auth: True, UserId: {userId}");
            }
            else
            {
                Console.WriteLine("[DEBUG] Auth: False (Guest Chat)");
            }

            var result = await _chatService.ProcessMessageAsync(userId, chatMessage);
            return Ok(result);
        }

        [HttpPost("link-session")]
        [Authorize]
        public async Task<IActionResult> LinkSession([FromBody] string sessionId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            await _chatService.LinkSessionToUserAsync(sessionId, userId);
            return Ok(new { message = "Session linked successfully" });
        }

        [HttpGet("history")]
        [Authorize]
        public async Task<IActionResult> GetCustomerFullHistory()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var history = await _chatService.GetCustomerHistoryAsync(userId);
            return Ok(history);
        }

        [HttpGet("history/{sessionId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetSessionHistoryForCustomer(string sessionId)
        {
            var history = await _chatService.GetSessionHistoryAsync(sessionId);

            if (User.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    // Check if the session history contains messages belonging to OTHER customers
                    if (history.Any(m => m.CustomerId.HasValue && m.CustomerId != userId))
                    {
                        return Forbid();
                    }
                }
            }
            else
            {
                // For guest users, only allow if the session is truly a guest session (no CustomerId assigned in ANY message)
                if (history.Any(m => m.CustomerId.HasValue))
                {
                    return Forbid();
                }
            }

            return Ok(history);
        }

        [HttpGet("agent/sessions")]
        [Authorize(Roles = "Agent,Admin")]
        public async Task<IActionResult> GetSessions()
        {
            var agentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var sessions = await _chatService.GetAgentSessionsAsync(agentId);
            return Ok(sessions);
        }

        [HttpGet("agent/history/{customerId}")]
        [Authorize(Roles = "Agent,Admin")]
        public async Task<IActionResult> GetHistory(int customerId)
        {
            var agentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var history = await _chatService.GetChatHistoryAsync(agentId, customerId);
            return Ok(history);
        }

        [HttpGet("agent/session-history/{sessionId}")]
        [Authorize(Roles = "Agent,Admin")]
        public async Task<IActionResult> GetSessionHistory(string sessionId)
        {
            var history = await _chatService.GetSessionHistoryAsync(sessionId);
            return Ok(history);
        }

        [HttpPost("agent/reply")]
        [Authorize(Roles = "Agent,Admin")]
        public async Task<IActionResult> Reply([FromBody] StaffReplyDto reply)
        {
            var agentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            await _chatService.AgentReplyAsync(agentId, reply.CustomerId, reply.SessionId, reply.Message);
            return Ok();
        }

        [HttpPost("agent/send-plan-link")]
        [Authorize(Roles = "Agent,Admin")]
        public async Task<IActionResult> SendPlanLink([FromBody] PlanLinkDto dto)
        {
            var agentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            await _chatService.SendPlanLinkAsync(agentId, dto.SessionId, dto.PlanId);
            return Ok();
        }

        [HttpGet("agent/policy-context/{policyNumber}")]
        [Authorize(Roles = "Agent,Admin")]
        public async Task<IActionResult> GetPolicyContext(string policyNumber)
        {
            var policy = await _chatService.GetPolicyContextAsync(policyNumber);
            if (policy == null) return NotFound();
            return Ok(policy);
        }

        [HttpGet("agent/policy-context-by-id/{policyId}")]
        [Authorize(Roles = "Agent,Admin")]
        public async Task<IActionResult> GetPolicyContextById(int policyId)
        {
            var policy = await _chatService.GetPolicyContextByIdAsync(policyId);
            if (policy == null) return NotFound();
            return Ok(policy);
        }

        [HttpGet("agent/customer-policies/{customerId}")]
        [Authorize(Roles = "Agent,Admin")]
        public async Task<IActionResult> GetCustomerPolicies(int customerId)
        {
            var policies = await _chatService.GetCustomerPoliciesAsync(customerId);
            return Ok(policies);
        }

        [HttpGet("officer/sessions")]
        [Authorize(Roles = "ClaimsOfficer,Admin")]
        public async Task<IActionResult> GetOfficerSessions()
        {
            var officerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var sessions = await _chatService.GetOfficerSessionsAsync(officerId);
            return Ok(sessions);
        }

        [HttpGet("officer/session-history/{sessionId}")]
        [Authorize(Roles = "ClaimsOfficer,Admin")]
        public async Task<IActionResult> GetOfficerSessionHistory(string sessionId)
        {
            var history = await _chatService.GetSessionHistoryAsync(sessionId);
            return Ok(history);
        }

        [HttpPost("officer/reply")]
        [Authorize(Roles = "ClaimsOfficer,Admin")]
        public async Task<IActionResult> OfficerReply([FromBody] StaffReplyDto reply)
        {
            var officerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            await _chatService.OfficerReplyAsync(officerId, reply.CustomerId, reply.SessionId, reply.Message);
            return Ok();
        }

        [HttpGet("officer/claim-context/{claimId}")]
        [Authorize(Roles = "ClaimsOfficer,Admin")]
        public async Task<IActionResult> GetClaimContext(int claimId)
        {
            var claim = await _chatService.GetClaimContextAsync(claimId);
            if (claim == null) return NotFound();
            return Ok(claim);
        }

        [HttpPost("officer/update-claim-status")]
        [Authorize(Roles = "ClaimsOfficer,Admin")]
        public async Task<IActionResult> UpdateClaimStatus([FromBody] UpdateClaimStatusDto dto)
        {
            var officerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            await _chatService.UpdateClaimStatusAsync(officerId, dto.ClaimId, dto.SessionId, dto.Status);
            return Ok();
        }

        [HttpGet("sessions/{sessionId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetChatSession(string sessionId)
        {
            var session = await _chatService.GetChatSessionAsync(sessionId);
            if (session == null) return NotFound();
            return Ok(session);
        }

        [HttpGet("welcome")]
        [AllowAnonymous]
        public async Task<IActionResult> GetWelcomeMessage([FromQuery] string sessionId)
        {
            int? userId = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                var idClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (idClaim != null && int.TryParse(idClaim.Value, out int id))
                {
                    userId = id;
                }
            }

            var result = await _chatService.GetWelcomeAsync(userId, sessionId);
            return Ok(result);
        }

        [HttpPost("close/{sessionId}")]
        [Authorize(Roles = "Agent,ClaimsOfficer,Admin")]
        public async Task<IActionResult> CloseSession(string sessionId)
        {
            await _chatService.CloseSessionAsync(sessionId);
            return Ok(new { message = "Chat session closed successfully" });
        }
    }

    public class PlanLinkDto
    {
        public string SessionId { get; set; } = string.Empty;
        public int PlanId { get; set; }
    }
}
