using Application.DTOs;
using Application.Exceptions;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Application.Tests.Services
{
    public class AuthServiceTests
    {
        private (InsuranceDbContext db, AuthService service) BuildTestContextAndService()
        {
            var dbOptions = new DbContextOptionsBuilder<InsuranceDbContext>()
                .UseInMemoryDatabase($"AuthServiceTestDb_{Guid.NewGuid()}")
                .ConfigureWarnings(cfg => cfg.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            var dbContext = new InsuranceDbContext(dbOptions);
            var userRepo = new UserRepository(dbContext);

            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["Jwt:Key"]).Returns("SuperSecretKeyForTestingPurposesOnly123!");
            mockConfig.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
            mockConfig.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");

            var mockEmail = new Mock<IEmailService>();
            var mockTemplate = new Mock<IEmailTemplateService>();
            var mockScopeFactory = new Mock<IServiceScopeFactory>();

            var service = new AuthService(userRepo, mockConfig.Object, mockEmail.Object, mockTemplate.Object, mockScopeFactory.Object);

            return (dbContext, service);
        }


        [Fact]
        public async Task RegisterAsync_ShouldRegisterUser_WhenEmailIsNew()
        {
            var (db, service) = BuildTestContextAndService();
            var dto = new RegisterDto { Name = "Test User", Email = "newuser@example.com", Password = "Password123!", Phone = "1234567890" };

            var result = await service.RegisterAsync(dto);

            Assert.Equal("User registered successfully", result);
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            Assert.NotNull(user);
            Assert.Equal(dto.Name, user.Name);
        }

        [Fact]
        public async Task RegisterAsync_ShouldThrowConflictException_WhenEmailAlreadyExists()
        {
            var (db, service) = BuildTestContextAndService();
            var email = "existing@example.com";
            db.Users.Add(new User { Name = "Existing", Email = email, PasswordHash = "hashed", Role = UserRole.Customer, IsActive = true });
            await db.SaveChangesAsync();

            var dto = new RegisterDto { Name = "New User", Email = email, Password = "Password123!", Phone = "1234567890" };

            await Assert.ThrowsAsync<ConflictException>(() => service.RegisterAsync(dto));
        }

        [Fact]
        public async Task RegisterAsync_ShouldHashPasswordAndAssignCustomerRole_WhenRegistered()
        {
            var (db, service) = BuildTestContextAndService();
            var password = "PlainPass@123";
            var dto = new RegisterDto { Name = "User", Email = "u@example.com", Password = password, Phone = "0000000000" };

            await service.RegisterAsync(dto);

            var saved = await db.Users.FirstAsync();
            Assert.NotEqual(password, saved.PasswordHash);
            Assert.True(BCrypt.Net.BCrypt.Verify(password, saved.PasswordHash));
            Assert.Equal(UserRole.Customer, saved.Role);
        }


        [Fact]
        public async Task LoginAsync_ShouldReturnToken_WhenCredentialsAreValid()
        {
            var (db, service) = BuildTestContextAndService();
            var email = "loginuser@example.com";
            var password = "Password123!";
            db.Users.Add(new User { Name = "Login User", Email = email, PasswordHash = BCrypt.Net.BCrypt.HashPassword(password), Role = UserRole.Customer, IsActive = true });
            await db.SaveChangesAsync();

            var result = await service.LoginAsync(new LoginDto { Email = email, Password = password });

            Assert.NotNull(result);
            Assert.Equal(email, result.Email);
            Assert.NotNull(result.Token);
            Assert.Equal("Customer", result.Role);
        }

        [Fact]
        public async Task LoginAsync_ShouldThrowUnauthorizedException_WhenEmailDoesNotExist()
        {
            var (_, service) = BuildTestContextAndService();

            await Assert.ThrowsAsync<UnauthorizedException>(() =>
                service.LoginAsync(new LoginDto { Email = "nobody@example.com", Password = "Any123!" }));
        }

        [Fact]
        public async Task LoginAsync_ShouldThrowUnauthorizedException_WhenPasswordIsWrong()
        {
            var (db, service) = BuildTestContextAndService();
            var email = "user@example.com";
            db.Users.Add(new User { Email = email, PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPass!"), Role = UserRole.Customer, IsActive = true });
            await db.SaveChangesAsync();

            await Assert.ThrowsAsync<UnauthorizedException>(() =>
                service.LoginAsync(new LoginDto { Email = email, Password = "WrongPass!" }));
        }


        [Fact]
        public async Task CreatePasswordResetTokenAsync_ShouldReturnTrue_WhenEmailExists()
        {
            var (db, service) = BuildTestContextAndService();
            db.Users.Add(new User { Email = "reset@example.com", PasswordHash = "h", Role = UserRole.Customer, IsActive = true });
            await db.SaveChangesAsync();

            var result = await service.CreatePasswordResetTokenAsync("reset@example.com");

            Assert.True(result);
            var user = await db.Users.FirstAsync(u => u.Email == "reset@example.com");
            Assert.NotNull(user.ResetToken);
        }

        [Fact]
        public async Task CreatePasswordResetTokenAsync_ShouldReturnFalse_WhenEmailDoesNotExist()
        {
            var (_, service) = BuildTestContextAndService();

            var result = await service.CreatePasswordResetTokenAsync("nobody@example.com");

            Assert.False(result);
        }

        [Fact]
        public async Task CreatePasswordResetTokenAsync_ShouldGenerateUniqueToken_EachTime()
        {
            var (db, service) = BuildTestContextAndService();
            db.Users.Add(new User { Email = "u@example.com", PasswordHash = "h", Role = UserRole.Customer, IsActive = true });
            await db.SaveChangesAsync();

            await service.CreatePasswordResetTokenAsync("u@example.com");
            var user1 = await db.Users.AsNoTracking().FirstAsync(u => u.Email == "u@example.com");
            var token1 = user1.ResetToken;

            await service.CreatePasswordResetTokenAsync("u@example.com");
            var user2 = await db.Users.AsNoTracking().FirstAsync(u => u.Email == "u@example.com");
            var token2 = user2.ResetToken;

            Assert.NotEqual(token1, token2);
        }


        [Fact]
        public async Task ResetPasswordAsync_ShouldReturnTrue_WhenTokenIsValid()
        {
            var (db, service) = BuildTestContextAndService();
            var token = Guid.NewGuid().ToString();
            db.Users.Add(new User
            {
                Email = "r@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPass!"),
                ResetToken = token,
                ResetTokenExpiry = DateTime.UtcNow.AddHours(1),
                Role = UserRole.Customer,
                IsActive = true
            });
            await db.SaveChangesAsync();

            var result = await service.ResetPasswordAsync(new ResetPasswordDto { Token = token, NewPassword = "NewPass@123" });

            Assert.True(result);
            var user = await db.Users.FirstAsync();
            Assert.True(BCrypt.Net.BCrypt.Verify("NewPass@123", user.PasswordHash));
            Assert.Null(user.ResetToken);
        }

        [Fact]
        public async Task ResetPasswordAsync_ShouldReturnFalse_WhenTokenIsExpired()
        {
            var (db, service) = BuildTestContextAndService();
            var token = Guid.NewGuid().ToString();
            db.Users.Add(new User
            {
                Email = "e@example.com",
                PasswordHash = "h",
                ResetToken = token,
                ResetTokenExpiry = DateTime.UtcNow.AddHours(-1), // Expired
                Role = UserRole.Customer,
                IsActive = true
            });
            await db.SaveChangesAsync();

            var result = await service.ResetPasswordAsync(new ResetPasswordDto { Token = token, NewPassword = "NewPass@123" });

            Assert.False(result);
        }

        [Fact]
        public async Task ResetPasswordAsync_ShouldReturnFalse_WhenTokenIsInvalid()
        {
            var (_, service) = BuildTestContextAndService();

            var result = await service.ResetPasswordAsync(new ResetPasswordDto { Token = "invalid-token", NewPassword = "NewPass@123" });

            Assert.False(result);
        }
    }
}
