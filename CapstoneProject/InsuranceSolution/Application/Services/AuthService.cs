using Application.DTOs;
using Application.Exceptions;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _templateService;
    private readonly IServiceScopeFactory _scopeFactory;

    public AuthService(IUserRepository userRepository,
                       IConfiguration configuration,
                       IEmailService emailService,
                       IEmailTemplateService templateService,
                       IServiceScopeFactory scopeFactory)
    {
        _userRepository = userRepository;
        _configuration = configuration;
        _emailService = emailService;
        _templateService = templateService;
        _scopeFactory = scopeFactory;
    }

    public async Task<string> RegisterAsync(RegisterDto dto)
    {
        var existingUser = await _userRepository
            .GetByEmailAsync(dto.Email);

        if (existingUser != null)
            throw new ConflictException("User with this email already exists");

        var user = new User
        {
            Name = dto.Name,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Phone = dto.Phone,
            Role = UserRole.Customer,
            DateOfBirth = dto.DateOfBirth,
            Gender = dto.Gender,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        return "User registered successfully";
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await _userRepository
            .GetByEmailAsync(dto.Email);

        if (user == null)
            throw new UnauthorizedException("Invalid credentials");

        bool isValid =
            BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);

        if (!isValid)
            throw new UnauthorizedException("Invalid credentials");

        var token = GenerateJwtToken(user);

        return new AuthResponseDto
        {
            Token = token,
            Email = user.Email,
            Role = user.Role.ToString()
        };
    }


    private string GenerateJwtToken(User user)
    {
        var claims = new[]
        {
            new System.Security.Claims.Claim(
                System.Security.Claims.ClaimTypes.NameIdentifier,
                user.Id.ToString()
            ),
            new System.Security.Claims.Claim(
                System.Security.Claims.ClaimTypes.Email,
                user.Email
            ),
            new System.Security.Claims.Claim(
                System.Security.Claims.ClaimTypes.Role,
                user.Role.ToString()
            )
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));

        var creds = new SigningCredentials(
            key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    // No email sending, just validate email exists and return token directly
    public async Task<bool> CreatePasswordResetTokenAsync(string email)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null) return false;

        string token = Guid.NewGuid().ToString();
        DateTime expiry = DateTime.UtcNow.AddHours(1);

        await _userRepository.UpdateResetTokenAsync(user.Id, token, expiry);

        // Send Email (Safe Background Task)
        _ = Task.Run(async () => {
            using var scope = _scopeFactory.CreateScope();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            var templateService = scope.ServiceProvider.GetRequiredService<IEmailTemplateService>();

            try {
                var resetLink = $"http://localhost:4200/reset-password?token={token}";
                var body = templateService.GetForgotPasswordTemplate(user.Name, resetLink);
                
                await emailService.SendEmailAsync(new EmailRequest 
                { 
                    ToEmail = user.Email, 
                    ToName = user.Name, 
                    Subject = "Reset Your Password", 
                    HtmlContent = body 
                });
            } catch { /* log error if needed */ }
        });

        return true;
    }
    public async Task<bool> ResetPasswordAsync(ResetPasswordDto dto)
    {
        var user = await _userRepository.GetUserByResetTokenAsync(dto.Token);

        // Validate user and token expiry
        if (user == null || user.ResetTokenExpiry < DateTime.UtcNow)
            return false;

        // Hash new password and clear token fields
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.ResetToken = null;
        user.ResetTokenExpiry = null;

        _userRepository.Update(user); 
        await _userRepository.SaveChangesAsync(); 

        // Send Confirmation Email
        var body = _templateService.GetGenericNotificationTemplate("Password Reset Successful", 
            "Your password has been reset successfully. If you did not perform this action, please contact support immediately.");
        
        await _emailService.SendEmailAsync(new EmailRequest
        {
            ToEmail = user.Email,
            ToName = user.Name,
            Subject = "Password Reset Successful",
            HtmlContent = body
        });

        return true;
    }
}