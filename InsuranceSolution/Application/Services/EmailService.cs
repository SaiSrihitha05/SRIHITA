using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettingsDto _settings;

        public EmailService(IOptions<EmailSettingsDto> settings)
        {
            _settings = settings.Value;
        }

        public virtual async Task SendEmailAsync(
            string toEmail, string toName,
            string subject, string htmlBody)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(
                _settings.SenderName, _settings.SenderEmail));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = subject;

            message.Body = new TextPart("html") { Text = htmlBody };

            using var client = new SmtpClient();
            await client.ConnectAsync(
                _settings.Host,
                _settings.Port,
                SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(
                _settings.SenderEmail, _settings.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

        public async Task SendPremiumReminderAsync(
            string toEmail, string toName,
            string policyNumber, DateTime dueDate, decimal amount)
        {
            var subject = $"Premium Due Reminder - {policyNumber}";
            var body = $"""
                <html><body style="font-family: Arial, sans-serif;">
                    <div style="max-width:600px;margin:auto;border:1px solid #ddd;border-radius:8px;overflow:hidden;">
                        <div style="background:#1a73e8;padding:20px;text-align:center;">
                            <h2 style="color:white;margin:0;">Premium Due Reminder</h2>
                        </div>
                        <div style="padding:30px;">
                            <p>Dear <strong>{toName}</strong>,</p>
                            <p>This is a reminder that your insurance premium is due soon.</p>
                            <table style="width:100%;border-collapse:collapse;margin:20px 0;">
                                <tr style="background:#f5f5f5;">
                                    <td style="padding:10px;border:1px solid #ddd;"><strong>Policy Number</strong></td>
                                    <td style="padding:10px;border:1px solid #ddd;">{policyNumber}</td>
                                </tr>
                                <tr>
                                    <td style="padding:10px;border:1px solid #ddd;"><strong>Due Date</strong></td>
                                    <td style="padding:10px;border:1px solid #ddd;">{dueDate:dd-MMM-yyyy}</td>
                                </tr>
                                <tr style="background:#f5f5f5;">
                                    <td style="padding:10px;border:1px solid #ddd;"><strong>Amount Due</strong></td>
                                    <td style="padding:10px;border:1px solid #ddd;"><strong>₹{amount:N2}</strong></td>
                                </tr>
                            </table>
                            <p>Please make your payment before the due date to avoid policy lapse.</p>
                            <p style="color:#888;font-size:12px;">Hartford Insurance Co.</p>
                        </div>
                    </div>
                </body></html>
                """;

            await SendEmailAsync(toEmail, toName, subject, body);
        }

        public async Task SendPolicyStatusChangedAsync(
            string toEmail, string toName,
            string policyNumber, string newStatus)
        {
            var statusColor = newStatus switch
            {
                "Active" => "#28a745",
                "Rejected" => "#dc3545",
                "Cancelled" => "#ffc107",
                _ => "#1a73e8"
            };

            var subject = $"Policy Status Update - {policyNumber}";
            var body = $"""
                <html><body style="font-family: Arial, sans-serif;">
                    <div style="max-width:600px;margin:auto;border:1px solid #ddd;border-radius:8px;overflow:hidden;">
                        <div style="background:#1a73e8;padding:20px;text-align:center;">
                            <h2 style="color:white;margin:0;">Policy Status Update</h2>
                        </div>
                        <div style="padding:30px;">
                            <p>Dear <strong>{toName}</strong>,</p>
                            <p>Your policy status has been updated.</p>
                            <table style="width:100%;border-collapse:collapse;margin:20px 0;">
                                <tr style="background:#f5f5f5;">
                                    <td style="padding:10px;border:1px solid #ddd;"><strong>Policy Number</strong></td>
                                    <td style="padding:10px;border:1px solid #ddd;">{policyNumber}</td>
                                </tr>
                                <tr>
                                    <td style="padding:10px;border:1px solid #ddd;"><strong>New Status</strong></td>
                                    <td style="padding:10px;border:1px solid #ddd;">
                                        <span style="color:{statusColor};font-weight:bold;">{newStatus}</span>
                                    </td>
                                </tr>
                            </table>
                            <p style="color:#888;font-size:12px;">Hartford Insurance Co.</p>
                        </div>
                    </div>
                </body></html>
                """;

            await SendEmailAsync(toEmail, toName, subject, body);
        }

        public async Task SendPaymentConfirmationAsync(
            string toEmail, string toName,
            string policyNumber, string invoiceNumber,
            decimal amount, DateTime paymentDate)
        {
            var subject = $"Payment Confirmation - {invoiceNumber}";
            var body = $"""
                <html><body style="font-family: Arial, sans-serif;">
                    <div style="max-width:600px;margin:auto;border:1px solid #ddd;border-radius:8px;overflow:hidden;">
                        <div style="background:#28a745;padding:20px;text-align:center;">
                            <h2 style="color:white;margin:0;">Payment Successful!</h2>
                        </div>
                        <div style="padding:30px;">
                            <p>Dear <strong>{toName}</strong>,</p>
                            <p>Your premium payment has been received successfully.</p>
                            <table style="width:100%;border-collapse:collapse;margin:20px 0;">
                                <tr style="background:#f5f5f5;">
                                    <td style="padding:10px;border:1px solid #ddd;"><strong>Invoice Number</strong></td>
                                    <td style="padding:10px;border:1px solid #ddd;">{invoiceNumber}</td>
                                </tr>
                                <tr>
                                    <td style="padding:10px;border:1px solid #ddd;"><strong>Policy Number</strong></td>
                                    <td style="padding:10px;border:1px solid #ddd;">{policyNumber}</td>
                                </tr>
                                <tr style="background:#f5f5f5;">
                                    <td style="padding:10px;border:1px solid #ddd;"><strong>Amount Paid</strong></td>
                                    <td style="padding:10px;border:1px solid #ddd;"><strong>₹{amount:N2}</strong></td>
                                </tr>
                                <tr>
                                    <td style="padding:10px;border:1px solid #ddd;"><strong>Payment Date</strong></td>
                                    <td style="padding:10px;border:1px solid #ddd;">{paymentDate:dd-MMM-yyyy HH:mm}</td>
                                </tr>
                            </table>
                            <p style="color:#888;font-size:12px;">Hartford Insurance Co.</p>
                        </div>
                    </div>
                </body></html>
                """;

            await SendEmailAsync(toEmail, toName, subject, body);
        }
    }
}