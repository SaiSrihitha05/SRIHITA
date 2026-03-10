using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Services;
using Domain.Enums;

namespace InsuranceAPI.BackgroundServices
{
    public class PremiumReminderService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PremiumReminderService> _logger;

        public PremiumReminderService(
            IServiceScopeFactory scopeFactory,
            ILogger<PremiumReminderService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SendRemindersAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending premium reminders");
                }

                // Run once every 24 hours
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }

        private async Task SendRemindersAsync()
        {
            using var scope = _scopeFactory.CreateScope();

            var policyRepo = scope.ServiceProvider
                .GetRequiredService<IPolicyRepository>();
            var userRepo = scope.ServiceProvider
                .GetRequiredService<IUserRepository>();
            var emailService = scope.ServiceProvider
                .GetRequiredService<IEmailService>();
            var notificationService = scope.ServiceProvider
                .GetRequiredService<INotificationService>();

            // Get all active policies due in next 7 days
            var duePolicies = await policyRepo.GetPoliciesDueSoonAsync(7);

            foreach (var policy in duePolicies)
            {
                var customer = await userRepo.GetByIdAsync(policy.CustomerId);
                if (customer == null) continue;

                _logger.LogInformation(
                    "Sending reminder for policy {PolicyNumber}", policy.PolicyNumber);

                // In-app notification
                await notificationService.CreateNotificationAsync(
                    userId: policy.CustomerId,
                    title: "Premium Due Soon",
                    message: $"Your policy {policy.PolicyNumber} premium is due on {policy.NextDueDate:dd MMM yyyy}.",
                    type: NotificationType.PremiumReminder,
                    claimId: null,
                    policyId: policy.Id,     // ← pass policy id here
                    paymentId: null);

                // Email reminder
                await emailService.SendPremiumReminderAsync(
                    customer.Email,
                    customer.Name,
                    policy.PolicyNumber,
                    policy.NextDueDate,
                    policy.TotalPremiumAmount);
            }
        }
    }
}