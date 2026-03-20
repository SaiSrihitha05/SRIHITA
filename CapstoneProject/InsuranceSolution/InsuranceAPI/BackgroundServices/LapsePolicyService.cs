using Application.DTOs;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace InsuranceAPI.BackgroundServices
{
    public class LapsePolicyService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<LapsePolicyService> _logger;

        // Run once every 24 hours
        private readonly TimeSpan _interval = TimeSpan.FromHours(24);

        public LapsePolicyService(
            IServiceScopeFactory scopeFactory,
            ILogger<LapsePolicyService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("LapsePolicyService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessLapsedPoliciesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in LapsePolicyService.");
                }

                // Wait 24 hours before running again
                await Task.Delay(_interval, stoppingToken);
            }
        }

        private async Task ProcessLapsedPoliciesAsync()
        {
            using var scope = _scopeFactory.CreateScope();

            var policyRepo = scope.ServiceProvider
                .GetRequiredService<IPolicyRepository>();
            var notificationService = scope.ServiceProvider
                .GetRequiredService<INotificationService>();
            var emailService = scope.ServiceProvider
                .GetRequiredService<IEmailService>();
            var templateService = scope.ServiceProvider
                .GetRequiredService<IEmailTemplateService>();

            // 1. Find all policies that should lapse
            var candidates = await policyRepo.GetLapsedCandidatesAsync();

            _logger.LogInformation(
                "Found {Count} policies to lapse.", candidates.Count());

            foreach (var policy in candidates)
            {
                // 2. Update status
                policy.Status = PolicyStatus.Lapsed;
                policyRepo.Update(policy);

                _logger.LogInformation(
                    "Policy {Number} lapsed.", policy.PolicyNumber);

                // 3. Send in-app notification to customer
                await notificationService.CreateNotificationAsync(
                    userId: policy.CustomerId,
                    title: "Policy Lapsed",
                    message: $"Your policy {policy.PolicyNumber} has lapsed " +
                               $"due to non-payment. Please contact us to reinstate.",
                    type: NotificationType.PolicyStatusUpdate,
                    policyId: policy.Id,
                    claimId: null,
                    paymentId: null);

                // 4. Send email to customer
                if (policy.Customer != null)
                {
                    var body = templateService.GetPolicyLapsedTemplate(policy.Customer.Name, policy.PolicyNumber);
                    await emailService.SendEmailAsync(new EmailRequest
                    {
                        ToEmail = policy.Customer.Email,
                        ToName = policy.Customer.Name,
                        Subject = $"Policy Lapsed - {policy.PolicyNumber}",
                        HtmlContent = body
                    });
                }
            }

            // 5. Save all changes at once
            await policyRepo.SaveChangesAsync();

            _logger.LogInformation("LapsePolicyService completed processing.");
        }
    }
}
