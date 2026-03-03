using Application.DTOs;
using Application.Exceptions;
using Application.Interfaces.Repositories;
using Application.Tests.Common;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Application.Tests.Services
{
    public class AuthServiceTests : ApplicationTestBase
    {
        private readonly AuthService _authService;
        private readonly IUserRepository _userRepository;
        private readonly Mock<IConfiguration> _mockConfig;

        public AuthServiceTests()
        {
            _userRepository = new UserRepository(Context);
            _mockConfig = new Mock<IConfiguration>();

            // Setup mock config for JWT
            _mockConfig.Setup(x => x["Jwt:Key"]).Returns("SuperSecretKey12345678901234567890");
            _mockConfig.Setup(x => x["Jwt:Issuer"]).Returns("TestIssuer");
            _mockConfig.Setup(x => x["Jwt:Audience"]).Returns("TestAudience");

            _authService = new AuthService(_userRepository, _mockConfig.Object);
        }

        #region RegisterAsync Tests (5)

        [Fact]
        public async Task RegisterAsync_ValidDetails_ReturnsSuccess()
        {
            // Arrange
            var dto = new RegisterDto { Name = "Test User", Email = "new@test.com", Password = "Pass@123", Phone = "1234567890" };

            // Act
            var result = await _authService.RegisterAsync(dto);

            // Assert
            Assert.Equal("User registered successfully", result);
            var user = await _userRepository.GetByEmailAsync(dto.Email);
            Assert.NotNull(user);
        }

        [Fact]
        public async Task RegisterAsync_ExistingEmail_ThrowsConflictException()
        {
            // Arrange
            var email = "existing@test.com";
            await _userRepository.AddAsync(new User { Name = "Old", Email = email, PasswordHash = "hash", Role = UserRole.Customer });
            await _userRepository.SaveChangesAsync();

            var dto = new RegisterDto { Name = "New", Email = email, Password = "Pass", Phone = "1" };

            // Act & Assert
            await Assert.ThrowsAsync<ConflictException>(() => _authService.RegisterAsync(dto));
        }

        [Fact]
        public async Task RegisterAsync_NullEmail_ThrowsException()
        {
            // Arrange
            var dto = new RegisterDto { Name = "Name", Email = null!, Password = "p", Phone = "1" };

            // Act & Assert
            await Assert.ThrowsAnyAsync<Exception>(() => _authService.RegisterAsync(dto));
        }

        [Fact]
        public async Task RegisterAsync_VeryLongPhone_ShouldStillWork()
        {
            // Arrange
            var dto = new RegisterDto { Name = "T", Email = "long@test.com", Password = "p", Phone = new string('9', 20) };

            // Act
            var result = await _authService.RegisterAsync(dto);

            // Assert
            Assert.Equal("User registered successfully", result);
        }

        [Fact]
        public async Task RegisterAsync_SpecialCharactersInName_ShouldStillWork()
        {
            // Arrange
            var dto = new RegisterDto { Name = "User!@#$%", Email = "spec@test.com", Password = "p", Phone = "1" };

            // Act
            var result = await _authService.RegisterAsync(dto);

            // Assert
            Assert.Equal("User registered successfully", result);
        }

        #endregion

        #region LoginAsync Tests (5)

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsToken()
        {
            // Arrange
            var email = "login@test.com";
            var password = "Password123";
            await _authService.RegisterAsync(new RegisterDto { Name = "L", Email = email, Password = password, Phone = "1" });

            var dto = new LoginDto { Email = email, Password = password };

            // Act
            var response = await _authService.LoginAsync(dto);

            // Assert
            Assert.NotNull(response.Token);
            Assert.Equal(email, response.Email);
        }

        [Fact]
        public async Task LoginAsync_InvalidEmail_ThrowsUnauthorizedException()
        {
            // Arrange
            var dto = new LoginDto { Email = "wrong@test.com", Password = "p" };

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedException>(() => _authService.LoginAsync(dto));
        }

        [Fact]
        public async Task LoginAsync_WrongPassword_ThrowsUnauthorizedException()
        {
            // Arrange
            var email = "wrongpass@test.com";
            await _authService.RegisterAsync(new RegisterDto { Name = "L", Email = email, Password = "Correct", Phone = "1" });

            var dto = new LoginDto { Email = email, Password = "Wrong" };

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedException>(() => _authService.LoginAsync(dto));
        }

        [Fact]
        public async Task LoginAsync_NullCredentials_ThrowsException()
        {
            // Arrange
            var dto = new LoginDto { Email = null!, Password = null! };

            // Act & Assert
            await Assert.ThrowsAnyAsync<Exception>(() => _authService.LoginAsync(dto));
        }

        [Fact]
        public async Task LoginAsync_EmptyPassword_ThrowsUnauthorizedException()
        {
            // Arrange
            var email = "empty@test.com";
            await _authService.RegisterAsync(new RegisterDto { Name = "L", Email = email, Password = "p", Phone = "1" });

            var dto = new LoginDto { Email = email, Password = "" };

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedException>(() => _authService.LoginAsync(dto));
        }

        #endregion

        #region CreatePasswordResetTokenAsync Tests (5)

        [Fact]
        public async Task CreatePasswordResetTokenAsync_ExistingEmail_ReturnsToken()
        {
            // Arrange
            var email = "reset@test.com";
            await _authService.RegisterAsync(new RegisterDto { Name = "R", Email = email, Password = "p", Phone = "1" });

            // Act
            var token = await _authService.CreatePasswordResetTokenAsync(email);

            // Assert
            Assert.NotNull(token);
        }

        [Fact]
        public async Task CreatePasswordResetTokenAsync_NonExistentEmail_ReturnsNull()
        {
            // Arrange
            var email = "none@test.com";

            // Act
            var token = await _authService.CreatePasswordResetTokenAsync(email);

            // Assert
            Assert.Null(token);
        }

        [Fact]
        public async Task CreatePasswordResetTokenAsync_NullEmail_ReturnsNull()
        {
            // Act
            var token = await _authService.CreatePasswordResetTokenAsync(null!);

            // Assert
            Assert.Null(token);
        }

        [Fact]
        public async Task CreatePasswordResetTokenAsync_EmptyEmail_ReturnsNull()
        {
            // Act
            var token = await _authService.CreatePasswordResetTokenAsync("");

            // Assert
            Assert.Null(token);
        }

        [Fact]
        public async Task CreatePasswordResetTokenAsync_MultipleCalls_GeneratesDifferentTokens()
        {
            // Arrange
            var email = "multi@test.com";
            await _authService.RegisterAsync(new RegisterDto { Name = "R", Email = email, Password = "p", Phone = "1" });

            // Act
            var token1 = await _authService.CreatePasswordResetTokenAsync(email);
            var token2 = await _authService.CreatePasswordResetTokenAsync(email);

            // Assert
            Assert.NotEqual(token1, token2);
        }

        #endregion

        #region ResetPasswordAsync Tests (5)

        [Fact]
        public async Task ResetPasswordAsync_ValidToken_ReturnsTrue()
        {
            // Arrange
            var email = "actualreset@test.com";
            await _authService.RegisterAsync(new RegisterDto { Name = "R", Email = email, Password = "OldPassword", Phone = "1" });
            var token = await _authService.CreatePasswordResetTokenAsync(email);

            var dto = new ResetPasswordDto { Token = token!, NewPassword = "NewPassword123" };

            // Act
            var result = await _authService.ResetPasswordAsync(dto);

            // Assert
            Assert.True(result);

            // Verify login with new password
            var loginResult = await _authService.LoginAsync(new LoginDto { Email = email, Password = "NewPassword123" });
            Assert.NotNull(loginResult.Token);
        }

        [Fact]
        public async Task ResetPasswordAsync_InvalidToken_ReturnsFalse()
        {
            // Arrange
            var dto = new ResetPasswordDto { Token = "InvalidToken", NewPassword = "p" };

            // Act
            var result = await _authService.ResetPasswordAsync(dto);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ResetPasswordAsync_ExpiredToken_ReturnsFalse()
        {
            // Arrange
            var email = "expired@test.com";
            await _authService.RegisterAsync(new RegisterDto { Name = "R", Email = email, Password = "p", Phone = "1" });
            var token = await _authService.CreatePasswordResetTokenAsync(email);

            // Manually expire token in DB
            var user = await _userRepository.GetByEmailAsync(email);
            user!.ResetTokenExpiry = DateTime.UtcNow.AddMinutes(-1);
            await _userRepository.SaveChangesAsync();

            var dto = new ResetPasswordDto { Token = token!, NewPassword = "p" };

            // Act
            var result = await _authService.ResetPasswordAsync(dto);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ResetPasswordAsync_NullToken_ReturnsFalse()
        {
            // Arrange
            var dto = new ResetPasswordDto { Token = null!, NewPassword = "p" };

            // Act
            var result = await _authService.ResetPasswordAsync(dto);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ResetPasswordAsync_TokenUsedTwice_ReturnsFalseOnSecondTime()
        {
            // Arrange
            var email = "twice@test.com";
            await _authService.RegisterAsync(new RegisterDto { Name = "R", Email = email, Password = "p", Phone = "1" });
            var token = await _authService.CreatePasswordResetTokenAsync(email);

            var dto = new ResetPasswordDto { Token = token!, NewPassword = "p1" };

            // Act
            await _authService.ResetPasswordAsync(dto);
            var result2 = await _authService.ResetPasswordAsync(dto);

            // Assert
            Assert.False(result2);
        }

        #endregion
    }
}
