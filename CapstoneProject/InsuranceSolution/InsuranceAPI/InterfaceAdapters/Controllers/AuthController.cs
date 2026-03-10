using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Application.DTOs;
using Application.Interfaces;
using Application.Services;

namespace InsuranceAPI.InterfaceAdapters.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var result = await _authService.RegisterAsync(dto);
            return Ok(new { message = result });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var result = await _authService.LoginAsync(dto);
            return Ok(result);
        }
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(
    [FromBody] ForgotPasswordDto dto)
        {
            var token = await _authService
                .CreatePasswordResetTokenAsync(dto.Email);

            if (token == null)
                return BadRequest(new { message = "No account found with this email." });

            // ✅ Return token directly — no email sent
            return Ok(new { token = token });
        }
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(
    [FromBody] ResetPasswordDto dto)
        {
            var result = await _authService.ResetPasswordAsync(dto);
            if (!result)
                return BadRequest(new
                {
                    message = "Invalid or expired reset token."
                });

            return Ok(new
            {
                message = "Password has been reset successfully."
            });
        }
        [HttpGet("get-captcha")]
        public IActionResult GetCaptcha()
        {
            string code = Guid.NewGuid()
                .ToString()
                .Replace("-", "")
                .Substring(0, 6)
                .ToUpper();
            return Ok(new { captchaCode = code });
        }
    }
}
