using Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto> LoginAsync(LoginDto dto);
        Task<string> RegisterAsync(RegisterDto dto);
        Task<string?> CreatePasswordResetTokenAsync(string email);
        Task<bool> ResetPasswordAsync(ResetPasswordDto dto);
    }
}
