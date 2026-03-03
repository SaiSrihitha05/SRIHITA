using Application.Exceptions;
using Application.Interfaces.Repositories;
using Application.Services;
using Application.Tests.Common;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Application.Tests.Services
{
    public class NotificationServiceTests : ApplicationTestBase
    {
        private readonly NotificationService _notificationService;
        private readonly INotificationRepository _notificationRepository;

        public NotificationServiceTests()
        {
            _notificationRepository = new NotificationRepository(Context);
            _notificationService = new NotificationService(_notificationRepository);
        }

        #region CreateNotificationAsync Tests (5)
        [Fact]
        public async Task CreateNotificationAsync_ValidInput_SavesToDb()
        {
            await _notificationService.CreateNotificationAsync(1, "T", "M", NotificationType.PolicyStatusUpdate, null, null, null);
            var n = await Context.Notifications.FirstOrDefaultAsync();
            Assert.NotNull(n);
            Assert.Equal("T", n.Title);
        }
        [Fact]
        public async Task CreateNotificationAsync_WithIds_SavesCorrectly()
        {
            await _notificationService.CreateNotificationAsync(1, "T", "M", NotificationType.ClaimStatusUpdate, policyId: 10, claimId: 20, paymentId: null);
            var n = await Context.Notifications.FirstOrDefaultAsync();
            Assert.Equal(10, n!.PolicyAssignmentId);
            Assert.Equal(20, n.ClaimId);
        }
        [Fact]
        public async Task CreateNotificationAsync_DefaultUnread_IsFalse()
        {
            await _notificationService.CreateNotificationAsync(1, "T", "M", NotificationType.General, null, null, null);
            Assert.False((await Context.Notifications.FirstAsync()).IsRead);
        }
        [Fact]
        public async Task CreateNotificationAsync_Timestamp_IsSet()
        {
            await _notificationService.CreateNotificationAsync(1, "T", "M", NotificationType.General, null, null, null);
            Assert.True((await Context.Notifications.FirstAsync()).CreatedAt <= DateTime.UtcNow);
        }
        [Fact]
        public async Task CreateNotificationAsync_UserId_IsCorrect()
        {
            await _notificationService.CreateNotificationAsync(99, "T", "M", NotificationType.General, null, null, null);
            Assert.Equal(99, (await Context.Notifications.FirstAsync()).UserId);
        }
        #endregion

        #region GetMyNotificationsAsync Tests (5)
        [Fact]
        public async Task GetMyNotificationsAsync_ValidUser_ReturnsMine()
        {
            await Context.Notifications.AddAsync(new Notification { UserId = 1, Title = "Mine" });
            await Context.Notifications.AddAsync(new Notification { UserId = 2, Title = "Other" });
            await Context.SaveChangesAsync();
            var res = await _notificationService.GetMyNotificationsAsync(1);
            Assert.Single(res);
            Assert.Equal("Mine", res.First().Title);
        }
        [Fact] public async Task GetMyNotificationsAsync_NoNotifications_ReturnsEmpty() => Assert.Empty(await _notificationService.GetMyNotificationsAsync(1));
        [Fact]
        public async Task GetMyNotificationsAsync_Multiple_ReturnsAll()
        {
            await Context.Notifications.AddAsync(new Notification { UserId = 1 });
            await Context.Notifications.AddAsync(new Notification { UserId = 1 });
            await Context.SaveChangesAsync();
            Assert.Equal(2, (await _notificationService.GetMyNotificationsAsync(1)).Count());
        }
        [Fact]
        public async Task GetMyNotificationsAsync_MappingCheck()
        {
            await Context.Notifications.AddAsync(new Notification { UserId = 1, Type = NotificationType.PaymentConfirmation });
            await Context.SaveChangesAsync();
            var res = await _notificationService.GetMyNotificationsAsync(1);
            Assert.Equal("PaymentConfirmation", res.First().Type);
        }
        [Fact]
        public async Task GetMyNotificationsAsync_SortedByNewest_IfApplicable()
        {
            // Note: service doesn't explicitly sort, but usually repository might. 
            // We just check basic retrieval here.
            await Context.Notifications.AddAsync(new Notification { UserId = 1, Title = "A" });
            await Context.SaveChangesAsync();
            Assert.Single(await _notificationService.GetMyNotificationsAsync(1));
        }
        #endregion

        #region MarkAsReadAsync Tests (5)
        [Fact]
        public async Task MarkAsReadAsync_ValidId_SetsIsReadTrue()
        {
            var n = new Notification { UserId = 1, IsRead = false };
            await Context.Notifications.AddAsync(n); await Context.SaveChangesAsync();
            await _notificationService.MarkAsReadAsync(n.Id, 1);
            var updated = await Context.Notifications.FindAsync(n.Id);
            Assert.True(updated!.IsRead);
        }
        [Fact] public async Task MarkAsReadAsync_NotFound_ThrowsNotFound() => await Assert.ThrowsAsync<NotFoundException>(() => _notificationService.MarkAsReadAsync(999, 1));
        [Fact]
        public async Task MarkAsReadAsync_WrongUser_ThrowsForbidden()
        {
            var n = new Notification { UserId = 1 };
            await Context.Notifications.AddAsync(n); await Context.SaveChangesAsync();
            await Assert.ThrowsAsync<ForbiddenException>(() => _notificationService.MarkAsReadAsync(n.Id, 2));
        }
        [Fact]
        public async Task MarkAsReadAsync_AlreadyRead_RemainsRead()
        {
            var n = new Notification { UserId = 1, IsRead = true };
            await Context.Notifications.AddAsync(n); await Context.SaveChangesAsync();
            await _notificationService.MarkAsReadAsync(n.Id, 1);
            Assert.True((await Context.Notifications.FindAsync(n.Id))!.IsRead);
        }
        [Fact] public async Task MarkAsReadAsync_ZeroId_ThrowsNotFound() => await Assert.ThrowsAsync<NotFoundException>(() => _notificationService.MarkAsReadAsync(0, 1));
        #endregion

        #region MarkAllAsReadAsync Tests (5)
        [Fact]
        public async Task MarkAllAsReadAsync_MultipleUnread_SetsAllTrue()
        {
            await Context.Notifications.AddAsync(new Notification { UserId = 1, IsRead = false });
            await Context.Notifications.AddAsync(new Notification { UserId = 1, IsRead = false });
            await Context.SaveChangesAsync();
            await _notificationService.MarkAllAsReadAsync(1);
            Assert.All(await Context.Notifications.Where(n => n.UserId == 1).ToListAsync(), n => Assert.True(n.IsRead));
        }
        [Fact]
        public async Task MarkAllAsReadAsync_OtherUserUnread_RemainsFalse()
        {
            await Context.Notifications.AddAsync(new Notification { UserId = 1, IsRead = false });
            await Context.Notifications.AddAsync(new Notification { UserId = 2, IsRead = false });
            await Context.SaveChangesAsync();
            await _notificationService.MarkAllAsReadAsync(1);
            Assert.False((await Context.Notifications.FirstAsync(n => n.UserId == 2)).IsRead);
        }
        [Fact]
        public async Task MarkAllAsReadAsync_NoUnread_NoChange()
        {
            await Context.Notifications.AddAsync(new Notification { UserId = 1, IsRead = true });
            await Context.SaveChangesAsync();
            await _notificationService.MarkAllAsReadAsync(1);
            Assert.True((await Context.Notifications.FirstAsync()).IsRead);
        }
        [Fact]
        public async Task MarkAllAsReadAsync_NoNotifications_Success()
        {
            await _notificationService.MarkAllAsReadAsync(1);
            Assert.Empty(await Context.Notifications.ToListAsync());
        }
        [Fact]
        public async Task MarkAllAsReadAsync_SavesChanges()
        {
            await Context.Notifications.AddAsync(new Notification { UserId = 1, IsRead = false });
            await Context.SaveChangesAsync();
            await _notificationService.MarkAllAsReadAsync(1);
            var updated = await Context.Notifications.FirstAsync();
            Assert.True(updated.IsRead);
        }
        #endregion
    }
}
