using Application.Interfaces;
using System;

namespace Infrastructure.Services
{
    public class EmailTemplateService : Application.Interfaces.IEmailTemplateService
    {
        private const string PrimaryColor = "#75013f";
        private const string SystemName = "Hartford Insurance";

        private string GetBaseTemplate(string title, string content)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }}
        .wrapper {{ background-color: #f4f4f4; padding: 20px; }}
        .container {{ max-width: 600px; margin: 0 auto; background: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1); }}
        .header {{ background-color: {PrimaryColor}; color: #ffffff; padding: 30px; text-align: center; }}
        .content {{ padding: 30px; }}
        .footer {{ background-color: #f9f9f9; color: #777; padding: 20px; text-align: center; font-size: 12px; }}
        .btn {{ display: inline-block; padding: 12px 24px; background-color: {PrimaryColor}; color: #ffffff; text-decoration: none; border-radius: 4px; font-weight: bold; margin-top: 20px; }}
        .details-box {{ background-color: #f8f9fa; border-left: 4px solid {PrimaryColor}; padding: 15px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='wrapper'>
        <div class='container'>
            <div class='header'>
                <h1>{SystemName}</h1>
                <h3>{title}</h3>
            </div>
            <div class='content'>
                {content}
            </div>
            <div class='footer'>
                <p>&copy; {DateTime.Now.Year} {SystemName}. All rights reserved.</p>
                <p>This is an automated message, please do not reply.</p>
            </div>
        </div>
    </div>
</body>
</html>";
        }

        public string GetPolicyApprovedTemplate(string customerName, string policyNumber)
        {
            return GetBaseTemplate("Policy Approved", $@"
                <p>Dear {customerName},</p>
                <p>Congratulations! Your insurance policy application has been approved and is now active.</p>
                <div class='details-box'>
                    <strong>Policy Number:</strong> {policyNumber}
                </div>
                <p>Please find your Policy Document PDF attached to this email for your records.</p>
                <p>Thank you for choosing {SystemName}.</p>");
        }

        public string GetPolicyRejectedTemplate(string customerName, string reason)
        {
            return GetBaseTemplate("Policy Application Update", $@"
                <p>Dear {customerName},</p>
                <p>Thank you for your interest in {SystemName}. After a thorough review of your application, we regret to inform you that we are unable to approve your policy at this time.</p>
                <div class='details-box'>
                    <strong>Reason for Rejection:</strong> {reason}
                </div>
                <p>If you have any questions, please contact our support team.</p>");
        }

        public string GetPolicyLapsedTemplate(string customerName, string policyNumber)
        {
            return GetBaseTemplate("Policy Lapsed",
                $"<p>Dear {customerName},</p>" +
                $"<p>Your policy <strong>{policyNumber}</strong> has lapsed due to non-payment beyond the grace period.</p>" +
                "<p>A lapsed policy means you no longer have insurance coverage. Please contact your agent or our support team immediately to discuss reinstatement options.</p>");
        }

        public string GetClaimApprovedTemplate(string customerName, string claimId)
        {
            return GetBaseTemplate("Claim Approved", $@"
                <p>Dear {customerName},</p>
                <p>We are pleased to inform you that your claim has been approved. Our team is now processing the settlement.</p>
                <div class='details-box'>
                    <strong>Claim ID:</strong> {claimId}
                </div>
                <p>You will receive a follow-up email once the settlement is finalized.</p>");
        }

        public string GetClaimRejectedTemplate(string customerName, string claimId, string reason)
        {
            return GetBaseTemplate("Claim Update", $@"
                <p>Dear {customerName},</p>
                <p>We have completed the review of your claim. Unfortunately, your claim has been rejected.</p>
                <div class='details-box'>
                    <strong>Claim ID:</strong> {claimId}<br/>
                    <strong>Reason:</strong> {reason}
                </div>
                <p>Please log in to your dashboard for more details or to file an appeal.</p>");
        }

        public string GetClaimSettledTemplate(string customerName, string claimId, decimal amount)
        {
            return GetBaseTemplate("Claim Settled", $@"
                <p>Dear {customerName},</p>
                <p>Your claim has been successfully settled. The settlement amount has been dispatched.</p>
                <div class='details-box'>
                    <strong>Claim ID:</strong> {claimId}<br/>
                    <strong>Total Settlement:</strong> {amount:C}
                </div>
                <p>Please find the Claim Settlement PDF attached with a detailed breakdown.</p>");
        }

        public string GetPremiumReminderTemplate(string customerName, string policyNumber, decimal premium, DateTime dueDate)
        {
            return GetBaseTemplate("Premium Due Reminder", $@"
                <p>Dear {customerName},</p>
                <p>This is a friendly reminder that the premium for your policy is due soon.</p>
                <div class='details-box'>
                    <strong>Policy Number:</strong> {policyNumber}<br/>
                    <strong>Premium Amount:</strong> {premium:C}<br/>
                    <strong>Due Date:</strong> {dueDate:D}
                </div>
                <p>To avoid any lapse in coverage, please ensure payment is made before the due date.</p>");
        }

        public string GetLoanStatusTemplate(string customerName, string status, string loanType, string message)
        {
            return GetBaseTemplate($"Loan {status}", $@"
                <p>Dear {customerName},</p>
                <p>Your {loanType} loan request status has been updated to: <strong>{status}</strong>.</p>
                <div class='details-box'>
                    {message}
                </div>
                <p>If you have any further questions, please feel free to reach out.</p>");
        }

        public string GetForgotPasswordTemplate(string customerName, string resetLink)
        {
            return $@"
                <div style='font-family: Arial, sans-serif; padding: 20px; color: #333;'>
                    <h2 style='color: #2c3e50;'>Password Reset Request</h2>
                    <p>Hi {customerName},</p>
                    <p>We received a request to reset your password. Click the button below to proceed:</p>
                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='{resetLink}' style='background-color: #3498db; color: white; padding: 12px 25px; text-decoration: none; border-radius: 5px;'>Reset Password</a>
                    </div>
                    <p>If you didn't request this, please ignore this email.</p>
                </div>";
        }

        public string GetPaymentConfirmationTemplate(string customerName, string policyNumber, string invoiceNumber, decimal amount)
        {
            return $@"
                <div style='font-family: Arial, sans-serif; padding: 20px; color: #333;'>
                    <h2 style='color: #27ae60;'>Payment Received!</h2>
                    <p>Hi {customerName},</p>
                    <p>Thank you for your payment of <b>₹{amount:N2}</b> towards policy <b>{policyNumber}</b>.</p>
                    <p>Your invoice number is <b>{invoiceNumber}</b>.</p>
                    <p>You can download the official receipt from the 'My Payments' section in the app.</p>
                    <p>Thank you for choosing Hartford Insurance.</p>
                </div>";
        }

        public string GetGenericNotificationTemplate(string title, string message)
        {
            return GetBaseTemplate(title, $@"
                <p>{message}</p>");
        }
    }
}
