using Application.DTOs;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Application.Tests.Services
{
    public class ChatServiceTests
    {
        private readonly Mock<IAiService> _aiMock;
        private readonly Mock<IPolicyRepository> _policyMock;
        private readonly Mock<IClaimRepository> _claimMock;
        private readonly Mock<IUserRepository> _userMock;
        private readonly Mock<IPlanRepository> _planMock;
        private readonly Mock<INotificationRepository> _notificationMock;
        private readonly Mock<IChatMessageRepository> _chatRepoMock;
        private readonly Mock<ISystemConfigRepository> _systemConfigMock;
        private readonly Mock<IClaimsOfficerAssignmentService> _officerAssignmentMock;
        private readonly Mock<IChatNotificationService> _chatNotificationServiceMock;
        private readonly Mock<ILogger<ChatService>> _loggerMock;
        private readonly ChatService _chatService;

        public ChatServiceTests()
        {
            _aiMock = new Mock<IAiService>();
            _policyMock = new Mock<IPolicyRepository>();
            _claimMock = new Mock<IClaimRepository>();
            _userMock = new Mock<IUserRepository>();
            _planMock = new Mock<IPlanRepository>();
            _notificationMock = new Mock<INotificationRepository>();
            _chatRepoMock = new Mock<IChatMessageRepository>();
            _systemConfigMock = new Mock<ISystemConfigRepository>();
            _officerAssignmentMock = new Mock<IClaimsOfficerAssignmentService>();
            _chatNotificationServiceMock = new Mock<IChatNotificationService>();
            _loggerMock = new Mock<ILogger<ChatService>>();

            _chatService = new ChatService(
                _aiMock.Object, 
                _policyMock.Object,
                _claimMock.Object,
                _userMock.Object, 
                _planMock.Object,
                _notificationMock.Object,
                _chatRepoMock.Object,
                _systemConfigMock.Object,
                _officerAssignmentMock.Object,
                _chatNotificationServiceMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task ProcessMessageAsync_GeneralIntent_Guest_ReturnsPlansFromDb()
        {
            // Arrange
            var message = new ChatMessageDto { Message = "What policies do you have?", SessionId = "test-session" };
            _chatRepoMock.Setup(r => r.GetOrCreateSessionAsync(message.SessionId, null))
                        .ReturnsAsync(new ChatSession { Id = 1, SessionId = message.SessionId });

            _aiMock.Setup(a => a.GetAiResponseAsync(It.IsAny<string>(), message.Message, It.IsAny<List<string>>()))
                   .ReturnsAsync("{\"intent\":\"PlanSuggestion\",\"response\":\"We have Elite Shield: Premium protection.\"}");

            // Act
            var result = await _chatService.ProcessMessageAsync(null, message);

            // Assert
            Assert.Contains("Elite Shield", result.Response);
            _planMock.Verify(p => p.GetAllActiveAsync(), Times.Once);
        }

        [Fact]
        public async Task ProcessMessageAsync_Guest_ConnectToAgent_NotifiesFirstAgent()
        {
            // Arrange
            var message = new ChatMessageDto { Message = "Connect me with an agent", SessionId = "test-session" };
            _chatRepoMock.Setup(r => r.GetOrCreateSessionAsync(message.SessionId, null))
                        .ReturnsAsync(new ChatSession { Id = 1, SessionId = message.SessionId });

            var agent = new User { Id = 2, Name = "Ravi Kumar", Role = UserRole.Agent };
            _userMock.Setup(u => u.GetByRoleAsync(UserRole.Agent))
                     .ReturnsAsync(new List<User> { agent });

            _aiMock.Setup(a => a.GetAiResponseAsync(It.IsAny<string>(), message.Message, It.IsAny<List<string>>()))
                   .ReturnsAsync("{\"intent\":\"AgentNeed\",\"response\":\"I have notified Ravi Kumar.\"}");

            // Act
            var result = await _chatService.ProcessMessageAsync(null, message);

            // Assert
            Assert.Equal("Ravi Kumar", result.EscalationTarget.Name);
            _notificationMock.Verify(n => n.AddAsync(It.Is<Notification>(notif => 
                notif.UserId == agent.Id && notif.Type == NotificationType.AgentChatRequest)), Times.Once);
        }

        [Fact]
        public async Task ProcessMessageAsync_PolicyRelated_Authenticated_NotifiesAssignedAgent()
        {
            // Arrange
            int userId = 1;
            int agentId = 2;
            var message = new ChatMessageDto { Message = "Connect me to my agent", SessionId = "test-session" };
            _chatRepoMock.Setup(r => r.GetOrCreateSessionAsync(message.SessionId, userId))
                        .ReturnsAsync(new ChatSession { Id = 1, SessionId = message.SessionId, CustomerId = userId });
            
            _policyMock.Setup(p => p.GetByCustomerIdAsync(userId))
                       .ReturnsAsync(new List<PolicyAssignment> { 
                           new PolicyAssignment { Id = 10, AgentId = agentId, PolicyNumber = "POL123", CreatedAt = DateTime.Now } 
                       });

            _userMock.Setup(u => u.GetByIdAsync(agentId))
                     .ReturnsAsync(new User { Id = agentId, Name = "Agent Smith" });
            
            _userMock.Setup(u => u.GetByRoleAsync(UserRole.Agent))
                     .ReturnsAsync(new List<User> { new User { Id = agentId, Name = "Agent Smith" } });

            _aiMock.Setup(a => a.GetAiResponseAsync(It.IsAny<string>(), message.Message, It.IsAny<List<string>>()))
                   .ReturnsAsync("{\"intent\":\"AgentNeed\",\"response\":\"Agent Smith has been notified.\"}");

            // Act
            var result = await _chatService.ProcessMessageAsync(userId, message);

            // Assert
            Assert.NotNull(result.EscalationTarget);
            Assert.Equal("Agent Smith", result.EscalationTarget.Name);
            _notificationMock.Verify(n => n.AddAsync(It.Is<Notification>(notif => notif.UserId == agentId)), Times.Once);
        }



    }
}
