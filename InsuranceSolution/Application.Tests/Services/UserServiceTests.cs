using Application.DTOs;
using Application.Exceptions;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Application.Tests.Services
{
    public class UserServiceTests
    {
        private (InsuranceDbContext db, UserService service) BuildTestContextAndService()
        {
            var dbOptions = new DbContextOptionsBuilder<InsuranceDbContext>()
                .UseInMemoryDatabase($"UserServiceTestDb_{Guid.NewGuid()}")
                .Options;

            var dbContext = new InsuranceDbContext(dbOptions);
            var userRepo = new UserRepository(dbContext);
            var policyRepo = new PolicyRepository(dbContext);
            var service = new UserService(userRepo, policyRepo);

            return (dbContext, service);
        }


        [Fact]
        public async Task CreateAgentAsync_ShouldCreateAgent_WhenEmailIsUnique()
        {
            var (db, service) = BuildTestContextAndService();
            var dto = new CreateAgentDto { Name = "Agent One", Email = "agent@test.com", Password = "Pass@123", Phone = "9999999999" };

            var result = await service.CreateAgentAsync(dto);

            Assert.NotNull(result);
            Assert.Equal("Agent One", result.Name);
            Assert.Equal("Agent", result.Role);
            var saved = await db.Users.FirstAsync();
            Assert.Equal(UserRole.Agent, saved.Role);
        }

        [Fact]
        public async Task CreateAgentAsync_ShouldThrowConflictException_WhenEmailAlreadyExists()
        {
            var (db, service) = BuildTestContextAndService();
            db.Users.Add(new User { Email = "dup@test.com", Role = UserRole.Agent });
            await db.SaveChangesAsync();

            var dto = new CreateAgentDto { Name = "A", Email = "dup@test.com", Password = "Pass@123", Phone = "1234567890" };

            await Assert.ThrowsAsync<ConflictException>(() => service.CreateAgentAsync(dto));
        }

        [Fact]
        public async Task CreateAgentAsync_ShouldHashPassword_WhenUserIsCreated()
        {
            var (db, service) = BuildTestContextAndService();
            var password = "PlainPass@123";
            var dto = new CreateAgentDto { Name = "A", Email = "a@test.com", Password = password, Phone = "0000000000" };

            await service.CreateAgentAsync(dto);

            var saved = await db.Users.FirstAsync();
            Assert.NotEqual(password, saved.PasswordHash);
            Assert.True(BCrypt.Net.BCrypt.Verify(password, saved.PasswordHash));
        }


        [Fact]
        public async Task CreateClaimsOfficerAsync_ShouldCreateClaimsOfficer_WhenEmailIsUnique()
        {
            var (db, service) = BuildTestContextAndService();
            var dto = new CreateClaimsOfficerDto { Name = "Officer One", Email = "officer@test.com", Password = "Pass@123", Phone = "8888888888" };

            var result = await service.CreateClaimsOfficerAsync(dto);

            Assert.NotNull(result);
            Assert.Equal("ClaimsOfficer", result.Role);
            var saved = await db.Users.FirstAsync();
            Assert.Equal(UserRole.ClaimsOfficer, saved.Role);
        }

        [Fact]
        public async Task CreateClaimsOfficerAsync_ShouldThrowConflictException_WhenEmailAlreadyExists()
        {
            var (db, service) = BuildTestContextAndService();
            db.Users.Add(new User { Email = "taken@test.com", Role = UserRole.ClaimsOfficer });
            await db.SaveChangesAsync();

            var dto = new CreateClaimsOfficerDto { Name = "O", Email = "taken@test.com", Password = "Pass@123", Phone = "1234567890" };

            await Assert.ThrowsAsync<ConflictException>(() => service.CreateClaimsOfficerAsync(dto));
        }

        [Fact]
        public async Task CreateClaimsOfficerAsync_ShouldSetIsActiveToTrue_WhenCreated()
        {
            var (db, service) = BuildTestContextAndService();
            var dto = new CreateClaimsOfficerDto { Name = "O", Email = "o@test.com", Password = "Pass@123", Phone = "0000000000" };

            await service.CreateClaimsOfficerAsync(dto);

            var saved = await db.Users.FirstAsync();
            Assert.True(saved.IsActive);
        }


        [Fact]
        public async Task GetAllCustomersAsync_ShouldReturnOnlyCustomers_WhenMixedRolesExist()
        {
            var (db, service) = BuildTestContextAndService();
            db.Users.AddRange(
                new User { Role = UserRole.Customer, Email = "c1@t.com" },
                new User { Role = UserRole.Customer, Email = "c2@t.com" },
                new User { Role = UserRole.Agent, Email = "a@t.com" });
            await db.SaveChangesAsync();

            var results = await service.GetAllCustomersAsync();

            Assert.Equal(2, results.Count());
            Assert.All(results, r => Assert.Equal("Customer", r.Role));
        }

        [Fact]
        public async Task GetAllCustomersAsync_ShouldReturnEmptyList_WhenNoCustomersExist()
        {
            var (db, service) = BuildTestContextAndService();
            db.Users.Add(new User { Role = UserRole.Agent, Email = "a@t.com" });
            await db.SaveChangesAsync();

            var results = await service.GetAllCustomersAsync();

            Assert.Empty(results);
        }

        [Fact]
        public async Task GetAllCustomersAsync_ShouldMapEntityToDtoCorrectly()
        {
            var (db, service) = BuildTestContextAndService();
            db.Users.Add(new User { Name = "John", Email = "john@t.com", Role = UserRole.Customer, Phone = "111" });
            await db.SaveChangesAsync();

            var results = await service.GetAllCustomersAsync();

            var r = results.First();
            Assert.Equal("John", r.Name);
            Assert.Equal("john@t.com", r.Email);
            Assert.Equal("111", r.Phone);
        }


        [Fact]
        public async Task GetAllAgentsAsync_ShouldReturnOnlyAgents_WhenMixedRolesExist()
        {
            var (db, service) = BuildTestContextAndService();
            db.Users.AddRange(
                new User { Role = UserRole.Agent, Email = "a1@t.com" },
                new User { Role = UserRole.Customer, Email = "c@t.com" });
            await db.SaveChangesAsync();

            var results = await service.GetAllAgentsAsync();

            Assert.Single(results);
            Assert.Equal("Agent", results.First().Role);
        }

        [Fact]
        public async Task GetAllAgentsAsync_ShouldReturnEmptyList_WhenNoAgentsExist()
        {
            var (db, service) = BuildTestContextAndService();

            var results = await service.GetAllAgentsAsync();

            Assert.Empty(results);
        }

        [Fact]
        public async Task GetAllAgentsAsync_ShouldReturnAllAgents_WhenMultipleExist()
        {
            var (db, service) = BuildTestContextAndService();
            db.Users.AddRange(
                new User { Role = UserRole.Agent, Email = "a1@t.com" },
                new User { Role = UserRole.Agent, Email = "a2@t.com" },
                new User { Role = UserRole.Agent, Email = "a3@t.com" });
            await db.SaveChangesAsync();

            var results = await service.GetAllAgentsAsync();

            Assert.Equal(3, results.Count());
        }

        
    }
}
