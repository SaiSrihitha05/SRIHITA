using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Exceptions;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;

        public NotificationService(INotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        public async Task CreateNotificationAsync(
            int userId, string title, string message,
            NotificationType type,
            int? policyId, int? claimId, int? paymentId)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                PolicyAssignmentId = policyId,
                ClaimId = claimId,
                PaymentId = paymentId
            };

            await _notificationRepository.AddAsync(notification);
            await _notificationRepository.SaveChangesAsync();
        }

        public async Task<IEnumerable<NotificationResponseDto>> GetMyNotificationsAsync(
            int userId)
        {
            var notifications = await _notificationRepository
                .GetByUserIdAsync(userId);

            return notifications.Select(n => new NotificationResponseDto
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type.ToString(),
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt,
                PolicyId = n.PolicyAssignmentId,
                ClaimId = n.ClaimId,
                PaymentId = n.PaymentId
            });
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            var notifications = await _notificationRepository
                .GetByUserIdAsync(userId);

            return notifications.Count(n => !n.IsRead);
        }

        public async Task MarkAsReadAsync(int notificationId, int userId)
        {
            var notification = await _notificationRepository
                .GetByIdAsync(notificationId);

            if (notification == null)
                throw new NotFoundException("Notification", notificationId);

            if (notification.UserId != userId)
                throw new ForbiddenException(
                    "You can only mark your own notifications as read");

            notification.IsRead = true;
            await _notificationRepository.SaveChangesAsync();
        }

        public async Task MarkAllAsReadAsync(int userId)
        {
            var notifications = await _notificationRepository
                .GetByUserIdAsync(userId);

            foreach (var n in notifications.Where(n => !n.IsRead))
                n.IsRead = true;

            await _notificationRepository.SaveChangesAsync();
        }
    }
}