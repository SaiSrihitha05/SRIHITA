using Application.DTOs;
using Application.Exceptions;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class LoanService : ILoanService
    {
        private readonly ILoanRepository _loanRepository;
        private readonly IPolicyRepository _policyRepository;
        private readonly IPaymentRepository _paymentRepository; 
        private readonly INotificationService _notificationService;

        public LoanService(
            ILoanRepository loanRepository,
            IPolicyRepository policyRepository,
            IPaymentRepository paymentRepository, 
            INotificationService notificationService)
        {
            _loanRepository = loanRepository;
            _policyRepository = policyRepository;
            _paymentRepository = paymentRepository; 
            _notificationService = notificationService;
        }

        public async Task<LoanResponseDto> ApplyForLoanAsync(int customerId, ApplyLoanDto dto)
        {
            var policy = await _policyRepository.GetByIdWithDetailsAsync(dto.PolicyAssignmentId);

            if (policy == null)
                throw new NotFoundException("Policy", dto.PolicyAssignmentId);

            if (policy.CustomerId != customerId)
                throw new ForbiddenException("You can only apply loan on your own policy.");

            if (policy.Status != PolicyStatus.Active)
                throw new BadRequestException("Loan can only be applied on Active policies.");

            if (!policy.Plan!.HasLoanFacility)
                throw new BadRequestException("This plan does not offer loan facility.");

            var yearsActive = (DateTime.UtcNow - policy.StartDate).Days / 365;
            if (yearsActive < policy.Plan.LoanEligibleAfterYears)
                throw new BadRequestException($"Loan is available only after {policy.Plan.LoanEligibleAfterYears} years. Your policy is {yearsActive} year(s) old.");

            var existingLoan = await _loanRepository.GetActiveLoanByPolicyAsync(dto.PolicyAssignmentId);
            if (existingLoan != null)
                throw new BadRequestException("An active loan already exists on this policy. Please repay it before applying for a new loan.");

            // Calculate surrender value: Total Premiums Paid × 30%
            var totalPaid = policy.Payments?.Where(p => p.Status == PaymentStatus.Completed).Sum(p => p.Amount) ?? 0;
            var surrenderValue = totalPaid * 0.30m;

            if (surrenderValue <= 0)
                throw new BadRequestException("Insufficient surrender value for loan.");

            var maxLoan = surrenderValue * (policy.Plan.MaxLoanPercentage / 100);

            var loan = new PolicyLoan
            {
                PolicyAssignmentId = policy.Id,
                CustomerId = customerId,
                LoanAmount = maxLoan,
                InterestRate = policy.Plan.LoanInterestRate,
                OutstandingBalance = maxLoan,
                TotalInterestPaid = 0,
                Status = LoanStatus.Active,
                LoanDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                Remarks = "Loan approved against policy surrender value"
            };

            await _loanRepository.AddAsync(loan);
            await _loanRepository.SaveChangesAsync(); // Save first to get loan.Id

            await _notificationService.CreateNotificationAsync(
                userId: customerId,
                title: "Loan Approved",
                message: $"Your loan of Rs.{maxLoan:N2} has been approved against policy {policy.PolicyNumber}. Interest rate: {policy.Plan.LoanInterestRate}% p.a.",
                type: NotificationType.General,
                policyId: policy.Id,
                claimId: null,
                paymentId: null);

            return MapToDto(loan, policy);
        }

        public async Task<LoanResponseDto> RepayLoanAsync(int customerId, RepayLoanDto dto)
        {
            var loan = await _loanRepository.GetByIdAsync(dto.PolicyLoanId);

            if (loan == null)
                throw new NotFoundException("Loan", dto.PolicyLoanId);

            if (loan.CustomerId != customerId)
                throw new ForbiddenException("You can only repay your own loans.");

            if (loan.Status != LoanStatus.Active)
                throw new BadRequestException("This loan is already closed.");

            if (dto.Amount <= 0)
                throw new BadRequestException("Repayment amount must be greater than zero.");

            var monthlyInterest = Math.Round(loan.OutstandingBalance * (loan.InterestRate / 12 / 100), 2);
            var maxPayable = loan.OutstandingBalance + monthlyInterest;

            if (dto.Amount > maxPayable)
                throw new BadRequestException($"Repayment amount cannot exceed total payoff amount of Rs.{maxPayable:N2} (Principal: {loan.OutstandingBalance:N2} + Interest: {monthlyInterest:N2})");

            var interestPaid = Math.Min(dto.Amount, monthlyInterest);
            var principalPaid = dto.Amount - interestPaid;

            var payment = new Payment
            {
                PolicyAssignmentId = loan.PolicyAssignmentId,
                PolicyLoanId = loan.Id, // ✅ LINK TO LOAN
                Amount = dto.Amount,
                PrincipalPaid = principalPaid, // ✅ LOAN DETAIL
                InterestPaid = interestPaid,   // ✅ LOAN DETAIL
                BalanceAfter = Math.Round(loan.OutstandingBalance - principalPaid, 2), // ✅ LOAN DETAIL
                InstallmentsPaid = 0, // 0 for loan repayment
                PaymentDate = DateTime.UtcNow,
                PaymentMethod = "Loan Repayment",
                TransactionReference = $"LOAN-REPAY-{loan.Id}-{DateTime.UtcNow.Ticks}",
                Status = PaymentStatus.Completed,
                InvoiceNumber = await _paymentRepository.GenerateInvoiceNumberAsync(),
                CreatedAt = DateTime.UtcNow
            };
            await _paymentRepository.AddAsync(payment);

            // Perform status and balance updates
            loan.OutstandingBalance = Math.Round(loan.OutstandingBalance - principalPaid, 2);
            loan.TotalInterestPaid = Math.Round(loan.TotalInterestPaid + interestPaid, 2);

            if (loan.OutstandingBalance <= 0.01m)
            {
                loan.OutstandingBalance = 0;
                loan.Status = LoanStatus.Closed;
                loan.ClosedDate = DateTime.UtcNow;
            }

            _loanRepository.Update(loan);

            await _loanRepository.SaveChangesAsync();
            await _paymentRepository.SaveChangesAsync();

            await _notificationService.CreateNotificationAsync(
                userId: customerId,
                title: loan.Status == LoanStatus.Closed ? "Loan Fully Repaid" : "Loan Repayment Recorded",
                message: $"Repayment of Rs.{dto.Amount:N2} recorded. Outstanding: Rs.{loan.OutstandingBalance:N2}",
                type: NotificationType.General,
                policyId: loan.PolicyAssignmentId,
                claimId: null,
                paymentId: null);

            return MapToDto(loan, loan.PolicyAssignment);
        }

        public async Task<IEnumerable<LoanResponseDto>> GetMyLoansAsync(int customerId)
        {
            var loans = await _loanRepository.GetByCustomerIdAsync(customerId);
            return loans.Select(l => MapToDto(l, l.PolicyAssignment));
        }

        public async Task<LoanResponseDto?> GetLoanByIdAsync(int id)
        {
            var loan = await _loanRepository.GetByIdAsync(id);
            if (loan == null) return null;
            return MapToDto(loan, loan.PolicyAssignment);
        }

        public async Task<decimal> GetOutstandingLoanAsync(int policyId)
        {
            var loan = await _loanRepository.GetActiveLoanByPolicyAsync(policyId);
            return loan?.OutstandingBalance ?? 0;
        }

        public async Task<IEnumerable<LoanResponseDto>> GetAllLoansAsync()
        {
            var loans = await _loanRepository.GetAllAsync();
            return loans.Select(l => MapToDto(l, l.PolicyAssignment));
        }

        public async Task<IEnumerable<LoanResponseDto>> GetLoansByPolicyAsync(int policyId)
        {
            var loans = await _loanRepository.GetByPolicyIdAsync(policyId);
            return loans.Select(l => MapToDto(l, l.PolicyAssignment));
        }

        private LoanResponseDto MapToDto(PolicyLoan loan, PolicyAssignment policy)
        {
            return new LoanResponseDto
            {
                Id = loan.Id,
                PolicyAssignmentId = loan.PolicyAssignmentId,
                PolicyNumber = policy?.PolicyNumber ?? "",
                PlanName = policy?.Plan?.PlanName ?? "",
                CustomerName = policy?.Customer?.Name ?? "",
                CustomerEmail = policy?.Customer?.Email ?? "",
                LoanAmount = loan.LoanAmount,
                InterestRate = loan.InterestRate,
                OutstandingBalance = loan.OutstandingBalance,
                TotalInterestPaid = loan.TotalInterestPaid,
                Status = loan.Status.ToString(),
                LoanDate = loan.LoanDate,
                ClosedDate = loan.ClosedDate,
                // ✅ FETCH HISTORY FROM PAYMENTS TABLE
                Repayments = loan.Payments?
                    .OrderByDescending(p => p.PaymentDate)
                    .Select(p => new LoanRepaymentDto
                    {
                        Id = p.Id,
                        Amount = p.Amount,
                        PrincipalPaid = p.PrincipalPaid ?? 0,
                        InterestPaid = p.InterestPaid ?? 0,
                        BalanceAfter = p.BalanceAfter ?? 0,
                        RepaymentDate = p.PaymentDate
                    }).ToList() ?? new List<LoanRepaymentDto>()
            };
        }
    }
}
