using Application.DTOs;
using Application.Exceptions;
using Application.Interfaces.Repositories;
using Application.Services;
using Application.Tests.Common;
using Domain.Entities;
using Infrastructure.Repositories;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Application.Tests.Services
{
    public class PlanServiceTests : ApplicationTestBase
    {
        private readonly PlanService _planService;
        private readonly IPlanRepository _planRepository;

        public PlanServiceTests()
        {
            _planRepository = new PlanRepository(Context);
            _planService = new PlanService(_planRepository);
        }

        #region GetAllPlansAsync Tests (5)
        [Fact] public async Task GetAllPlansAsync_NoPlans_ReturnsEmpty() => Assert.Empty(await _planService.GetAllPlansAsync());
        [Fact]
        public async Task GetAllPlansAsync_WithPlans_ReturnsAll()
        {
            await _planRepository.AddAsync(new Plan { PlanName = "P1" }); await _planRepository.SaveChangesAsync();
            Assert.Single(await _planService.GetAllPlansAsync());
        }
        [Fact]
        public async Task GetAllPlansAsync_CountIsCorrect()
        {
            await _planRepository.AddAsync(new Plan { PlanName = "P1" });
            await _planRepository.AddAsync(new Plan { PlanName = "P2" }); await _planRepository.SaveChangesAsync();
            Assert.Equal(2, (await _planService.GetAllPlansAsync()).Count());
        }
        [Fact]
        public async Task GetAllPlansAsync_InactiveIncluded()
        {
            await _planRepository.AddAsync(new Plan { PlanName = "P1", IsActive = false }); await _planRepository.SaveChangesAsync();
            Assert.Single(await _planService.GetAllPlansAsync());
        }
        [Fact]
        public async Task GetAllPlansAsync_MappingCorrect()
        {
            await _planRepository.AddAsync(new Plan { PlanName = "Gold" }); await _planRepository.SaveChangesAsync();
            var res = await _planService.GetAllPlansAsync();
            Assert.Equal("Gold", res.First().PlanName);
        }
        #endregion

        #region GetActivePlansAsync Tests (5)
        [Fact] public async Task GetActivePlansAsync_NoPlans_ReturnsEmpty() => Assert.Empty(await _planService.GetActivePlansAsync());
        [Fact]
        public async Task GetActivePlansAsync_WithActive_ReturnsOnlyActive()
        {
            await _planRepository.AddAsync(new Plan { PlanName = "A", IsActive = true });
            await _planRepository.AddAsync(new Plan { PlanName = "I", IsActive = false });
            await _planRepository.SaveChangesAsync();
            var res = await _planService.GetActivePlansAsync();
            Assert.Single(res);
            Assert.True(res.First().IsActive);
        }
        [Fact]
        public async Task GetActivePlansAsync_MultipleActive_ReturnsAll()
        {
            await _planRepository.AddAsync(new Plan { PlanName = "A1", IsActive = true });
            await _planRepository.AddAsync(new Plan { PlanName = "A2", IsActive = true });
            await _planRepository.SaveChangesAsync();
            Assert.Equal(2, (await _planService.GetActivePlansAsync()).Count());
        }
        [Fact]
        public async Task GetActivePlansAsync_MappingCheck()
        {
            await _planRepository.AddAsync(new Plan { PlanName = "Act", IsActive = true });
            await _planRepository.SaveChangesAsync();
            var res = await _planService.GetActivePlansAsync();
            Assert.Equal("Act", res.First().PlanName);
        }
        [Fact] public async Task GetActivePlansAsync_ResultTypeCorrect() => Assert.IsAssignableFrom<IEnumerable<PlanResponseDto>>(await _planService.GetActivePlansAsync());
        #endregion

        #region GetPlanByIdAsync Tests (5)
        [Fact]
        public async Task GetPlanByIdAsync_ValidId_ReturnsPlan()
        {
            var p = new Plan { PlanName = "P" };
            await _planRepository.AddAsync(p); await _planRepository.SaveChangesAsync();
            var res = await _planService.GetPlanByIdAsync(p.Id, "Admin");
            Assert.Equal(p.Id, res.Id);
        }
        [Fact] public async Task GetPlanByIdAsync_NotFound_ThrowsNotFound() => await Assert.ThrowsAsync<NotFoundException>(() => _planService.GetPlanByIdAsync(999, "Admin"));
        [Fact]
        public async Task GetPlanByIdAsync_CustomerAccessingInactive_ThrowsNotFound()
        {
            var p = new Plan { PlanName = "I", IsActive = false };
            await _planRepository.AddAsync(p); await _planRepository.SaveChangesAsync();
            await Assert.ThrowsAsync<NotFoundException>(() => _planService.GetPlanByIdAsync(p.Id, "Customer"));
        }
        [Fact]
        public async Task GetPlanByIdAsync_AdminAccessingInactive_ReturnsPlan()
        {
            var p = new Plan { PlanName = "I", IsActive = false };
            await _planRepository.AddAsync(p); await _planRepository.SaveChangesAsync();
            var res = await _planService.GetPlanByIdAsync(p.Id, "Admin");
            Assert.False(res.IsActive);
        }
        [Fact] public async Task GetPlanByIdAsync_ZeroId_ThrowsNotFound() => await Assert.ThrowsAsync<NotFoundException>(() => _planService.GetPlanByIdAsync(0, "Admin"));
        #endregion

        #region CreatePlanAsync Tests (5)
        [Fact]
        public async Task CreatePlanAsync_ValidDto_ReturnsResponse()
        {
            var dto = new CreatePlanDto { PlanName = "New", MinAge = 18, MaxAge = 60, MinCoverageAmount = 1, MaxCoverageAmount = 10, MinTermYears = 1, MaxTermYears = 10 };
            var res = await _planService.CreatePlanAsync(dto);
            Assert.Equal("New", res.PlanName);
            Assert.True(res.IsActive);
        }
        [Fact]
        public async Task CreatePlanAsync_DuplicateName_ThrowsConflict()
        {
            await _planRepository.AddAsync(new Plan { PlanName = "Dup" }); await _planRepository.SaveChangesAsync();
            var dto = new CreatePlanDto { PlanName = "Dup" };
            await Assert.ThrowsAsync<ConflictException>(() => _planService.CreatePlanAsync(dto));
        }
        [Fact]
        public async Task CreatePlanAsync_InvalidAgeRange_ThrowsBadRequest()
        {
            var dto = new CreatePlanDto { PlanName = "A", MinAge = 60, MaxAge = 18 };
            await Assert.ThrowsAsync<BadRequestException>(() => _planService.CreatePlanAsync(dto));
        }
        [Fact]
        public async Task CreatePlanAsync_InvalidCoverageRange_ThrowsBadRequest()
        {
            var dto = new CreatePlanDto { PlanName = "C", MinCoverageAmount = 100, MaxCoverageAmount = 50 };
            await Assert.ThrowsAsync<BadRequestException>(() => _planService.CreatePlanAsync(dto));
        }
        [Fact]
        public async Task CreatePlanAsync_InvalidTermRange_ThrowsBadRequest()
        {
            var dto = new CreatePlanDto { PlanName = "T", MinTermYears = 10, MaxTermYears = 5 };
            await Assert.ThrowsAsync<BadRequestException>(() => _planService.CreatePlanAsync(dto));
        }
        #endregion

        #region UpdatePlanAsync Tests (5)
        [Fact]
        public async Task UpdatePlanAsync_ValidDto_UpdatesPlan()
        {
            var p = new Plan { PlanName = "Old", MinAge = 10, MaxAge = 20, MinCoverageAmount = 1, MaxCoverageAmount = 2, MinTermYears = 1, MaxTermYears = 2 };
            await _planRepository.AddAsync(p); await _planRepository.SaveChangesAsync();

            var dto = new UpdatePlanDto { PlanName = "New", MinAge = 18, MaxAge = 60, MinCoverageAmount = 100, MaxCoverageAmount = 200, MinTermYears = 5, MaxTermYears = 10, IsActive = true };
            var res = await _planService.UpdatePlanAsync(p.Id, dto);
            Assert.Equal("New", res.PlanName);
        }
        [Fact] public async Task UpdatePlanAsync_NotFound_ThrowsNotFound() => await Assert.ThrowsAsync<NotFoundException>(() => _planService.UpdatePlanAsync(999, new UpdatePlanDto()));
        [Fact]
        public async Task UpdatePlanAsync_InvalidAgeRange_ThrowsBadRequest()
        {
            var p = new Plan { PlanName = "P" }; await _planRepository.AddAsync(p); await _planRepository.SaveChangesAsync();
            var dto = new UpdatePlanDto { MinAge = 50, MaxAge = 20 };
            await Assert.ThrowsAsync<BadRequestException>(() => _planService.UpdatePlanAsync(p.Id, dto));
        }
        [Fact]
        public async Task UpdatePlanAsync_TogglingActiveStatus_Works()
        {
            var p = new Plan { PlanName = "P", IsActive = true, MinAge = 1, MaxAge = 2, MinCoverageAmount = 1, MaxCoverageAmount = 2, MinTermYears = 1, MaxTermYears = 2 };
            await _planRepository.AddAsync(p); await _planRepository.SaveChangesAsync();
            var dto = new UpdatePlanDto { PlanName = "P", IsActive = false, MinAge = 1, MaxAge = 2, MinCoverageAmount = 1, MaxCoverageAmount = 2, MinTermYears = 1, MaxTermYears = 2 };
            var res = await _planService.UpdatePlanAsync(p.Id, dto);
            Assert.False(res.IsActive);
        }
        [Fact]
        public async Task UpdatePlanAsync_SameName_Success()
        {
            var p = new Plan { PlanName = "P", MinAge = 1, MaxAge = 10, MinCoverageAmount = 1, MaxCoverageAmount = 10, MinTermYears = 1, MaxTermYears = 10 };
            await _planRepository.AddAsync(p); await _planRepository.SaveChangesAsync();
            var dto = new UpdatePlanDto { PlanName = "P", MinAge = 1, MaxAge = 10, MinCoverageAmount = 1, MaxCoverageAmount = 10, MinTermYears = 1, MaxTermYears = 10 };
            var res = await _planService.UpdatePlanAsync(p.Id, dto);
            Assert.Equal("P", res.PlanName);
        }
        #endregion

        #region DeletePlanAsync Tests (5)
        [Fact]
        public async Task DeletePlanAsync_ValidId_DeactivatesPlan()
        {
            var p = new Plan { PlanName = "P", IsActive = true };
            await _planRepository.AddAsync(p); await _planRepository.SaveChangesAsync();
            await _planService.DeletePlanAsync(p.Id);
            var updated = await _planRepository.GetByIdAsync(p.Id);
            Assert.False(updated!.IsActive);
        }
        [Fact] public async Task DeletePlanAsync_NotFound_ThrowsNotFound() => await Assert.ThrowsAsync<NotFoundException>(() => _planService.DeletePlanAsync(999));
        [Fact]
        public async Task DeletePlanAsync_AlreadyInactive_RemainsInactive()
        {
            var p = new Plan { PlanName = "P", IsActive = false };
            await _planRepository.AddAsync(p); await _planRepository.SaveChangesAsync();
            await _planService.DeletePlanAsync(p.Id);
            Assert.False((await _planRepository.GetByIdAsync(p.Id))!.IsActive);
        }
        [Fact] public async Task DeletePlanAsync_ZeroId_ThrowsNotFound() => await Assert.ThrowsAsync<NotFoundException>(() => _planService.DeletePlanAsync(0));
        [Fact]
        public async Task DeletePlanAsync_SavesChanges()
        {
            var p = new Plan { PlanName = "P" }; await _planRepository.AddAsync(p); await _planRepository.SaveChangesAsync();
            await _planService.DeletePlanAsync(p.Id);
            // Ensure it's persisted in DB
            var contextP = await Context.Plans.FindAsync(p.Id);
            Assert.False(contextP!.IsActive);
        }
        #endregion

        #region GetFilteredPlansAsync Tests (5)
        [Fact]
        public async Task GetFilteredPlansAsync_CustomerRole_ExcludesInactive()
        {
            await _planRepository.AddAsync(new Plan { PlanName = "A", IsActive = true });
            await _planRepository.AddAsync(new Plan { PlanName = "I", IsActive = false });
            await _planRepository.SaveChangesAsync();

            var res = await _planService.GetFilteredPlansAsync(new PlanFilterDto(), "Customer");
            Assert.Single(res);
            Assert.Equal("A", res.First().PlanName);
        }
        [Fact]
        public async Task GetFilteredPlansAsync_NoMatches_ThrowsNotFound()
        {
            await Assert.ThrowsAsync<NotFoundException>(() => _planService.GetFilteredPlansAsync(new PlanFilterDto(), "Admin"));
        }
        [Fact]
        public async Task GetFilteredPlansAsync_AdminRole_IncludesInactive()
        {
            await _planRepository.AddAsync(new Plan { PlanName = "I", IsActive = false });
            await _planRepository.SaveChangesAsync();
            var res = await _planService.GetFilteredPlansAsync(new PlanFilterDto(), "Admin");
            Assert.Single(res);
        }
        [Fact]
        public async Task GetFilteredPlansAsync_FilterByType_Works()
        {
            await _planRepository.AddAsync(new Plan { PlanName = "Life", PlanType = "Life", IsActive = true });
            await _planRepository.AddAsync(new Plan { PlanName = "Health", PlanType = "Health", IsActive = true });
            await _planRepository.SaveChangesAsync();
            var res = await _planService.GetFilteredPlansAsync(new PlanFilterDto { PlanType = "Life" }, "Admin");
            Assert.Single(res);
            Assert.Equal("Life", res.First().PlanType);
        }
        [Fact]
        public async Task GetFilteredPlansAsync_MultipleFilters_Works()
        {
            await _planRepository.AddAsync(new Plan { PlanName = "Gold", PlanType = "Life", IsActive = true, BaseRate = 100 });
            await _planRepository.SaveChangesAsync();
            var res = await _planService.GetFilteredPlansAsync(new PlanFilterDto { PlanType = "Life", MinPrice = 50, MaxPrice = 150 }, "Admin");
            Assert.Single(res);
        }
        #endregion
    }
}
