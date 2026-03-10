using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs;

namespace Application.Services
{
    public interface IPaymentService
    {
        Task<PaymentResponseDto> MakePaymentAsync(int customerId, CreatePaymentDto dto);
        Task<IEnumerable<PaymentResponseDto>> GetMyPaymentsAsync(int customerId);
        Task<IEnumerable<PaymentResponseDto>> GetPaymentsByPolicyAsync(int policyId);
        Task<IEnumerable<PaymentResponseDto>> GetAllPaymentsAsync();  // Admin
        Task<byte[]> GenerateInvoicePdfAsync(int paymentId, int customerId);
    }
}