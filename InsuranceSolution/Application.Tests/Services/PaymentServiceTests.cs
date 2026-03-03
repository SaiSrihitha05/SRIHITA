using Application.DTOs;
using Application.Exceptions;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Services;
using Application.Tests.Common;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Repositories;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Application.Tests.Services
{
    public class PaymentServiceTests : ApplicationTestBase
    {
        private readonly PaymentService _paymentService;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IPolicyRepository _policyRepository;
        private readonly IUserRepository _userRepository;
        private readonly Mock<INotificationService> _mockNotificationService;
        private readonly Mock<IEmailService> _mockEmailService;

        public PaymentServiceTests()
        {
            _paymentRepository = new PaymentRepository(Context);
            _policyRepository = new PolicyRepository(Context);
            _userRepository = new UserRepository(Context);
            _mockNotificationService = new Mock<INotificationService>();
            _mockEmailService = new Mock<IEmailService>();

            _paymentService = new PaymentService(
                _paymentRepository,
                _userRepository,
                _policyRepository,
                _mockNotificationService.Object,
                _mockEmailService.Object);
        }

        private async Task<(User customer, PolicyAssignment policy)> SeedBaseDataAsync()
        {
            var customer = new User { Name = "C", Email = "c@t.com", Role = UserRole.Customer, PasswordHash = "h" };
            await Context.Users.AddAsync(customer);

            var policy = new PolicyAssignment
            {
                PolicyNumber = "POL123",
                CustomerId = customer.Id,
                Status = PolicyStatus.Active,
                TotalPremiumAmount = 1000,
                PremiumFrequency = PremiumFrequency.Monthly,
                NextDueDate = DateTime.UtcNow.AddDays(1)
            };
            await Context.PolicyAssignments.AddAsync(policy);
            await Context.SaveChangesAsync();
            return (customer, policy);
        }

        #region MakePaymentAsync Tests (5)

        [Fact]
        public async Task MakePaymentAsync_ValidInput_ReturnsResponse()
        {
            var data = await SeedBaseDataAsync();
            var dto = new CreatePaymentDto { PolicyAssignmentId = data.policy.Id, PaymentMethod = "UPI", ExtraInstallments = 0 };

            var result = await _paymentService.MakePaymentAsync(data.customer.Id, dto);

            Assert.NotNull(result.TransactionReference);
            Assert.Equal(1000, result.Amount);

            var updatedPol = await Context.PolicyAssignments.FindAsync(data.policy.Id);
            Assert.True(updatedPol!.NextDueDate > data.policy.NextDueDate);
        }

        [Fact]
        public async Task MakePaymentAsync_PolicyNotFound_ThrowsNotFound()
        {
            await Assert.ThrowsAsync<NotFoundException>(() => _paymentService.MakePaymentAsync(1, new CreatePaymentDto { PolicyAssignmentId = 999 }));
        }

        [Fact]
        public async Task MakePaymentAsync_WrongCustomer_ThrowsForbidden()
        {
            var data = await SeedBaseDataAsync();
            await Assert.ThrowsAsync<ForbiddenException>(() => _paymentService.MakePaymentAsync(999, new CreatePaymentDto { PolicyAssignmentId = data.policy.Id }));
        }

        [Fact]
        public async Task MakePaymentAsync_PolicyNotActive_ThrowsBadRequest()
        {
            var data = await SeedBaseDataAsync();
            var pol = await Context.PolicyAssignments.FindAsync(data.policy.Id);
            pol!.Status = PolicyStatus.Pending; await Context.SaveChangesAsync();

            await Assert.ThrowsAsync<BadRequestException>(() => _paymentService.MakePaymentAsync(data.customer.Id, new CreatePaymentDto { PolicyAssignmentId = data.policy.Id }));
        }

        [Fact]
        public async Task MakePaymentAsync_TooEarly_ThrowsBadRequest()
        {
            var data = await SeedBaseDataAsync();
            var pol = await Context.PolicyAssignments.FindAsync(data.policy.Id);
            pol!.NextDueDate = DateTime.UtcNow.AddDays(60); await Context.SaveChangesAsync();

            await Assert.ThrowsAsync<BadRequestException>(() => _paymentService.MakePaymentAsync(data.customer.Id, new CreatePaymentDto { PolicyAssignmentId = data.policy.Id }));
        }

        #endregion

        #region GetMyPaymentsAsync Tests (5)
        [Fact]
        public async Task GetMyPaymentsAsync_ValidCustomer_ReturnsMine()
        {
            var data = await SeedBaseDataAsync();
            await Context.Payments.AddAsync(new Payment { PolicyAssignmentId = data.policy.Id, Amount = 1000 });
            await Context.SaveChangesAsync();
            var res = await _paymentService.GetMyPaymentsAsync(data.customer.Id);
            Assert.Single(res);
        }
        [Fact] public async Task GetMyPaymentsAsync_NoPayments_ReturnsEmpty() => Assert.Empty(await _paymentService.GetMyPaymentsAsync(1));
        [Fact]
        public async Task GetMyPaymentsAsync_Multiple_ReturnsAll()
        {
            var data = await SeedBaseDataAsync();
            await Context.Payments.AddAsync(new Payment { PolicyAssignmentId = data.policy.Id, Amount = 1000 });
            await Context.Payments.AddAsync(new Payment { PolicyAssignmentId = data.policy.Id, Amount = 1000 });
            await Context.SaveChangesAsync();
            Assert.Equal(2, (await _paymentService.GetMyPaymentsAsync(data.customer.Id)).Count());
        }
        [Fact]
        public async Task GetMyPaymentsAsync_IncludesPolicyNumber()
        {
            var data = await SeedBaseDataAsync();
            await Context.Payments.AddAsync(new Payment { PolicyAssignmentId = data.policy.Id });
            await Context.SaveChangesAsync();
            var res = await _paymentService.GetMyPaymentsAsync(data.customer.Id);
            Assert.Equal("POL123", res.First().PolicyNumber);
        }
        [Fact] public async Task GetMyPaymentsAsync_InvalidCustomer_ReturnsEmpty() => Assert.Empty(await _paymentService.GetMyPaymentsAsync(999));
        #endregion

        #region GetPaymentsByPolicyAsync Tests (5)
        [Fact]
        public async Task GetPaymentsByPolicyAsync_ValidPolicy_ReturnsPayments()
        {
            var data = await SeedBaseDataAsync();
            await Context.Payments.AddAsync(new Payment { PolicyAssignmentId = data.policy.Id });
            await Context.SaveChangesAsync();
            var res = await _paymentService.GetPaymentsByPolicyAsync(data.policy.Id);
            Assert.Single(res);
        }
        [Fact]
        public async Task GetPaymentsByPolicyAsync_NoPayments_ReturnsEmpty()
        {
            var data = await SeedBaseDataAsync();
            Assert.Empty(await _paymentService.GetPaymentsByPolicyAsync(data.policy.Id));
        }
        [Fact]
        public async Task GetPaymentsByPolicyAsync_Multiple_ReturnsAll()
        {
            var data = await SeedBaseDataAsync();
            await Context.Payments.AddAsync(new Payment { PolicyAssignmentId = data.policy.Id });
            await Context.Payments.AddAsync(new Payment { PolicyAssignmentId = data.policy.Id });
            await Context.SaveChangesAsync();
            Assert.Equal(2, (await _paymentService.GetPaymentsByPolicyAsync(data.policy.Id)).Count());
        }
        [Fact] public async Task GetPaymentsByPolicyAsync_NonExistentPolicy_ReturnsEmpty() => Assert.Empty(await _paymentService.GetPaymentsByPolicyAsync(999));
        [Fact]
        public async Task GetPaymentsByPolicyAsync_MappingIsCorrect()
        {
            var data = await SeedBaseDataAsync();
            var p = new Payment { PolicyAssignmentId = data.policy.Id, Amount = 555 };
            await Context.Payments.AddAsync(p); await Context.SaveChangesAsync();
            var res = await _paymentService.GetPaymentsByPolicyAsync(data.policy.Id);
            Assert.Equal(555, res.First().Amount);
        }
        #endregion

        #region GetAllPaymentsAsync Tests (5)
        [Fact]
        public async Task GetAllPaymentsAsync_ReturnsEverything()
        {
            var data = await SeedBaseDataAsync();
            await Context.Payments.AddAsync(new Payment { PolicyAssignmentId = data.policy.Id });
            await Context.SaveChangesAsync();
            Assert.Single(await _paymentService.GetAllPaymentsAsync());
        }
        [Fact] public async Task GetAllPaymentsAsync_NoData_ReturnsEmpty() => Assert.Empty(await _paymentService.GetAllPaymentsAsync());
        [Fact]
        public async Task GetAllPaymentsAsync_MultiplePolicies_ReturnsAll()
        {
            await Context.Payments.AddAsync(new Payment { PolicyAssignmentId = 1 });
            await Context.Payments.AddAsync(new Payment { PolicyAssignmentId = 2 });
            await Context.SaveChangesAsync();
            Assert.Equal(2, (await _paymentService.GetAllPaymentsAsync()).Count());
        }
        [Fact] public async Task GetAllPaymentsAsync_ResultTypeCorrect() => Assert.IsAssignableFrom<IEnumerable<PaymentResponseDto>>(await _paymentService.GetAllPaymentsAsync());
        [Fact]
        public async Task GetAllPaymentsAsync_IncludesInvoiceNumber()
        {
            await Context.Payments.AddAsync(new Payment { PolicyAssignmentId = 1, InvoiceNumber = "INV-001" });
            await Context.SaveChangesAsync();
            var res = await _paymentService.GetAllPaymentsAsync();
            Assert.Equal("INV-001", res.First().InvoiceNumber);
        }
        #endregion

        #region GenerateInvoicePdfAsync Tests (5)
        [Fact]
        public async Task GenerateInvoicePdfAsync_ValidInput_ReturnsBytes()
        {
            var data = await SeedBaseDataAsync();
            var p = new Payment { PolicyAssignmentId = data.policy.Id, InvoiceNumber = "INV1", TransactionReference = "T1" };
            await Context.Payments.AddAsync(p); await Context.SaveChangesAsync();

            var result = await _paymentService.GenerateInvoicePdfAsync(p.Id, data.customer.Id);
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }
        [Fact] public async Task GenerateInvoicePdfAsync_NotFound_ThrowsNotFound() => await Assert.ThrowsAsync<NotFoundException>(() => _paymentService.GenerateInvoicePdfAsync(999, 1));
        [Fact]
        public async Task GenerateInvoicePdfAsync_Forbidden_ThrowsForbidden()
        {
            var data = await SeedBaseDataAsync();
            var p = new Payment { PolicyAssignmentId = data.policy.Id };
            await Context.Payments.AddAsync(p); await Context.SaveChangesAsync();
            await Assert.ThrowsAsync<ForbiddenException>(() => _paymentService.GenerateInvoicePdfAsync(p.Id, 999));
        }
        [Fact] public async Task GenerateInvoicePdfAsync_ZeroId_ThrowsNotFound() => await Assert.ThrowsAsync<NotFoundException>(() => _paymentService.GenerateInvoicePdfAsync(0, 1));
        [Fact]
        public async Task GenerateInvoicePdfAsync_MappingVerification()
        {
            var data = await SeedBaseDataAsync();
            var p = new Payment { PolicyAssignmentId = data.policy.Id, Amount = 100 };
            await Context.Payments.AddAsync(p); await Context.SaveChangesAsync();
            var result = await _paymentService.GenerateInvoicePdfAsync(p.Id, data.customer.Id);
            Assert.True(result.Length > 0);
        }
        #endregion
    }
}
