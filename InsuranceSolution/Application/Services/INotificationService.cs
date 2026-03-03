using Application.DTOs;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface INotificationService
    {
        Task CreateNotificationAsync(int userId, string title,
            string message, NotificationType type,
            int? policyId, int? claimId, int? paymentId);

        Task<IEnumerable<NotificationResponseDto>> GetMyNotificationsAsync(int userId);
        Task MarkAsReadAsync(int notificationId, int userId);
        Task MarkAllAsReadAsync(int userId);
    }
}
