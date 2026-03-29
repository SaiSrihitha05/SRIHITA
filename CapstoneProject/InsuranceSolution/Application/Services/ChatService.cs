using Application.DTOs;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Application.Services
{
    public class ChatService : IChatService
    {
        private readonly IAiService _aiService;
        private readonly IPolicyRepository _policyRepository;
        private readonly IClaimRepository _claimRepository;
        private readonly IUserRepository _userRepository;
        private readonly IPlanRepository _planRepository;
        private readonly INotificationRepository _notificationRepo;
        private readonly IChatMessageRepository _chatRepo;
        private readonly ISystemConfigRepository _systemConfigRepository;
        private readonly IClaimsOfficerAssignmentService _claimsOfficerAssignmentService;
        private readonly IChatNotificationService _chatNotificationService;
        private readonly ILogger<ChatService> _logger;

        public ChatService(
            IAiService aiService,
            IPolicyRepository policyRepository,
            IClaimRepository claimRepository,
            IUserRepository userRepository,
            IPlanRepository planRepository,
            INotificationRepository notificationRepo,
            IChatMessageRepository chatRepo,
            ISystemConfigRepository systemConfigRepository,
            IClaimsOfficerAssignmentService claimsOfficerAssignmentService,
            IChatNotificationService chatNotificationService,
            ILogger<ChatService> logger)
        {
            _aiService = aiService;
            _policyRepository = policyRepository;
            _claimRepository = claimRepository;
            _userRepository = userRepository;
            _planRepository = planRepository;
            _notificationRepo = notificationRepo;
            _chatRepo = chatRepo;
            _systemConfigRepository = systemConfigRepository;
            _claimsOfficerAssignmentService = claimsOfficerAssignmentService;
            _chatNotificationService = chatNotificationService;
            _logger = logger;
        }

        public async Task<ChatResponseDto> ProcessMessageAsync(int? userId, ChatMessageDto chatMessage)
        {
            if (!IsInsuranceRelated(chatMessage.Message))
            {
                return new ChatResponseDto
                {
                    Response = "I specialize in Hartford Life Insurance. I can help with plans, policies, or claims. How can I assist you with insurance today? 😊",
                    Intent = "General",
                    SuggestedQuestions = GetDefaultSuggestedQuestions()
                };
            }

            var chatSession = await _chatRepo.GetOrCreateSessionAsync(chatMessage.SessionId, userId);

            await _chatRepo.AddAsync(new ChatMessage
            {
                CustomerId = userId,
                Message = chatMessage.Message,
                SenderType = ChatSenderType.Customer,
                SessionId = chatMessage.SessionId,
                SessionInternalId = chatSession.Id,
                CreatedAt = DateTime.UtcNow
            });

            await _chatNotificationService.NotifyMessageAsync(
                chatMessage.SessionId, chatMessage.Message, ChatSenderType.Customer);

            if (chatSession.IsChatClosed)
            {
                // 🔥 SELF-HEAL: If the session was hard-closed (likely by old logic), 
                // but the customer is messaging, re-open it to allow AI takeover.
                chatSession.IsChatClosed = false;
                await _chatRepo.UpdateSessionAsync(chatSession);
            }

            var cleanMsg = chatMessage.Message.ToLower().Trim();

            // 🚨 PRIORITY 1: Explicit Request Pre-check 
            bool forceAgentEscalation = IsExplicitAgentRequest(chatMessage.Message);
            bool forceClaimEscalation = IsExplicitClaimRequest(chatMessage.Message);

            // 🚨 PRIORITY 2: Existing Human-Led Sessions (Bypass if explicit request made)
            if (chatSession.IsClaimsOfficerAssigned && chatSession.ClaimsOfficerId.HasValue && !forceClaimEscalation)
            {
                // 🔥 EXTRA SAFETY CHECK: Re-verify state from DB to handle race conditions
                var latestSession = await _chatRepo.GetSessionAsync(chatMessage.SessionId);
                if (latestSession != null && latestSession.IsClaimsOfficerAssigned && latestSession.ClaimsOfficerId.HasValue)
                {
                    var officerWarning = "Claims Officer is in the conversation. Please wait for their reply.";
                    await PersistAiResponseAsync(officerWarning, chatMessage.SessionId, chatSession.Id, userId);
                    return new ChatResponseDto
                    {
                        Response = officerWarning,
                        Intent = "OfficerLed"
                    };
                }
            }

            if (chatSession.IsAgentAssigned && chatSession.AgentId.HasValue && !forceAgentEscalation)
            {
                // 🔥 EXTRA SAFETY CHECK: Re-verify state from DB to handle race conditions
                var latestSession = await _chatRepo.GetSessionAsync(chatMessage.SessionId);

                if (latestSession != null && latestSession.IsAgentAssigned && latestSession.AgentId.HasValue)
                {
                    var agentWarning = "Agent is in the conversation. Please wait for their reply.";
                    await PersistAiResponseAsync(agentWarning, chatMessage.SessionId, chatSession.Id, userId);
                    return new ChatResponseDto
                    {
                        Response = agentWarning,
                        Intent = "AgentLed"
                    };
                }
            }

            // 4. Fetch context and history
            var allPlans = (await _planRepository.GetAllActiveAsync()).ToList();
            string contextInfo = await BuildContextInfoAsync(userId, allPlans);
            var conversationHistory = (await _chatRepo.GetRecentBySessionAsync(chatMessage.SessionId, 8))
                .Where(m => m.SenderType != ChatSenderType.System)
                .Select(h => $"{h.SenderType}: {h.Message}")
                .ToList();

            string intent;
            var responseDto = new ChatResponseDto();

            // 5. Check for explicit escalation FIRST to prevent AI override
            if (forceAgentEscalation)
            {
                intent = "AgentNeed";
            }
            else if (forceClaimEscalation)
            {
                intent = "ClaimRelated";
            }
            else
            {
                // 6. Regular / Multi-lingual / AI Processing
                if (cleanMsg.Contains("nenu") || cleanMsg.Contains("cheyali") || cleanMsg.Contains("vundi"))
                {
                    intent = "General";
                    responseDto.Response = "మీరు హార్ట్‌ఫోర్డ్ లైఫ్ ఇన్సూరెన్స్ గురించి అడిగినందుకు ధన్యవాదాలు. మీరు ప్లాన్ కొనాలంటే లేదా మీ పాలసీ వివరాలు చూడాలంటే, నేను మీకు సహాయం చేయగలను 😊";
                }
                else if (cleanMsg == "hi" || cleanMsg == "hello")
                {
                    intent = "General";
                    responseDto.Response = "Hi 👋 How can I help you with insurance today?";
                }
                else
                {
                    // Full AI Processing
                    var aiResult = await ProcessWithAiAsync(chatMessage.Message, contextInfo, conversationHistory, allPlans);
                    intent = aiResult.Intent ?? "General";
                    responseDto.Response = aiResult.Response;
                    responseDto.Action = aiResult.Action;
                    responseDto.Intent = intent;

                    // Re-verify if AI detected agent need but logic didn't catch it
                    if (intent == "AgentNeed") forceAgentEscalation = true;
                }

                // If handled and no escalation needed, persistence layer handles it
                if (intent != "AgentNeed" && intent != "ClaimRelated" && !string.IsNullOrEmpty(responseDto.Response))
                {
                    // 🚨 GUEST RESTRICTION: Block policy/buying for unauthenticated users
                    if (!userId.HasValue && (intent == "PolicyRelated" || responseDto.Action?.Type == "BuyPlan"))
                    {
                        var guestMsg = "Please login to view your policy, track status, or purchase a plan. You can still ask me about our available insurance plans! 😊";
                        await PersistAiResponseAsync(guestMsg, chatMessage.SessionId, chatSession.Id, userId);
                        return new ChatResponseDto
                        {
                            Response = guestMsg,
                            Intent = "AuthRequired",
                            SuggestedQuestions = new List<SuggestedQuestionDto> { new() { Label = "Available Plans", Message = "Show me plans" } }
                        };
                    }

                    await PersistAiResponseAsync(responseDto.Response, chatMessage.SessionId, chatSession.Id, userId);
                    responseDto.SuggestedQuestions = GetDefaultSuggestedQuestions();
                    return responseDto;
                }
            }

            // 7. Escalation Handlers
            if (intent == "ClaimRelated")
            {
                if (!userId.HasValue)
                {
                    responseDto.Response = "Please login to access claim support.";
                    await PersistAiResponseAsync(responseDto.Response, chatMessage.SessionId, chatSession.Id, userId);
                    return responseDto;
                }

                var extractedClaimId = ExtractClaimId(chatMessage.Message);
                InsuranceClaim? claim = null;
                if (extractedClaimId.HasValue)
                {
                    claim = await _claimRepository.GetByIdAsync(extractedClaimId.Value);
                    if (claim != null && claim.PolicyAssignment?.CustomerId != userId) claim = null;
                }

                if (claim == null)
                {
                    var claims = await _claimRepository.GetByCustomerIdAsync(userId.Value);
                    claim = claims.OrderByDescending(c => c.CreatedAt).FirstOrDefault();
                }

                if (claim != null)
                {
                    User? officer = null;
                    if (claim.ClaimsOfficerId.HasValue)
                    {
                        officer = await _userRepository.GetByIdAsync(claim.ClaimsOfficerId.Value);
                    }

                    if (officer == null)
                    {
                        officer = await _claimsOfficerAssignmentService.AssignOfficerAsync();
                    }

                    if (officer != null)
                    {
                        claim.ClaimsOfficerId = officer.Id;
                        _claimRepository.Update(claim); await _claimRepository.SaveChangesAsync();
                        chatSession.ClaimsOfficerId = officer.Id;
                        chatSession.IsClaimsOfficerAssigned = true;
                        chatSession.RelatedClaimId = claim.Id;
                        chatSession.IsActive = true; // 🔥 CRITICAL: Show it on the officer's dashboard again
                        await _chatRepo.UpdateSessionAsync(chatSession);
                        var sysMsg = $"🔔 Claims Officer {officer.Name} is now connected for your claim #{claim.Id}.";
                        await AddSystemMessageToSessionAsync(chatSession.Id, chatMessage.SessionId, userId, sysMsg);
                        return new ChatResponseDto { Response = sysMsg, Intent = "ClaimEscalated", EscalationTarget = new EscalationTargetDto { Id = officer.Id, Name = officer.Name, Role = "Claims Officer" } };
                    }
                }
                responseDto.Response = "No active claims found for your account.";
                await PersistAiResponseAsync(responseDto.Response, chatMessage.SessionId, chatSession.Id, userId);
                return responseDto;
            }

            if (intent == "AgentNeed")
            {
                // ✅ 1. Login required
                if (!userId.HasValue)
                {
                    var authMsg = "Please login to connect with an agent.";
                    await PersistAiResponseAsync(authMsg, chatMessage.SessionId, chatSession.Id, userId);
                    return new ChatResponseDto
                    {
                        Response = authMsg,
                        Intent = "AgentNeed"
                    };
                }

                var policyNum = ExtractPolicyNumber(chatMessage.Message);

                // ❌ Policy is mandatory now (based on your requirement)
                if (string.IsNullOrEmpty(policyNum))
                {
                    var policyReqMsg = "Please provide your policy number to connect with the assigned agent.";
                    await PersistAiResponseAsync(policyReqMsg, chatMessage.SessionId, chatSession.Id, userId);
                    return new ChatResponseDto
                    {
                        Response = policyReqMsg,
                        Intent = "AgentNeed"
                    };
                }

                // ✅ Normalize (since DB already cleaned)
                var normalizedPolicy = policyNum.Replace("-", "").ToUpper().Trim();

                // ✅ Direct DB lookup
                var policy = await _policyRepository.GetByPolicyNumberAsync(normalizedPolicy);

                // ❌ Policy not found → STOP
                if (policy == null)
                {
                    return new ChatResponseDto
                    {
                        Response = $"❌ Policy {policyNum} not found. Please check and try again.",
                        Intent = "AgentNeed"
                    };
                }

                // ❌ No agent assigned → STOP
                if (!policy.AgentId.HasValue)
                {
                    return new ChatResponseDto
                    {
                        Response = $"⚠️ No agent is assigned to policy {policy.PolicyNumber} yet.",
                        Intent = "AgentNeed"
                    };
                }

                // ✅ Get assigned agent ONLY
                var agent = await _userRepository.GetByIdAsync(policy.AgentId.Value);

                if (agent == null)
                {
                    return new ChatResponseDto
                    {
                        Response = "Agent information not found. Please try again later.",
                        Intent = "AgentNeed"
                    };
                }

                // ✅ Update session
                chatSession.AgentId = agent.Id;
                chatSession.IsAgentAssigned = true;
                chatSession.RelatedPolicyId = policy.Id;
                chatSession.LinkedPolicyId = policy.Id;
                chatSession.IsActive = true; // 🔥 CRITICAL: Show it on the agent's dashboard again
                await _chatRepo.UpdateSessionAsync(chatSession);

                // ✅ Response
                var msg = $"🔔 Agent {agent.Name} is now connected. They are reviewing your policy {policy.PolicyNumber}.";

                await AddSystemMessageToSessionAsync(chatSession.Id, chatMessage.SessionId, userId, msg);

                return new ChatResponseDto
                {
                    Response = msg,
                    Intent = "AgentEscalated",
                    EscalationTarget = new EscalationTargetDto
                    {
                        Id = agent.Id,
                        Name = agent.Name,
                        Role = "Agent"
                    }
                };
            }

            responseDto.Response = "I'm here to help. Could you tell me more about what you're looking for?";
            await PersistAiResponseAsync(responseDto.Response, chatMessage.SessionId, chatSession.Id, userId);
            return responseDto;
        }

        private async Task<AiProcessResult> ProcessWithAiAsync(
            string message, string contextInfo, List<string> history, List<Plan> plans)
        {
            var planList = string.Join(", ", plans.Select(p => $"{p.Id}={p.PlanName}"));
            string historyBlock = history.Any()
                ? "\nRECENT CONVERSATION:\n" + string.Join("\n", history) + "\n"
                : "";

            string systemPrompt = $@"You are Hartford LifeConnect AI. ONLY handle Life Insurance.
AVAILABLE PLANS:
{contextInfo}

{historyBlock}

CORE RULES:
1. FOLLOW-UP: Reference RECENT CONVERSATION for 'yes', 'ok', 'above', 'this plan'.
2. BUY FLOW: If user wants to buy -> show 'Buy Plan' button ONLY. NEVER ask personal info.
3. EXPLAIN: Key benefit + Who it's for. Provide EXACT plan details from context. < 4 lines.
4. POLICY STATUS GUIDANCE:
   - LAPSED: Explain it happened due to missed payments. Action: Tell them they can reinstate by paying missed premium + penalty.
   - MATURED: Explain the term is complete. Action: Tell them they are now eligible for maturity benefits as per the plan.
   - ACTIVE: Regular premium payments keep it active.
5. WHY DETAILS: Premium calculation + policy accuracy.
6. NO REPETITION: Only mention 'Explore plans' if user specifically asked for ALL plans.
7. STYLE: Simple English or Telugu if requested. Helpful tone. < 6 lines.

IMPORTANT: Return ONLY valid JSON: {{""intent"":""<intent>"",""response"":""<reply>"",""selectedPlan"":""<ID or NONE>""}}";

            try
            {
                var aiTask = _aiService.GetAiResponseAsync(systemPrompt, message, history);
                var completedTask = await Task.WhenAny(aiTask, Task.Delay(5000));

                if (completedTask != aiTask)
                {
                    _logger.LogWarning("AI timeout for message: {Msg}", message);
                    return new AiProcessResult { Intent = "General", Response = "I'm taking longer than expected. Please try again." };
                }

                string raw = await aiTask;
                return ParseAiResult(raw, plans);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI Call failed");
                return new AiProcessResult { Intent = "General", Response = "I'm having trouble connecting right now." };
            }
        }

        private AiProcessResult ParseAiResult(string raw, List<Plan> plans)
        {
            try
            {
                string cleaned = Regex.Replace(raw.Trim(), @"^```json|^```|```$", "", RegexOptions.Multiline).Trim();
                var match = Regex.Match(cleaned, @"\{.*\}", RegexOptions.Singleline);
                if (!match.Success) return new AiProcessResult { Intent = InferIntentFromText(raw), Response = raw };

                string safeJson = Regex.Replace(match.Value, @"(?<!\\)\n", "\\n").Replace("\r", "");
                try
                {
                    using var doc = JsonDocument.Parse(safeJson);
                    var root = doc.RootElement;
                    string intent = NormalizeIntent(root.TryGetProperty("intent", out var ip) ? ip.GetString() ?? "" : "");
                    string response = root.TryGetProperty("response", out var rp) ? rp.GetString() ?? "" : "";
                    string selectedPlanRaw = root.TryGetProperty("selectedPlan", out var sp) ? sp.GetString() ?? "NONE" : "NONE";

                    ChatActionDto? action = null;
                    if (int.TryParse(selectedPlanRaw.Trim(), out int id))
                    {
                        var matched = plans.FirstOrDefault(p => p.Id == id);
                        if (matched != null)
                        {
                            action = new ChatActionDto { Type = "BuyPlan", PlanId = matched.Id, PlanName = matched.PlanName, Url = $"http://localhost:4200/customer-dashboard/buy-policy?planId={matched.Id}" };
                        }
                    }

                    if (action == null && response.Contains("explore", StringComparison.OrdinalIgnoreCase))
                    {
                        action = new ChatActionDto { Type = "ExplorePlans", Url = "http://localhost:4200/customer-dashboard/explore-plans" };
                    }

                    return new AiProcessResult { Intent = intent, Response = response, Action = action };
                }
                catch (JsonException)
                {
                    var respMatch = Regex.Match(safeJson, @"""response""\s*:\s*""([^""]*)""");
                    if (respMatch.Success) return new AiProcessResult { Intent = "General", Response = respMatch.Groups[1].Value };
                    return new AiProcessResult { Intent = InferIntentFromText(raw), Response = raw };
                }
            }
            catch 
            { 
                return new AiProcessResult { Intent = "General", Response = "I encountered an issue processing that. Could you try asking in another way?" }; 
            }
        }

        private string InferIntentFromText(string text)
        {
            text = text.ToLower();
            if (text.Contains("plan") || text.Contains("buy")) return "PlanSuggestion";
            if (text.Contains("claim")) return "ClaimRelated";
            if (text.Contains("agent") || text.Contains("talk")) return "AgentNeed";
            return "General";
        }

        private string NormalizeIntent(string raw)
        {
            return raw.ToLower().Trim() switch
            {
                "claimrelated" or "claim_related" => "ClaimRelated",
                "agentneed" or "agent_need" => "AgentNeed",
                "plansuggestion" or "plan_suggestion" => "PlanSuggestion",
                "policyrelated" or "policy_related" => "PolicyRelated",
                _ => "General"
            };
        }

        private async Task<string> BuildContextInfoAsync(int? userId, List<Plan> plans)
        {
            string info = "PLANS:\n" + string.Join("\n", plans.Select(p => $"- {p.PlanName} (ID: {p.Id}) | Type: {p.PlanType} | Benefits: {(p.HasMaturityBenefit ? "Maturity, " : "")}{(p.HasBonus ? "Bonus, " : "")}"));
            if (userId.HasValue)
            {
                var policies = await _policyRepository.GetByCustomerIdAsync(userId.Value);
                if (policies.Any())
                {
                    info += "\nUSER POLICIES:\n" + string.Join("\n", policies.Select(p => $"- {p.PolicyNumber} | Plan: {p.Plan?.PlanName ?? "Unknown"} | Status: {p.Status} | Premium Paid: ₹{p.TotalPremiumAmount}"));
                }

                var claims = await _claimRepository.GetByCustomerIdAsync(userId.Value);
                if (claims.Any())
                {
                    info += "\nUSER CLAIMS:\n" + string.Join("\n", claims.Select(c => $"- Claim #{c.Id} | Policy: {c.PolicyAssignment?.PolicyNumber} | Status: {c.Status} | Type: {c.ClaimType}"));
                }
            }
            return info;
        }

        private string? ExtractPolicyNumber(string message)
        {
            var match = Regex.Match(message, @"\bPOL[-]?\d+\b", RegexOptions.IgnoreCase);
            return match.Success ? match.Value : null;
        }

        private string NormalizePolicyNumber(string value)
        {
            return value.Replace("-", "").ToUpper().Trim();
        }

        private int? ExtractClaimId(string message)
        {
            var match = Regex.Match(message, @"#?(\d+)", RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out int id)) return id;
            return null;
        }

        private bool IsInsuranceRelated(string message)
        {
            if (message.Length < 3) return true;
            // Use regex or stem-based match for plural support (policies, claims)
            var patterns = new[] { "insur", "polic", "claim", "plan", "life", "benefit", "hartford", "hi", "hello", "nenu", "cheyali", "stat" };
            return patterns.Any(p => message.Contains(p, StringComparison.OrdinalIgnoreCase));
        }

        private async Task AddSystemMessageToSessionAsync(int sessionInternalId, string sessionId, int? userId, string message)
        {
            await _chatRepo.AddAsync(new ChatMessage { CustomerId = userId, Message = message, SenderType = ChatSenderType.System, SessionId = sessionId, SessionInternalId = sessionInternalId, CreatedAt = DateTime.UtcNow });
            await _chatNotificationService.NotifyMessageAsync(sessionId, message, ChatSenderType.System);
        }

        private async Task<User?> GetAssignedAgentAsync(int? customerId, PolicyAssignment? targetPolicy = null)
        {
            // Use agent from the specific policy mentioned
            if (targetPolicy?.AgentId.HasValue == true)
            {
                var agent = await _userRepository.GetByIdAsync(targetPolicy.AgentId.Value);
                if (agent != null) return agent;
            }

            // Check all customer policies for any assigned agent
            if (customerId.HasValue)
            {
                var policies = await _policyRepository.GetByCustomerIdAsync(customerId.Value);

                var policyWithAgent = policies
                    .Where(p => p.AgentId.HasValue)
                    .OrderBy(p => p.Status == PolicyStatus.Pending ? 0 :
                                 p.Status == PolicyStatus.Active ? 1 : 2)
                    .ThenByDescending(p => p.CreatedAt)
                    .FirstOrDefault();

                if (policyWithAgent?.AgentId != null)
                {
                    var agent = await _userRepository.GetByIdAsync(policyWithAgent.AgentId.Value);
                    if (agent != null) return agent;
                }
            }

            // Last resort — round-robin using SystemConfig
            var agents = await _userRepository.GetByRoleAsync(UserRole.Agent);
            if (!agents.Any()) return null;

            var config = await _systemConfigRepository.GetConfigAsync();
            int index = (config.LastAgentAssignmentIndex + 1) % agents.Count();
            config.LastAgentAssignmentIndex = index;
            _systemConfigRepository.Update(config);
            await _systemConfigRepository.SaveChangesAsync();

            return agents.ElementAt(index);
        }

        private bool IsExplicitAgentRequest(string message)
        {
            var patterns = new[]
            {
                @"\bconnect\b.*\bagent\b",
                @"\btalk\b.*\bagent\b",
                @"\bspeak\b.*\bagent\b",
                @"\bagent\b.*\blinked\b",
                @"\bagent\b.*\bpolicy\b",
                @"\bhuman\b.*\bagent\b",
                @"\brepresentative\b",
                @"\bperson\b",
                @"\bagent\b.*\bfor\b.*\bpol\b",
                @"\bneed\s*agent\b"
            };
            return patterns.Any(p => Regex.IsMatch(message, p, RegexOptions.IgnoreCase));
        }

        private bool IsExplicitClaimRequest(string message)
        {
            var patterns = new[]
            {
                @"connect\s*(me\s*)?(with|to)\s*(a\s*)?claims?\s*officer",
                @"talk\s*(to|with)\s*(a\s*)?claims?\s*officer",
                @"speak\s*(to|with)\s*(a\s*)?claims?\s*officer",
                @"officer\s*assigned",
                @"connect\s*(me\s*)?(with|to)\s*(an?\s*)?officer"
            };
            return patterns.Any(p => Regex.IsMatch(message, p, RegexOptions.IgnoreCase));
        }

        private async Task PersistAiResponseAsync(string response, string sessionId, int internalId, int? userId)
        {
            await _chatRepo.AddAsync(new ChatMessage { CustomerId = userId, Message = response, SenderType = ChatSenderType.AI, SessionId = sessionId, SessionInternalId = internalId, CreatedAt = DateTime.UtcNow });
            await _chatNotificationService.NotifyMessageAsync(sessionId, response, ChatSenderType.AI);
        }

        public async Task<List<ChatSessionSummaryDto>> GetAgentSessionsAsync(int agentId)
        {
            var sessions = await _chatRepo.GetActiveSessionsForAgentAsync(agentId);
            return sessions.Select(s => new ChatSessionSummaryDto
            {
                Id = s.Id,
                SessionId = s.SessionId,
                CustomerId = s.CustomerId,
                Name = "User-" + (s.CustomerId ?? 0),
                IsActive = s.IsActive,
                RelatedPolicyId = s.RelatedPolicyId,
                RelatedClaimId = s.RelatedClaimId
            }).ToList();
        }

        public async Task<List<ChatSessionSummaryDto>> GetOfficerSessionsAsync(int officerId)
        {
            var sessions = await _chatRepo.GetActiveSessionsForOfficerAsync(officerId);
            return sessions.Select(s => new ChatSessionSummaryDto
            {
                Id = s.Id,
                SessionId = s.SessionId,
                CustomerId = s.CustomerId,
                Name = "User-" + (s.CustomerId ?? 0),
                IsActive = s.IsActive,
                RelatedPolicyId = s.RelatedPolicyId,
                RelatedClaimId = s.RelatedClaimId
            }).ToList();
        }

        public async Task<List<ChatMessage>> GetChatHistoryAsync(int agentId, int customerId)
        {
            var sessions = await _chatRepo.GetActiveSessionsForAgentAsync(agentId);
            var session = sessions.FirstOrDefault(s => s.CustomerId == customerId);
            return session != null ? await _chatRepo.GetBySessionIdAsync(session.SessionId) : new List<ChatMessage>();
        }

        public async Task<List<ChatMessage>> GetSessionHistoryAsync(string sessionId) => await _chatRepo.GetBySessionIdAsync(sessionId);

        public async Task AgentReplyAsync(int agentId, int? customerId, string sessionId, string message)
        {
            var session = await _chatRepo.GetSessionAsync(sessionId);
            if (session == null) return;
            await _chatRepo.AddAsync(new ChatMessage { AgentId = agentId, CustomerId = customerId, Message = message, SenderType = ChatSenderType.Agent, SessionId = sessionId, SessionInternalId = session.Id, CreatedAt = DateTime.UtcNow });
            await _chatNotificationService.NotifyMessageAsync(sessionId, message, ChatSenderType.Agent);
        }

        public async Task OfficerReplyAsync(int officerId, int? customerId, string sessionId, string message)
        {
            var session = await _chatRepo.GetSessionAsync(sessionId);
            if (session == null) return;
            await _chatRepo.AddAsync(new ChatMessage { CustomerId = customerId, Message = message, SenderType = ChatSenderType.Officer, SessionId = sessionId, SessionInternalId = session.Id, CreatedAt = DateTime.UtcNow });
            await _chatNotificationService.NotifyMessageAsync(sessionId, message, ChatSenderType.Officer);
        }

        public async Task SendPlanLinkAsync(int agentId, string sessionId, int planId)
        {
            var plan = await _planRepository.GetByIdAsync(planId);
            var session = await _chatRepo.GetSessionAsync(sessionId);
            if (plan != null && session != null)
            {
                var message = $"I recommend the {plan.PlanName}";
                var planUrl = $"http://localhost:4200/customer-dashboard/buy-policy?planId={plan.Id}";

                await _chatRepo.AddAsync(new ChatMessage
                {
                    AgentId = agentId,
                    CustomerId = session.CustomerId,
                    Message = message,
                    SenderType = ChatSenderType.Agent,
                    MessageType = ChatMessageType.PlanLink,
                    LinkedPlanId = plan.Id,
                    LinkedPlanName = plan.PlanName,
                    LinkedPlanUrl = planUrl,
                    SessionId = sessionId,
                    SessionInternalId = session.Id,
                    CreatedAt = DateTime.UtcNow
                });

                // ✅ Real-time notify with full metadata
                await _chatNotificationService.NotifyPlanLinkAsync(sessionId, new
                {
                    sessionId = sessionId,
                    message = message,
                    messageType = "PlanLink",
                    planId = plan.Id,
                    planName = plan.PlanName,
                    planUrl = planUrl
                });
            }
        }

        public async Task CloseSessionAsync(string sessionId)
        {
            var session = await _chatRepo.GetSessionAsync(sessionId);
            if (session == null) return;

            session.IsActive = false;
            bool wasAgent = session.IsAgentAssigned;
            bool wasOfficer = session.IsClaimsOfficerAssigned;

            session.IsAgentAssigned = false;
            session.AgentId = null;
            session.IsClaimsOfficerAssigned = false;
            session.ClaimsOfficerId = null;
            session.ClosedAt = DateTime.UtcNow;
            session.IsChatClosed = false; // 🔥 IMPORTANT: Allow customer to continue with AI

            await _chatRepo.UpdateSessionAsync(session);

            // 🔥 Send System Message to notify customer
            string msg = wasAgent
                ? "🔕 Agent has left the chat. You can continue with AI or connect again if needed."
                : "🔕 Claims officer has left the chat. You can continue with AI or connect again if needed.";

            await _chatRepo.AddAsync(new ChatMessage
            {
                CustomerId = session.CustomerId,
                Message = msg,
                SenderType = ChatSenderType.System,
                SessionId = sessionId,
                SessionInternalId = session.Id,
                CreatedAt = DateTime.UtcNow
            });

            // ✅ Real-time notify via SignalR
            await _chatNotificationService.NotifyMessageAsync(sessionId, msg, ChatSenderType.System);
            await _chatNotificationService.NotifyChatClosedAsync(sessionId, false);
        }

        public async Task<List<ChatMessage>> GetCustomerHistoryAsync(int customerId)
        {
            var sessions = await _chatRepo.GetByCustomerIdAsync(customerId);
            var last = sessions.OrderByDescending(s => s.CreatedAt).FirstOrDefault();
            return last != null ? await _chatRepo.GetBySessionIdAsync(last.SessionId) : new List<ChatMessage>();
        }

        public async Task<int?> GetLastAgentIdAsync(int customerId)
        {
            var sessions = await _chatRepo.GetByCustomerIdAsync(customerId);
            var lastSession = sessions.OrderByDescending(s => s.CreatedAt).FirstOrDefault();
            if (lastSession == null) return null;
            var msgs = await _chatRepo.GetBySessionIdAsync(lastSession.SessionId);
            return msgs.LastOrDefault(m => m.AgentId.HasValue)?.AgentId;
        }

        public async Task LinkSessionToUserAsync(string sessionId, int userId)
        {
            var session = await _chatRepo.GetSessionAsync(sessionId);
            if (session != null && !session.CustomerId.HasValue) { session.CustomerId = userId; await _chatRepo.UpdateSessionAsync(session); }
        }

        public async Task<object?> GetClaimContextAsync(int claimId)
        {
            var claim = await _claimRepository.GetByIdAsync(claimId);
            return claim != null ? new { claim.Id, Status = claim.Status.ToString() } : null;
        }

        public async Task UpdateClaimStatusAsync(int officerId, int claimId, string sessionId, string status)
        {
            var claim = await _claimRepository.GetByIdAsync(claimId);
            if (claim != null && Enum.TryParse<ClaimStatus>(status, out var s)) { claim.Status = s; claim.ClaimsOfficerId = officerId; _claimRepository.Update(claim); await _claimRepository.SaveChangesAsync(); }
        }

        public async Task<PolicyContextDto?> GetPolicyContextAsync(string policyNumber)
        {
            var policy = await _policyRepository.GetByPolicyNumberAsync(policyNumber);
            if (policy == null) return null;

            return new PolicyContextDto
            {
                Id = policy.Id,
                PolicyNumber = policy.PolicyNumber,
                PlanName = policy.Plan?.PlanName ?? "N/A",
                Status = policy.Status.ToString(),
                CoverageAmount = policy.TotalPremiumAmount,
                CustomerName = policy.Customer?.Name ?? "N/A",
                CreatedAt = policy.CreatedAt
            };
        }

        public async Task<PolicyContextDto?> GetPolicyContextByIdAsync(int policyId)
        {
            var policy = await _policyRepository.GetByIdWithPlanAsync(policyId);
            if (policy == null) return null;

            return new PolicyContextDto
            {
                Id = policy.Id,
                PolicyNumber = policy.PolicyNumber,
                PlanName = policy.Plan?.PlanName ?? "N/A",
                Status = policy.Status.ToString(),
                CoverageAmount = policy.TotalPremiumAmount,
                CustomerName = policy.Customer?.Name ?? "N/A",
                CreatedAt = policy.CreatedAt
            };
        }

        public async Task<IEnumerable<object>> GetCustomerPoliciesAsync(int customerId)
        {
            var policies = await _policyRepository.GetByCustomerIdAsync(customerId);
            return policies.Select(p => new { p.Id, p.PolicyNumber, PlanName = p.Plan?.PlanName });
        }

        public async Task<string> GetUserNameAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            return user?.Name ?? "User";
        }

        public async Task<ChatSession?> GetChatSessionAsync(string sessionId) => await _chatRepo.GetSessionAsync(sessionId);

        public async Task<ChatResponseDto> GetWelcomeAsync(int? userId, string sessionId)
        {
            var session = await _chatRepo.GetOrCreateSessionAsync(sessionId, userId);
            var history = await _chatRepo.GetBySessionIdAsync(sessionId);
            if (history.Any(m => m.SenderType == ChatSenderType.AI)) return new ChatResponseDto { Response = history.First(m => m.SenderType == ChatSenderType.AI).Message, Intent = "General", SuggestedQuestions = GetDefaultSuggestedQuestions() };

            var userName = userId.HasValue ? (await _userRepository.GetByIdAsync(userId.Value))?.Name : "there";
            var welcome = $"Hi {userName ?? "there"}! 👋 How can I help you today?";
            await _chatRepo.AddAsync(new ChatMessage { CustomerId = userId, Message = welcome, SenderType = ChatSenderType.AI, SessionId = sessionId, SessionInternalId = session.Id, CreatedAt = DateTime.UtcNow });
            return new ChatResponseDto { Response = welcome, Intent = "General", SuggestedQuestions = GetDefaultSuggestedQuestions() };
        }

        private List<SuggestedQuestionDto> GetDefaultSuggestedQuestions() => new List<SuggestedQuestionDto> { new() { Label = "Available Plans", Message = "Show me plans" }, new() { Label = "Check Policy", Message = "Show my policy" }, new() { Label = "Track Claim", Message = "Track my claim" }, new() { Label = "Talk to agent", Message = "Connect me to agent" } };
    }

    internal class AiProcessResult { public string Intent { get; set; } = "General"; public string Response { get; set; } = string.Empty; public ChatActionDto? Action { get; set; } }
}