using Application.DTOs;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InsuranceAPI.InterfaceAdapters.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentsController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        // ── Customer 

        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> MakePayment([FromBody] CreatePaymentDto dto)
        {
            var customerId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var result = await _paymentService.MakePaymentAsync(customerId, dto);
            return Ok(result);
        }

        [HttpGet("my-payments")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetMyPayments()
        {
            var customerId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var result = await _paymentService.GetMyPaymentsAsync(customerId);
            return Ok(result);
        }

        [HttpGet("{id}/invoice")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> DownloadInvoice(int id)
        {
            var customerId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var pdfBytes = await _paymentService
                .GenerateInvoicePdfAsync(id, customerId);

            return File(pdfBytes, "application/pdf",
                $"Invoice_{id}_{DateTime.UtcNow:yyyyMMdd}.pdf");
        }

        // ── Admin 

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllPayments()
        {
            var result = await _paymentService.GetAllPaymentsAsync();
            return Ok(result);
        }

        [HttpGet("policy/{policyId}")]
        [Authorize(Roles = "Admin,Agent")]
        public async Task<IActionResult> GetPaymentsByPolicy(int policyId)
        {
            var result = await _paymentService
                .GetPaymentsByPolicyAsync(policyId);
            return Ok(result);
        }
    }
}