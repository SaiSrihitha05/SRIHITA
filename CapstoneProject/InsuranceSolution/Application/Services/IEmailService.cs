using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string toName,
            string subject, string htmlBody);

        Task SendPremiumReminderAsync(string toEmail, string toName,
            string policyNumber, DateTime dueDate, decimal amount);

        Task SendPolicyStatusChangedAsync(string toEmail, string toName,
            string policyNumber, string newStatus);

        Task SendPaymentConfirmationAsync(string toEmail, string toName,
            string policyNumber, string invoiceNumber,
            decimal amount, DateTime paymentDate);
    }
}