using Application.DTOs;
using Application.Exceptions;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static QuestPDF.Helpers.Colors;

namespace Application.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IPolicyRepository _policyRepository;
        private readonly IUserRepository _userRepository;
        private readonly INotificationService _notificationService;
        private readonly IEmailService _emailService;
        private readonly IEmailTemplateService _templateService;

        public PaymentService(
            IPaymentRepository paymentRepository,
            IUserRepository userRepository,
            IPolicyRepository policyRepository,
            INotificationService notificationService,
            IEmailService emailService,
            IEmailTemplateService templateService)
        {
            _paymentRepository = paymentRepository;
            _policyRepository = policyRepository;
            _userRepository = userRepository;
            _notificationService = notificationService;
            _emailService = emailService;
            _templateService = templateService;
        }

        public async Task<PaymentResponseDto> MakePaymentAsync(
            int customerId, CreatePaymentDto dto)
        {
            // Validate policy exists and belongs to customer
            var policy = await _policyRepository
                .GetByIdWithDetailsAsync(dto.PolicyAssignmentId);

            if (policy == null)
                throw new NotFoundException("Policy", dto.PolicyAssignmentId);

            if (policy.CustomerId != customerId)
                throw new ForbiddenException(
                    "You can only make payments for your own policies");

            if (policy.Status == PolicyStatus.Lapsed)
                throw new BadRequestException(
                    "This policy has lapsed. Please contact support to reinstate before making payments.");

            if (policy.Status != PolicyStatus.Active)
                throw new BadRequestException(
                    "Payments can only be made on Active policies.");
            // Removed validation so customers can pay at any time

            // Total installments = 1 (current) + extra
            var totalInstallments = 1 + dto.ExtraInstallments;

            // Total amount = premium * total installments
            var totalAmount = policy.TotalPremiumAmount * totalInstallments;

            var transactionRef = GenerateTransactionReference();

            var payment = new Payment
            {
                PolicyAssignmentId = dto.PolicyAssignmentId,
                Amount = totalAmount,
                InstallmentsPaid = totalInstallments,
                PaymentDate = DateTime.UtcNow,
                PaymentMethod = dto.PaymentMethod,
                TransactionReference = transactionRef,
                Status = PaymentStatus.Completed,
                PaymentType = PaymentType.PremiumPayment,
                InvoiceNumber = await _paymentRepository
                                           .GenerateInvoiceNumberAsync(),
                CreatedAt = DateTime.UtcNow
            };

            await _paymentRepository.AddAsync(payment);

            // Update NextDueDate on policy after successful payment
            policy.NextDueDate = CalculateNextDueDate(
                policy.NextDueDate,
                policy.PremiumFrequency,
                totalInstallments);

            policy.CommissionStatus = CommissionStatus.Paid;

            _policyRepository.Update(policy);
            await _paymentRepository.SaveChangesAsync();
            // Get customer details
            var customer = await _userRepository.GetByIdAsync(policy.CustomerId);

            // Send in-app notification
            await _notificationService.CreateNotificationAsync(
                userId: policy.CustomerId,
                title: "Payment Successful",
                message: $"Payment of ₹{totalAmount:N2} received for policy " +
                           $"{policy.PolicyNumber}. Invoice: {payment.InvoiceNumber}",
                type: NotificationType.PaymentConfirmation,
                policyId: policy.Id,
                claimId: null,
                paymentId: payment.Id);

            // Notify Agent if assigned
            if (policy.AgentId.HasValue)
            {
                await _notificationService.CreateNotificationAsync(
                    userId: policy.AgentId.Value,
                    title: "Premium Paid",
                    message: $"Premium of ₹{totalAmount:N2} has been paid for policy {policy.PolicyNumber} by {customer?.Name}.",
                    type: NotificationType.PaymentConfirmation,
                    policyId: policy.Id,
                    claimId: null,
                    paymentId: payment.Id);
            }

            // Send email confirmation
            try
            {
                // Generate Invoice PDF for attachment
                var pdfBytes = GeneratePdfBytes(payment);

                _ = Task.Run(async () =>
                {
                    try
                    {
                        var body = _templateService.GetPaymentConfirmationTemplate(
                            customer!.Name, policy.PolicyNumber, payment.InvoiceNumber, totalAmount);

                        await _emailService.SendEmailAsync(new EmailRequest
                        {
                            ToEmail = customer!.Email,
                            ToName = customer.Name,
                            Subject = $"Payment Confirmation - {payment.InvoiceNumber}",
                            HtmlContent = body,
                            Attachments = new List<EmailAttachment>
                            {
                                new EmailAttachment
                                {
                                    Name = $"Invoice_{payment.InvoiceNumber}.pdf",
                                    Content = pdfBytes,
                                    ContentType = "application/pdf"
                                }
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Background Email Failed: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SMTP Connection Error: {ex.Message}");
            }

            // Ensure the DTO returns immediately regardless of email status
            return MapToDto(payment, policy.PolicyNumber, policy.NextDueDate);
        }

        public async Task<PaymentResponseDto> ReinstatePolicyAsync(int customerId, int policyAssignmentId)
        {
            // Validate policy exists and belongs to customer
            var policy = await _policyRepository.GetByIdWithDetailsAsync(policyAssignmentId);

            if (policy == null)
                throw new NotFoundException("Policy", policyAssignmentId);

            if (policy.CustomerId != customerId)
                throw new ForbiddenException("You can only reinstate your own policies.");

            if (policy.Status != PolicyStatus.Lapsed)
                throw new BadRequestException(
                    $"Only lapsed policies can be reinstated. Current status: {policy.Status}");

            if (!policy.LapsedDate.HasValue)
                throw new BadRequestException(
                    "Lapse date not recorded. Please contact administrator.");

            var daysSinceLapse = (DateTime.Today - policy.LapsedDate.Value).TotalDays;

            if (daysSinceLapse > policy.Plan!.ReinstatementDays)
                throw new BadRequestException(
                    $"Reinstatement window of {policy.Plan.ReinstatementDays} days has expired. " +
                    $"Policy lapsed on {policy.LapsedDate.Value:dd MMM yyyy}.");

            // Calculate reinstatement amount
            // Ensure at least 1 missed month if lapsed
            var missedMonths = Math.Max(1, (int)Math.Ceiling(daysSinceLapse / 30.0));
            var monthlyPremium = policy.TotalPremiumAmount / 12;
            var missedPremiums = Math.Round(monthlyPremium * missedMonths, 2);
            
            // Penalty from plan
            var penalty = policy.Plan.ReinstatementPenaltyAmount;
            
            // If penalty is 0, we can add a small processing fee for demonstration if needed, 
            // but we'll stick to plan values.
            var totalAmount = missedPremiums + penalty;

            // Generate reference — same method you already have
            var transactionRef = GenerateTransactionReference();

            // Create payment record — same shape as your existing payments
            var payment = new Payment
            {
                PolicyAssignmentId = policyAssignmentId,
                Amount = totalAmount,
                InstallmentsPaid = missedMonths,
                PaymentDate = DateTime.UtcNow,
                PaymentMethod = "Online",          // mock — no gateway
                TransactionReference = transactionRef,
                Status = PaymentStatus.Completed,
                PaymentType = PaymentType.ReinstatementPayment,  // NEW type
                InvoiceNumber = await _paymentRepository.GenerateInvoiceNumberAsync(),
                CreatedAt = DateTime.UtcNow
            };

            await _paymentRepository.AddAsync(payment);

            // Reinstate the policy
            policy.Status = PolicyStatus.Active;
            policy.ReinstatedDate = DateTime.Today;
            policy.LapsedDate = null;
            policy.NextDueDate = CalculateNextDueDate(
                                        DateTime.Today,
                                        policy.PremiumFrequency,
                                        1);              // reset to 1 month from today (or according to installments)
            policy.EndDate = DateTime.Today.AddYears(1);

            _policyRepository.Update(policy);
            await _paymentRepository.SaveChangesAsync();

            // Get customer — same as MakePaymentAsync
            var customer = await _userRepository.GetByIdAsync(policy.CustomerId);

            // In-app notification — same service, same pattern
            await _notificationService.CreateNotificationAsync(
                userId: policy.CustomerId,
                title: "Policy Reinstated Successfully",
                message: $"Your policy {policy.PolicyNumber} has been reinstated. " +
                          $"Reinstatement payment of ₹{totalAmount:N2} received. " +
                          $"Invoice: {payment.InvoiceNumber}. " +
                          $"Next due: {policy.NextDueDate:dd MMM yyyy}.",
                type: NotificationType.PolicyReinstated,
                policyId: policy.Id,
                claimId: null,
                paymentId: payment.Id);

            // Notify agent if assigned — same as MakePaymentAsync
            if (policy.AgentId.HasValue)
            {
                await _notificationService.CreateNotificationAsync(
                    userId: policy.AgentId.Value,
                    title: "Policy Reinstated",
                    message: $"Policy {policy.PolicyNumber} has been reinstated by " +
                              $"{customer?.Name}. Reinstatement amount: ₹{totalAmount:N2}.",
                    type: NotificationType.PolicyReinstated,
                    policyId: policy.Id,
                    claimId: null,
                    paymentId: payment.Id);
            }

            // Email — with Attachment
            try
            {
                var pdfBytes = GeneratePdfBytes(payment);

                _ = Task.Run(async () =>
                {
                    try
                    {
                        var body = _templateService.GetPaymentConfirmationTemplate(
                            customer!.Name,
                            policy.PolicyNumber,
                            payment.InvoiceNumber,
                            totalAmount);

                        await _emailService.SendEmailAsync(new EmailRequest
                        {
                            ToEmail = customer!.Email,
                            ToName = customer.Name,
                            Subject = $"Policy Reinstatement Confirmation - {payment.InvoiceNumber}",
                            HtmlContent = body,
                            Attachments = new List<EmailAttachment>
                            {
                                new EmailAttachment
                                {
                                    Name = $"Invoice_{payment.InvoiceNumber}.pdf",
                                    Content = pdfBytes,
                                    ContentType = "application/pdf"
                                }
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Reinstatement Email Failed: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email Generation Failed: {ex.Message}");
            }

            // Return same DTO shape as MakePaymentAsync
            return MapToDto(payment, policy.PolicyNumber, policy.NextDueDate);
        }

        public async Task<PaymentResponseDto> RenewPolicyAsync(int customerId, int policyId)
        {
            var policy = await _policyRepository.GetByIdWithDetailsAsync(policyId);
            if (policy == null) throw new NotFoundException("Policy", policyId);
            if (policy.CustomerId != customerId)
                throw new ForbiddenException("You can only renew your own policies.");

            if (policy.Status != PolicyStatus.Active && policy.Status != PolicyStatus.Expired)
                throw new BadRequestException("Only Active or Expired policies can be renewed.");

            var daysToExpiry = (policy.EndDate - DateTime.Today).TotalDays;
            if (Math.Abs(daysToExpiry) > 30)
                throw new BadRequestException("Policy can only be renewed within 30 days before or after the expiry date.");

            // 1. Extend EndDate correctly (from current EndDate, not from today)
            var oldEndDate = policy.EndDate;
            policy.EndDate = policy.EndDate.AddYears(policy.TermYears);
            
            // Re-activate if it was expired
            if (policy.Status == PolicyStatus.Expired)
            {
                policy.Status = PolicyStatus.Active;
            }

            // 2. Apply Coverage Increase if plan supports it
            if (policy.Plan!.CoverageIncreasing)
            {
                var increaseRate = policy.Plan.CoverageIncreaseRate;
                foreach (var member in policy.PolicyMembers)
                {
                    member.CoverageAmount += Math.Round(member.CoverageAmount * increaseRate / 100, 2);
                }
            }

            // 3. Reset NextDueDate for the new term
            // Since they are paying for renewal now, we advance the due date relative to today's cycle
            policy.NextDueDate = CalculateNextDueDate(DateTime.Today, policy.PremiumFrequency, 1);

            // 4. Create Payment record
            var transactionRef = GenerateTransactionReference();
            var payment = new Payment
            {
                PolicyAssignmentId = policyId,
                Amount = policy.TotalPremiumAmount, // Renewal payment covers the first premium of the new term
                InstallmentsPaid = 1,
                PaymentDate = DateTime.UtcNow,
                PaymentMethod = "Online",
                TransactionReference = transactionRef,
                Status = PaymentStatus.Completed,
                PaymentType = PaymentType.RenewalPayment,
                InvoiceNumber = await _paymentRepository.GenerateInvoiceNumberAsync(),
                CreatedAt = DateTime.UtcNow
            };

            await _paymentRepository.AddAsync(payment);
            _policyRepository.Update(policy);
            await _paymentRepository.SaveChangesAsync();

            // 5. Notifications
            var customer = await _userRepository.GetByIdAsync(policy.CustomerId);
            await _notificationService.CreateNotificationAsync(
                userId: policy.CustomerId,
                title: "Policy Renewed Successfully",
                message: $"Your policy {policy.PolicyNumber} has been renewed until {policy.EndDate:dd MMM yyyy}.",
                type: NotificationType.PolicyStatusUpdate,
                policyId: policy.Id,
                claimId: null,
                paymentId: payment.Id);

            // 6. Email with Invoice & Renewal Confirmation
            try
            {
                var pdfBytes = GeneratePdfBytes(payment);
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var body = _templateService.GetPolicyRenewalSuccessTemplate(
                            customer!.Name, policy.PolicyNumber, policy.EndDate);

                        await _emailService.SendEmailAsync(new EmailRequest
                        {
                            ToEmail = customer!.Email,
                            ToName = customer.Name,
                            Subject = $"Policy Renewal Confirmation - {policy.PolicyNumber}",
                            HtmlContent = body,
                            Attachments = new List<EmailAttachment>
                            {
                                new EmailAttachment
                                {
                                    Name = $"Renewal_Receipt_{payment.InvoiceNumber}.pdf",
                                    Content = pdfBytes,
                                    ContentType = "application/pdf"
                                }
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Renewal Email Failed: {ex.Message}");
                    }
                });
            }
            catch { }

            return MapToDto(payment, policy.PolicyNumber, policy.NextDueDate);
        }
        private static DateTime CalculateNextDueDate(
            DateTime currentDueDate,
            PremiumFrequency frequency,
            int installments)
        {
            // Move forward by (frequency * installments)
            return frequency switch
            {
                PremiumFrequency.Monthly =>
                    currentDueDate.AddMonths(1 * installments),

                PremiumFrequency.Quarterly =>
                    currentDueDate.AddMonths(3 * installments),

                PremiumFrequency.Yearly =>
                    currentDueDate.AddYears(1 * installments),

                _ => currentDueDate.AddMonths(1 * installments)
            };
        }
        public async Task<IEnumerable<PaymentResponseDto>> GetMyPaymentsAsync(
            int customerId)
        {
            var payments = await _paymentRepository
                .GetByCustomerIdAsync(customerId);

            return payments
                .OrderByDescending(p => p.PaymentDate)
                .Select(p =>
                    MapToDto(p, p.PolicyAssignment?.PolicyNumber ?? string.Empty,
                    p.PolicyAssignment?.NextDueDate ?? DateTime.MinValue));
        }

        public async Task<IEnumerable<PaymentResponseDto>> GetPaymentsByPolicyAsync(
            int policyId)
        {
            var payments = await _paymentRepository.GetByPolicyIdAsync(policyId);
            return payments
                .OrderByDescending(p => p.PaymentDate)
                .Select(p =>
                    MapToDto(p, p.PolicyAssignment?.PolicyNumber ?? string.Empty,
                    p.PolicyAssignment?.NextDueDate ?? DateTime.MinValue));
        }

        public async Task<IEnumerable<PaymentResponseDto>> GetAllPaymentsAsync()
        {
            var payments = await _paymentRepository.GetAllAsync();
            return payments
                .OrderByDescending(p => p.PaymentDate)
                .Select(p =>
                    MapToDto(p, p.PolicyAssignment?.PolicyNumber ?? string.Empty
                    , p.PolicyAssignment?.NextDueDate ?? DateTime.MinValue));
        }

        public async Task<byte[]> GenerateInvoicePdfAsync(
            int paymentId, int customerId)
        {
            var payment = await _paymentRepository.GetByIdAsync(paymentId);
            if (payment == null)
                throw new NotFoundException("Payment", paymentId);

            // Verify ownership
            if (payment.PolicyAssignment?.CustomerId != customerId)
                throw new ForbiddenException(
                    "You can only download your own invoices");

            return GeneratePdfBytes(payment);
        }


        private static string GenerateTransactionReference() =>
            $"TXN{DateTime.UtcNow:yyyyMMddHHmmss}{Guid.NewGuid().ToString()[..6].ToUpper()}";



        private static byte[] GeneratePdfBytes(Payment payment)
        {
            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    // Header
                    page.Header().Column(col =>
                    {
                        col.Item().AlignCenter().Text("INSURANCE PREMIUM INVOICE")
                            .FontSize(20).Bold();
                        col.Item().AlignCenter().Text("Hartford Insurance Co.")
                            .FontSize(14);
                        col.Item().PaddingTop(5).LineHorizontal(1);
                    });

                    // Content
                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                    {
                        // Invoice Details
                        col.Item().Background(Colors.Grey.Lighten3)
                            .Padding(10).Text("Invoice Details").Bold().FontSize(14);

                        col.Item().PaddingTop(10).Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn();
                                cols.RelativeColumn();
                            });

                            AddRow(table, "Invoice Number",
                                payment.InvoiceNumber);
                            AddRow(table, "Transaction Reference",
                                payment.TransactionReference);
                            AddRow(table, "Payment Date",
                                payment.PaymentDate.ToString("dd-MMM-yyyy HH:mm"));
                            AddRow(table, "Payment Method",
                                payment.PaymentMethod);
                            AddRow(table, "Status",
                                payment.Status.ToString());
                        });

                        col.Item().PaddingTop(20).Background(Colors.Grey.Lighten3)
                            .Padding(10).Text("Policy Details").Bold().FontSize(14);

                        col.Item().PaddingTop(10).Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn();
                                cols.RelativeColumn();
                            });

                            AddRow(table, "Policy Number",
                                payment.PolicyAssignment?.PolicyNumber ?? "-");
                            AddRow(table, "Premium Frequency",
                                payment.PolicyAssignment?.PremiumFrequency.ToString() ?? "-");
                            AddRow(table, "Next Due Date",
                                payment.PolicyAssignment?.NextDueDate
                                    .ToString("dd-MMM-yyyy") ?? "-");
                        });

                        // Amount Box
                        col.Item().PaddingTop(30)
                            .Border(1)
                            .Background(Colors.Blue.Lighten4)
                            .Padding(15)
                            .Row(row =>
                            {
                                row.RelativeItem().Text("Amount Paid")
                                    .FontSize(16).Bold();
                                row.RelativeItem().AlignRight()
                                    .Text($"Rs.{payment.Amount:N2}")
                                    .FontSize(16).Bold();
                            });
                    });

                    // Footer
                    page.Footer().AlignCenter().Column(col =>
                    {
                        col.Item().LineHorizontal(1);
                        col.Item().PaddingTop(5).AlignCenter()
                            .Text("Thank you for your payment!")
                            .FontSize(12).Italic();
                        col.Item().AlignCenter()
                            .Text($"Generated on {DateTime.UtcNow:dd-MMM-yyyy HH:mm} UTC")
                            .FontSize(10).FontColor(Colors.Grey.Medium);
                    });
                });
            });

            return document.GeneratePdf();
        }

        // Helper to add table rows
        private static void AddRow(TableDescriptor table, string label, string value)
        {
            table.Cell().Padding(5).Text(label).Bold();
            table.Cell().Padding(5).Text(value);
        }

        private static PaymentResponseDto MapToDto(
            Payment payment, string policyNumber, DateTime nextDueDate) => new()
            {
                Id = payment.Id,
                PolicyAssignmentId = payment.PolicyAssignmentId,
                PolicyNumber = policyNumber,
                Amount = payment.Amount,
                InstallmentsPaid = payment.InstallmentsPaid,
                PaymentDate = payment.PaymentDate,
                PaymentMethod = payment.PaymentMethod,
                TransactionReference = payment.TransactionReference,
                Status = payment.Status.ToString(),
                PaymentType = payment.PaymentType.ToString(),
                CommissionStatus = payment.PolicyAssignment?.CommissionStatus.ToString() ?? string.Empty,
                InvoiceNumber = payment.InvoiceNumber,
                NextDueDate = nextDueDate,
                CreatedAt = payment.CreatedAt
            };
    }
}