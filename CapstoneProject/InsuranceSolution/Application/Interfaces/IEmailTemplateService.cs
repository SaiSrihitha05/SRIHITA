using System;

namespace Application.Interfaces
{
    public interface IEmailTemplateService
    {
        string GetPolicyApprovedTemplate(string customerName, string policyNumber);
        string GetPolicyRejectedTemplate(string customerName, string reason);
        string GetClaimApprovedTemplate(string customerName, string claimId);
        string GetClaimRejectedTemplate(string customerName, string claimId, string reason);
        string GetClaimSettledTemplate(string customerName, string claimId, decimal amount);
        string GetPremiumReminderTemplate(string customerName, string policyNumber, decimal premium, DateTime dueDate);
        string GetPolicyLapsedTemplate(string customerName, string policyNumber);
        string GetForgotPasswordTemplate(string customerName, string resetLink);
        string GetPaymentConfirmationTemplate(string customerName, string policyNumber, string invoiceNumber, decimal amount);
        string GetGenericNotificationTemplate(string title, string message);
    }
}
