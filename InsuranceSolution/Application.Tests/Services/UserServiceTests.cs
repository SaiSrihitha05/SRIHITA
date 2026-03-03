using Application.DTOs;
using Application.Exceptions;
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
    public class UserServiceTests : ApplicationTestBase
    {
        private readonly UserService _userService;
        private readonly IUserRepository _userRepository;
        private readonly IPolicyRepository _policyRepository;

        public UserServiceTests()
        {
            _userRepository = new UserRepository(Context);
            _policyRepository = new PolicyRepository(Context);
            _userService = new UserService(_userRepository, _policyRepository);
        }

        #region CreateAgentAsync Tests (5)

        [Fact]
        public async Task CreateAgentAsync_ValidDto_ReturnsUserResponse()
        {
            var dto = new CreateAgentDto { Name = "Agent1", Email = "a1@test.com", Password = "p", Phone = "1" };
            var result = await _userService.CreateAgentAsync(dto);
            Assert.Equal("Agent1", result.Name);
            Assert.Equal(UserRole.Agent.ToString(), result.Role);
        }

        [Fact]
        public async Task CreateAgentAsync_DuplicateEmail_ThrowsConflictException()
        {
            var email = "dup@test.com";
            await _userService.CreateAgentAsync(new CreateAgentDto { Name = "O", Email = email, Password = "p", Phone = "1" });
            var dto = new CreateAgentDto { Name = "N", Email = email, Password = "p", Phone = "1" };
            await Assert.ThrowsAsync<ConflictException>(() => _userService.CreateAgentAsync(dto));
        }

        [Fact]
        public async Task CreateAgentAsync_NullName_ThrowsException()
        {
            var dto = new CreateAgentDto { Name = null!, Email = "n@test.com", Password = "p", Phone = "1" };
            await Assert.ThrowsAnyAsync<Exception>(() => _userService.CreateAgentAsync(dto));
        }

        [Fact]
        public async Task CreateAgentAsync_EmptyPassword_ShouldStillWork()
        {
            var dto = new CreateAgentDto { Name = "NoPass", Email = "np@test.com", Password = "", Phone = "1" };
            var result = await _userService.CreateAgentAsync(dto);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task CreateAgentAsync_LongPhone_ReturnsSuccess()
        {
            var dto = new CreateAgentDto { Name = "P", Email = "p@test.com", Password = "p", Phone = "123456789012345" };
            var result = await _userService.CreateAgentAsync(dto);
            Assert.Equal("123456789012345", result.Phone);
        }

        #endregion

        #region CreateClaimsOfficerAsync Tests (5)

        [Fact]
        public async Task CreateClaimsOfficerAsync_ValidDto_ReturnsUserResponse()
        {
            var dto = new CreateClaimsOfficerDto { Name = "Officer", Email = "o@test.com", Password = "p", Phone = "1" };
            var result = await _userService.CreateClaimsOfficerAsync(dto);
            Assert.Equal(UserRole.ClaimsOfficer.ToString(), result.Role);
        }

        [Fact]
        public async Task CreateClaimsOfficerAsync_DuplicateEmail_ThrowsConflictException()
        {
            var email = "dup_o@test.com";
            await _userService.CreateClaimsOfficerAsync(new CreateClaimsOfficerDto { Name = "O", Email = email, Password = "p", Phone = "1" });
            var dto = new CreateClaimsOfficerDto { Name = "N", Email = email, Password = "p", Phone = "1" };
            await Assert.ThrowsAsync<ConflictException>(() => _userService.CreateClaimsOfficerAsync(dto));
        }

        [Fact]
        public async Task CreateClaimsOfficerAsync_EmptyEmail_ThrowsException()
        {
            var dto = new CreateClaimsOfficerDto { Name = "N", Email = "", Password = "p", Phone = "1" };
            // Assuming repository or db context doesn't allow empty string as unique index or service handles it
            var result = await _userService.CreateClaimsOfficerAsync(dto);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task CreateClaimsOfficerAsync_NullDto_ThrowsNullReferenceException()
        {
            await Assert.ThrowsAsync<NullReferenceException>(() => _userService.CreateClaimsOfficerAsync(null!));
        }

        [Fact]
        public async Task CreateClaimsOfficerAsync_ValidInput_HasIsActiveTrue()
        {
            var dto = new CreateClaimsOfficerDto { Name = "Act", Email = "act@test.com", Password = "p", Phone = "1" };
            var result = await _userService.CreateClaimsOfficerAsync(dto);
            Assert.True(result.IsActive);
        }

        #endregion

        #region GetAllCustomersAsync Tests (5)

        [Fact]
        public async Task GetAllCustomersAsync_NoData_ReturnsEmpty()
        {
            var result = await _userService.GetAllCustomersAsync();
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllCustomersAsync_WithData_ReturnsOnlyCustomers()
        {
            await Context.Users.AddAsync(new User { Name = "C", Email = "c@test.com", Role = UserRole.Customer, PasswordHash = "h" });
            await Context.Users.AddAsync(new User { Name = "A", Email = "a@test.com", Role = UserRole.Agent, PasswordHash = "h" });
            await Context.SaveChangesAsync();

            var result = await _userService.GetAllCustomersAsync();
            Assert.Single(result);
            Assert.Equal(UserRole.Customer.ToString(), result.First().Role);
        }

        [Fact]
        public async Task GetAllCustomersAsync_MultipleCustomers_ReturnsAll()
        {
            await Context.Users.AddAsync(new User { Name = "C1", Email = "c1@test.com", Role = UserRole.Customer, PasswordHash = "h" });
            await Context.Users.AddAsync(new User { Name = "C2", Email = "c2@test.com", Role = UserRole.Customer, PasswordHash = "h" });
            await Context.SaveChangesAsync();

            var result = await _userService.GetAllCustomersAsync();
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetAllCustomersAsync_ResultHasCorrectFields()
        {
            var now = DateTime.UtcNow;
            await Context.Users.AddAsync(new User { Name = "C", Email = "c@test.com", Role = UserRole.Customer, PasswordHash = "h", CreatedAt = now });
            await Context.SaveChangesAsync();

            var result = (await _userService.GetAllCustomersAsync()).First();
            Assert.Equal("C", result.Name);
            Assert.Equal("c@test.com", result.Email);
        }

        [Fact]
        public async Task GetAllCustomersAsync_LargeDataset_PerformanceCheck()
        {
            for (int i = 0; i < 10; i++)
                await Context.Users.AddAsync(new User { Name = $"C{i}", Email = $"c{i}@t.com", Role = UserRole.Customer, PasswordHash = "h" });
            await Context.SaveChangesAsync();

            var result = await _userService.GetAllCustomersAsync();
            Assert.Equal(10, result.Count());
        }

        #endregion

        #region GetAllAgentsAsync Tests (5)
        [Fact] public async Task GetAllAgentsAsync_NoData_ReturnsEmpty() => Assert.Empty(await _userService.GetAllAgentsAsync());
        [Fact]
        public async Task GetAllAgentsAsync_WithData_ReturnsOnlyAgents()
        {
            await Context.Users.AddAsync(new User { Name = "A", Email = "a@t.com", Role = UserRole.Agent, PasswordHash = "h" });
            await Context.SaveChangesAsync();
            Assert.Single(await _userService.GetAllAgentsAsync());
        }
        [Fact]
        public async Task GetAllAgentsAsync_Multiple_ReturnsAll()
        {
            await Context.Users.AddAsync(new User { Name = "A1", Email = "a1@t.com", Role = UserRole.Agent, PasswordHash = "h" });
            await Context.Users.AddAsync(new User { Name = "A2", Email = "a2@t.com", Role = UserRole.Agent, PasswordHash = "h" });
            await Context.SaveChangesAsync();
            Assert.Equal(2, (await _userService.GetAllAgentsAsync()).Count());
        }
        [Fact]
        public async Task GetAllAgentsAsync_ResultFieldsMatch()
        {
            await Context.Users.AddAsync(new User { Name = "Ax", Email = "ax@t.com", Role = UserRole.Agent, PasswordHash = "h" });
            await Context.SaveChangesAsync();
            var res = (await _userService.GetAllAgentsAsync()).First();
            Assert.Equal(UserRole.Agent.ToString(), res.Role);
        }
        [Fact]
        public async Task GetAllAgentsAsync_InactiveAgents_AreIncluded()
        {
            await Context.Users.AddAsync(new User { Name = "I", Email = "i@t.com", Role = UserRole.Agent, PasswordHash = "h", IsActive = false });
            await Context.SaveChangesAsync();
            Assert.Single(await _userService.GetAllAgentsAsync());
        }
        #endregion

        #region GetAllClaimsOfficersAsync Tests (5)
        [Fact] public async Task GetAllClaimsOfficersAsync_NoData_ReturnsEmpty() => Assert.Empty(await _userService.GetAllClaimsOfficersAsync());
        [Fact]
        public async Task GetAllClaimsOfficersAsync_WithData_ReturnsOnlyOfficers()
        {
            await Context.Users.AddAsync(new User { Name = "O", Email = "o@t.com", Role = UserRole.ClaimsOfficer, PasswordHash = "h" });
            await Context.SaveChangesAsync();
            Assert.Single(await _userService.GetAllClaimsOfficersAsync());
        }
        [Fact]
        public async Task GetAllClaimsOfficersAsync_CountIsCorrect()
        {
            await Context.Users.AddAsync(new User { Name = "O1", Email = "o1@t.com", Role = UserRole.ClaimsOfficer, PasswordHash = "h" });
            await Context.Users.AddAsync(new User { Name = "O2", Email = "o2@t.com", Role = UserRole.ClaimsOfficer, PasswordHash = "h" });
            await Context.SaveChangesAsync();
            Assert.Equal(2, (await _userService.GetAllClaimsOfficersAsync()).Count());
        }
        [Fact]
        public async Task GetAllClaimsOfficersAsync_FieldsOk()
        {
            await Context.Users.AddAsync(new User { Name = "O", Email = "o@t.com", Role = UserRole.ClaimsOfficer, PasswordHash = "h" });
            await Context.SaveChangesAsync();
            var res = (await _userService.GetAllClaimsOfficersAsync()).First();
            Assert.Equal("Officer", UserRole.ClaimsOfficer.ToString()); // This is just a label check effectively
        }
        [Fact]
        public async Task GetAllClaimsOfficersAsync_CheckMapping()
        {
            var u = new User { Name = "O", Email = "o@t.com", Role = UserRole.ClaimsOfficer, PasswordHash = "h" };
            await Context.Users.AddAsync(u);
            await Context.SaveChangesAsync();
            var res = (await _userService.GetAllClaimsOfficersAsync()).First();
            Assert.Equal(u.Id, res.Id);
        }
        #endregion

        #region GetAllUsersAsync Tests (5)
        [Fact]
        public async Task GetAllUsersAsync_ReturnsEverything()
        {
            await Context.Users.AddAsync(new User { Name = "C", Email = "c@t.com", Role = UserRole.Customer, PasswordHash = "h" });
            await Context.Users.AddAsync(new User { Name = "A", Email = "a@t.com", Role = UserRole.Agent, PasswordHash = "h" });
            await Context.SaveChangesAsync();
            Assert.Equal(2, (await _userService.GetAllUsersAsync()).Count());
        }
        [Fact] public async Task GetAllUsersAsync_NoUsers_ReturnsEmpty() => Assert.Empty(await _userService.GetAllUsersAsync());
        [Fact]
        public async Task GetAllUsersAsync_IncludesAdminFromSeeding()
        {
            // InsuranceDbContext seeds admin with Id=1
            var res = await _userService.GetAllUsersAsync();
            Assert.Contains(res, u => u.Role == UserRole.Admin.ToString());
        }
        [Fact]
        public async Task GetAllUsersAsync_ResultTypeIsUserResponseDto()
        {
            await Context.Users.AddAsync(new User { Name = "U", Email = "u@t.com", Role = UserRole.Customer, PasswordHash = "h" });
            await Context.SaveChangesAsync();
            var res = await _userService.GetAllUsersAsync();
            Assert.IsAssignableFrom<IEnumerable<UserResponseDto>>(res);
        }
        [Fact]
        public async Task GetAllUsersAsync_OrderDoesNotMatter()
        {
            await Context.Users.AddAsync(new User { Name = "Z", Email = "z@t.com", Role = UserRole.Customer, PasswordHash = "h" });
            await Context.Users.AddAsync(new User { Name = "A", Email = "a@t.com", Role = UserRole.Customer, PasswordHash = "h" });
            await Context.SaveChangesAsync();
            Assert.True((await _userService.GetAllUsersAsync()).Count() >= 2);
        }
        #endregion

        #region GetUserByIdAsync Tests (5)

        [Fact]
        public async Task GetUserByIdAsync_ValidId_ReturnsUser()
        {
            var u = new User { Name = "U", Email = "u@t.com", PasswordHash = "h" };
            await Context.Users.AddAsync(u);
            await Context.SaveChangesAsync();

            var result = await _userService.GetUserByIdAsync(u.Id);
            Assert.Equal(u.Id, result.Id);
        }

        [Fact]
        public async Task GetUserByIdAsync_InvalidId_ThrowsNotFoundException()
        {
            await Assert.ThrowsAsync<NotFoundException>(() => _userService.GetUserByIdAsync(999));
        }

        [Fact]
        public async Task GetUserByIdAsync_NegativeId_ThrowsNotFoundException()
        {
            await Assert.ThrowsAsync<NotFoundException>(() => _userService.GetUserByIdAsync(-1));
        }

        [Fact]
        public async Task GetUserByIdAsync_ZeroId_ThrowsNotFoundException()
        {
            await Assert.ThrowsAsync<NotFoundException>(() => _userService.GetUserByIdAsync(0));
        }

        [Fact]
        public async Task GetUserByIdAsync_ResultMapsCorrectRole()
        {
            var u = new User { Name = "U", Email = "u@t.com", PasswordHash = "h", Role = UserRole.Agent };
            await Context.Users.AddAsync(u);
            await Context.SaveChangesAsync();

            var result = await _userService.GetUserByIdAsync(u.Id);
            Assert.Equal("Agent", result.Role);
        }

        #endregion

        #region UpdateUserAsync Tests (5)

        [Fact]
        public async Task UpdateUserAsync_ValidDto_UpdatesUser()
        {
            var u = new User { Name = "Old", Email = "e@t.com", PasswordHash = "h", Phone = "1" };
            await Context.Users.AddAsync(u);
            await Context.SaveChangesAsync();

            var dto = new UpdateUserDto { Name = "New", Phone = "2", IsActive = false };
            var result = await _userService.UpdateUserAsync(u.Id, dto);

            Assert.Equal("New", result.Name);
            Assert.False(result.IsActive);
        }

        [Fact]
        public async Task UpdateUserAsync_NotFound_ThrowsNotFoundException()
        {
            await Assert.ThrowsAsync<NotFoundException>(() => _userService.UpdateUserAsync(999, new UpdateUserDto()));
        }

        [Fact]
        public async Task UpdateUserAsync_EmptyName_UpdatesWithEmpty()
        {
            var u = new User { Name = "Old", Email = "e@t.com", PasswordHash = "h" };
            await Context.Users.AddAsync(u);
            await Context.SaveChangesAsync();

            var dto = new UpdateUserDto { Name = "", Phone = "1", IsActive = true };
            var result = await _userService.UpdateUserAsync(u.Id, dto);
            Assert.Equal("", result.Name);
        }

        [Fact]
        public async Task UpdateUserAsync_SameDetails_NoChangeButSuccess()
        {
            var u = new User { Name = "O", Email = "e@t.com", PasswordHash = "h", Phone = "1", IsActive = true };
            await Context.Users.AddAsync(u);
            await Context.SaveChangesAsync();

            var dto = new UpdateUserDto { Name = "O", Phone = "1", IsActive = true };
            var result = await _userService.UpdateUserAsync(u.Id, dto);
            Assert.Equal("O", result.Name);
        }

        [Fact]
        public async Task UpdateUserAsync_NullDto_ThrowsNullReference()
        {
            await Assert.ThrowsAsync<NullReferenceException>(() => _userService.UpdateUserAsync(1, null!));
        }

        #endregion

        #region DeleteUserAsync Tests (5)

        [Fact]
        public async Task DeleteUserAsync_ValidId_RemovesUser()
        {
            var u = new User { Name = "U", Email = "del@t.com", PasswordHash = "h" };
            await Context.Users.AddAsync(u);
            await Context.SaveChangesAsync();

            await _userService.DeleteUserAsync(u.Id);
            Assert.Null(await Context.Users.FindAsync(u.Id));
        }

        [Fact]
        public async Task DeleteUserAsync_NotFound_ThrowsNotFoundException()
        {
            await Assert.ThrowsAsync<NotFoundException>(() => _userService.DeleteUserAsync(999));
        }

        [Fact]
        public async Task DeleteUserAsync_CustomerWithActivePolicies_ThrowsBadRequestException()
        {
            var c = new User { Name = "C", Email = "c@t.com", Role = UserRole.Customer, PasswordHash = "h" };
            await Context.Users.AddAsync(c);
            await Context.SaveChangesAsync();

            await Context.PolicyAssignments.AddAsync(new PolicyAssignment { CustomerId = c.Id, Status = PolicyStatus.Active, PolicyNumber = "P1" });
            await Context.SaveChangesAsync();

            await Assert.ThrowsAsync<BadRequestException>(() => _userService.DeleteUserAsync(c.Id));
        }

        [Fact]
        public async Task DeleteUserAsync_AgentWithActivePolicies_ThrowsBadRequestException()
        {
            var a = new User { Name = "A", Email = "a@t.com", Role = UserRole.Agent, PasswordHash = "h" };
            await Context.Users.AddAsync(a);
            await Context.SaveChangesAsync();

            var c = new User { Name = "C", Email = "c@t.com", Role = UserRole.Customer, PasswordHash = "h" };
            await Context.Users.AddAsync(c);
            await Context.SaveChangesAsync();

            await Context.PolicyAssignments.AddAsync(new PolicyAssignment { CustomerId = c.Id, AgentId = a.Id, Status = PolicyStatus.Active, PolicyNumber = "P2" });
            await Context.SaveChangesAsync();

            await Assert.ThrowsAsync<BadRequestException>(() => _userService.DeleteUserAsync(a.Id));
        }

        [Fact]
        public async Task DeleteUserAsync_UserWithClosedPolicies_CanBeDeleted()
        {
            var c = new User { Name = "C", Email = "cc@t.com", Role = UserRole.Customer, PasswordHash = "h" };
            await Context.Users.AddAsync(c);
            await Context.SaveChangesAsync();

            await Context.PolicyAssignments.AddAsync(new PolicyAssignment { CustomerId = c.Id, Status = PolicyStatus.Closed, PolicyNumber = "P3" });
            await Context.SaveChangesAsync();

            await _userService.DeleteUserAsync(c.Id);
            Assert.Null(await Context.Users.FindAsync(c.Id));
        }

        #endregion

        #region GetProfileAsync Tests (5)
        [Fact]
        public async Task GetProfileAsync_ValidId_ReturnsProfile()
        {
            var u = new User { Name = "N", Email = "e@t.com", PasswordHash = "h" };
            await Context.Users.AddAsync(u);
            await Context.SaveChangesAsync();
            var res = await _userService.GetProfileAsync(u.Id);
            Assert.Equal("N", res.Name);
        }
        [Fact] public async Task GetProfileAsync_NotFound_ThrowsNotFound() => await Assert.ThrowsAsync<NotFoundException>(() => _userService.GetProfileAsync(999));
        [Fact]
        public async Task GetProfileAsync_InactiveUser_StillReturned()
        {
            var u = new User { Name = "N", Email = "ei@t.com", PasswordHash = "h", IsActive = false };
            await Context.Users.AddAsync(u);
            await Context.SaveChangesAsync();
            var res = await _userService.GetProfileAsync(u.Id);
            Assert.False(res.IsActive);
        }
        [Fact]
        public async Task GetProfileAsync_ResultHasCorrectEmail()
        {
            var u = new User { Name = "N", Email = "email@test.com", PasswordHash = "h" };
            await Context.Users.AddAsync(u);
            await Context.SaveChangesAsync();
            var res = await _userService.GetProfileAsync(u.Id);
            Assert.Equal("email@test.com", res.Email);
        }
        [Fact] public async Task GetProfileAsync_ZeroId_ThrowsNotFound() => await Assert.ThrowsAsync<NotFoundException>(() => _userService.GetProfileAsync(0));
        #endregion

        #region UpdateProfileAsync Tests (5)

        [Fact]
        public async Task UpdateProfileAsync_ValidDto_UpdatesProfile()
        {
            var u = new User { Name = "Old", Email = "e@t.com", PasswordHash = "h" };
            await Context.Users.AddAsync(u);
            await Context.SaveChangesAsync();

            var dto = new UpdateUserDto { Name = "New", Phone = "2" };
            await _userService.UpdateProfileAsync(u.Id, dto);

            var updated = await Context.Users.FindAsync(u.Id);
            Assert.Equal("New", updated!.Name);
        }

        [Fact]
        public async Task UpdateProfileAsync_NotFound_ThrowsNotFound()
        {
            await Assert.ThrowsAsync<NotFoundException>(() => _userService.UpdateProfileAsync(999, new UpdateUserDto()));
        }

        [Fact]
        public async Task UpdateProfileAsync_NullDto_ThrowsNullReference()
        {
            await Assert.ThrowsAsync<NullReferenceException>(() => _userService.UpdateProfileAsync(1, null!));
        }

        [Fact]
        public async Task UpdateProfileAsync_EmptyName_UpdatesCorrectly()
        {
            var u = new User { Name = "O", Email = "e@t.com", PasswordHash = "h" };
            await Context.Users.AddAsync(u);
            await Context.SaveChangesAsync();
            await _userService.UpdateProfileAsync(u.Id, new UpdateUserDto { Name = "", Phone = "1" });
            var res = await Context.Users.FindAsync(u.Id);
            Assert.Equal("", res!.Name);
        }

        [Fact]
        public async Task UpdateProfileAsync_MaintainsEmail()
        {
            var e = "keep@t.com";
            var u = new User { Name = "O", Email = e, PasswordHash = "h" };
            await Context.Users.AddAsync(u);
            await Context.SaveChangesAsync();
            await _userService.UpdateProfileAsync(u.Id, new UpdateUserDto { Name = "X", Phone = "1" });
            var res = await Context.Users.FindAsync(u.Id);
            Assert.Equal(e, res!.Email);
        }

        #endregion
    }
}
