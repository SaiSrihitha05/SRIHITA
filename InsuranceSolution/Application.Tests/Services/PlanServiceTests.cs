using Application.DTOs;
using Application.Exceptions;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Application.Tests.Services
{
    public class PlanServiceTests
    {
        private (InsuranceDbContext db, PlanService service) BuildTestContextAndService()
        {
            var dbOptions = new DbContextOptionsBuilder<InsuranceDbContext>()
                .UseInMemoryDatabase($"PlanServiceTestDb_{Guid.NewGuid()}")
                .Options;

            var dbContext = new InsuranceDbContext(dbOptions);
            var planRepo = new PlanRepository(dbContext);
            var service = new PlanService(planRepo);

            return (dbContext, service);
        }

        // --- 1. GetAllPlansAsync (5 Tests) ---

        [Fact]
        public async Task GetAllPlansAsync_ShouldReturnAllPlans_WhenDataExists()
        {
            var (db, service) = BuildTestContextAndService();
            db.Plans.Add(new Plan { PlanName = "Plan1", IsActive = true });
            db.Plans.Add(new Plan { PlanName = "Plan2", IsActive = false });
            await db.SaveChangesAsync();

            var plans = await service.GetAllPlansAsync();

            Assert.Equal(2, plans.Count());
        }

        [Fact]
        public async Task GetAllPlansAsync_ShouldReturnEmptyList_WhenNoDataExists()
        {
            var (_, service) = BuildTestContextAndService();

            var plans = await service.GetAllPlansAsync();

            Assert.Empty(plans);
        }

        [Fact]
        public async Task GetAllPlansAsync_ShouldMapEntityToDtoCorrectly()
        {
            var (db, service) = BuildTestContextAndService();
            var plan = new Plan { PlanName = "MapTest", PlanType = PlanCategory.TermLife, Description = "Desc", IsActive = true, CommissionRate = 12 };
            db.Plans.Add(plan);
            await db.SaveChangesAsync();

            var plans = await service.GetAllPlansAsync();

            var dto = plans.First();
            Assert.Equal("MapTest", dto.PlanName);
            Assert.Equal(PlanCategory.TermLife, dto.PlanType);
            Assert.Equal("Desc", dto.Description);
            Assert.True(dto.IsActive);
            Assert.Equal(12, dto.CommissionRate);
        }

        [Fact]
        public async Task GetAllPlansAsync_ShouldReturnExactNumberOfSeededPlans()
        {
            var (db, service) = BuildTestContextAndService();
            for (int i = 0; i < 5; i++) db.Plans.Add(new Plan { PlanName = $"Plan{i}" });
            await db.SaveChangesAsync();

            var plans = await service.GetAllPlansAsync();

            Assert.Equal(5, plans.Count());
        }

        [Fact]
        public async Task GetAllPlansAsync_ShouldHandleLargeSetsOfPlansCorrectly()
        {
            var (db, service) = BuildTestContextAndService();
            for (int i = 0; i < 100; i++) db.Plans.Add(new Plan { PlanName = $"Plan{i}" });
            await db.SaveChangesAsync();

            var plans = await service.GetAllPlansAsync();

            Assert.Equal(100, plans.Count());
        }

        // --- 2. GetActivePlansAsync (5 Tests) ---

        [Fact]
        public async Task GetActivePlansAsync_ShouldReturnOnlyActivePlans_WhenMixedDataExists()
        {
            var (db, service) = BuildTestContextAndService();
            db.Plans.Add(new Plan { PlanName = "Plan1", IsActive = true });
            db.Plans.Add(new Plan { PlanName = "Plan2", IsActive = true });
            db.Plans.Add(new Plan { PlanName = "Plan3", IsActive = false });
            await db.SaveChangesAsync();

            var plans = await service.GetActivePlansAsync();

            Assert.Equal(2, plans.Count());
            Assert.All(plans, p => Assert.True(p.IsActive));
        }

        [Fact]
        public async Task GetActivePlansAsync_ShouldReturnEmptyList_WhenOnlyInactivePlansExist()
        {
            var (db, service) = BuildTestContextAndService();
            db.Plans.Add(new Plan { PlanName = "Plan1", IsActive = false });
            db.Plans.Add(new Plan { PlanName = "Plan2", IsActive = false });
            await db.SaveChangesAsync();

            var plans = await service.GetActivePlansAsync();

            Assert.Empty(plans);
        }

        [Fact]
        public async Task GetActivePlansAsync_ShouldMapEntityToDtoCorrectly()
        {
            var (db, service) = BuildTestContextAndService();
            db.Plans.Add(new Plan { PlanName = "ActivePlan", IsActive = true, Description = "Active" });
            await db.SaveChangesAsync();

            var plans = await service.GetActivePlansAsync();

            var dto = plans.First();
            Assert.Equal("ActivePlan", dto.PlanName);
            Assert.True(dto.IsActive);
        }

        [Fact]
        public async Task GetActivePlansAsync_ShouldReturnEmptyList_WhenNoDataExists()
        {
            var (_, service) = BuildTestContextAndService();

            var plans = await service.GetActivePlansAsync();

            Assert.Empty(plans);
        }

        [Fact]
        public async Task GetActivePlansAsync_ShouldReturnAllPlans_WhenAllAreActive()
        {
            var (db, service) = BuildTestContextAndService();
            for (int i = 0; i < 5; i++) db.Plans.Add(new Plan { PlanName = $"P{i}", IsActive = true });
            await db.SaveChangesAsync();

            var plans = await service.GetActivePlansAsync();

            Assert.Equal(5, plans.Count());
        }

        // --- 3. GetPlanByIdAsync (5 Tests) ---

        [Fact]
        public async Task GetPlanByIdAsync_ShouldReturnPlan_WhenExistsAndRequestedByAdmin()
        {
            var (db, service) = BuildTestContextAndService();
            var plan = new Plan { PlanName = "AdminPlan", IsActive = false };
            db.Plans.Add(plan);
            await db.SaveChangesAsync();

            var result = await service.GetPlanByIdAsync(plan.Id, "Admin");

            Assert.NotNull(result);
            Assert.Equal("AdminPlan", result.PlanName);
        }

        [Fact]
        public async Task GetPlanByIdAsync_ShouldThrowNotFoundException_WhenPlanDoesNotExist()
        {
            var (_, service) = BuildTestContextAndService();

            await Assert.ThrowsAsync<NotFoundException>(() => service.GetPlanByIdAsync(999, "Customer"));
        }

        [Fact]
        public async Task GetPlanByIdAsync_ShouldThrowNotFoundException_WhenPlanIsInactiveAndRequestedByCustomer()
        {
            var (db, service) = BuildTestContextAndService();
            var plan = new Plan { PlanName = "InactivePlan", IsActive = false };
            db.Plans.Add(plan);
            await db.SaveChangesAsync();

            await Assert.ThrowsAsync<NotFoundException>(() => service.GetPlanByIdAsync(plan.Id, "Customer"));
        }

        [Fact]
        public async Task GetPlanByIdAsync_ShouldReturnPlan_WhenPlanIsActiveAndRequestedByCustomer()
        {
            var (db, service) = BuildTestContextAndService();
            var plan = new Plan { PlanName = "ActivePlan", IsActive = true };
            db.Plans.Add(plan);
            await db.SaveChangesAsync();

            var result = await service.GetPlanByIdAsync(plan.Id, "Customer");

            Assert.NotNull(result);
            Assert.Equal("ActivePlan", result.PlanName);
        }

        [Fact]
        public async Task GetPlanByIdAsync_ShouldReturnInactivePlan_WhenRequestedByAgent()
        {
            var (db, service) = BuildTestContextAndService();
            var plan = new Plan { PlanName = "AgentPlan", IsActive = false };
            db.Plans.Add(plan);
            await db.SaveChangesAsync();

            var result = await service.GetPlanByIdAsync(plan.Id, "Agent"); // Agent can view

            Assert.NotNull(result);
            Assert.Equal("AgentPlan", result.PlanName);
            Assert.False(result.IsActive);
        }

        // --- 4. CreatePlanAsync (5 Tests) ---

        [Fact]
        public async Task CreatePlanAsync_ShouldCreateAndReturnPlan_WhenDataIsValid()
        {
            var (_, service) = BuildTestContextAndService();
            var dto = new CreatePlanDto { PlanName = "NewPlan", MinAge = 18, MaxAge = 65, MinCoverageAmount = 1000, MaxCoverageAmount = 5000, MinTermYears = 1, MaxTermYears = 5 };

            var result = await service.CreatePlanAsync(dto);

            Assert.NotNull(result);
            Assert.Equal("NewPlan", result.PlanName);
            Assert.True(result.IsActive);
        }

        [Fact]
        public async Task CreatePlanAsync_ShouldThrowConflictException_WhenPlanNameAlreadyExists()
        {
            var (db, service) = BuildTestContextAndService();
            db.Plans.Add(new Plan { PlanName = "ExistingPlan" });
            await db.SaveChangesAsync();

            var dto = new CreatePlanDto { PlanName = "ExistingPlan", MinAge = 18, MaxAge = 65, MinCoverageAmount = 1000, MaxCoverageAmount = 5000, MinTermYears = 1, MaxTermYears = 5 };

            await Assert.ThrowsAsync<ConflictException>(() => service.CreatePlanAsync(dto));
        }

        [Fact]
        public async Task CreatePlanAsync_ShouldThrowBadRequestException_WhenMinAgeIsGreaterThanOrEqualToMaxAge()
        {
            var (_, service) = BuildTestContextAndService();
            var dto = new CreatePlanDto { PlanName = "InvalidAge", MinAge = 65, MaxAge = 18 };

            var ex = await Assert.ThrowsAsync<BadRequestException>(() => service.CreatePlanAsync(dto));
            Assert.Contains("MinAge", ex.Message);
        }

        [Fact]
        public async Task CreatePlanAsync_ShouldThrowBadRequestException_WhenMinCoverageIsGreaterThanOrEqualToMaxCoverage()
        {
            var (_, service) = BuildTestContextAndService();
            var dto = new CreatePlanDto { PlanName = "InvalidCov", MinAge = 18, MaxAge = 65, MinCoverageAmount = 5000, MaxCoverageAmount = 1000 };

            var ex = await Assert.ThrowsAsync<BadRequestException>(() => service.CreatePlanAsync(dto));
            Assert.Contains("MinCoverageAmount", ex.Message);
        }

        [Fact]
        public async Task CreatePlanAsync_ShouldThrowBadRequestException_WhenMinTermIsGreaterThanOrEqualToMaxTerm()
        {
            var (_, service) = BuildTestContextAndService();
            var dto = new CreatePlanDto { PlanName = "InvalidTerm", MinAge = 18, MaxAge = 65, MinCoverageAmount = 1000, MaxCoverageAmount = 5000, MinTermYears = 5, MaxTermYears = 1 };

            var ex = await Assert.ThrowsAsync<BadRequestException>(() => service.CreatePlanAsync(dto));
            Assert.Contains("MinTermYears", ex.Message);
        }

        // --- 5. UpdatePlanAsync (5 Tests) ---

        [Fact]
        public async Task UpdatePlanAsync_ShouldUpdateAndReturnPlan_WhenDataIsValid()
        {
            var (db, service) = BuildTestContextAndService();
            var plan = new Plan { PlanName = "OldPlan", IsActive = true };
            db.Plans.Add(plan);
            await db.SaveChangesAsync();

            var dto = new UpdatePlanDto { PlanName = "UpdatedPlan", MinAge = 18, MaxAge = 65, MinCoverageAmount = 1000, MaxCoverageAmount = 5000, MinTermYears = 1, MaxTermYears = 5, IsActive = true };

            var result = await service.UpdatePlanAsync(plan.Id, dto);

            Assert.Equal("UpdatedPlan", result.PlanName);
            var updatedDbPlan = await db.Plans.FindAsync(plan.Id);
            Assert.Equal("UpdatedPlan", updatedDbPlan!.PlanName);
        }

        [Fact]
        public async Task UpdatePlanAsync_ShouldThrowNotFoundException_WhenPlanDoesNotExist()
        {
            var (_, service) = BuildTestContextAndService();
            var dto = new UpdatePlanDto { PlanName = "Update", MinAge = 18, MaxAge = 65, MinCoverageAmount = 1000, MaxCoverageAmount = 5000, MinTermYears = 1, MaxTermYears = 5 };

            await Assert.ThrowsAsync<NotFoundException>(() => service.UpdatePlanAsync(999, dto));
        }

        [Fact]
        public async Task UpdatePlanAsync_ShouldThrowBadRequestException_WhenMinAgeIsGreaterThanOrEqualToMaxAgeInUpdate()
        {
            var (db, service) = BuildTestContextAndService();
            var plan = new Plan { PlanName = "OldPlan" };
            db.Plans.Add(plan);
            await db.SaveChangesAsync();

            var dto = new UpdatePlanDto { MinAge = 65, MaxAge = 18 };

            await Assert.ThrowsAsync<BadRequestException>(() => service.UpdatePlanAsync(plan.Id, dto));
        }

        [Fact]
        public async Task UpdatePlanAsync_ShouldThrowBadRequestException_WhenMinCoverageIsGreaterThanOrEqualToMaxCoverageInUpdate()
        {
            var (db, service) = BuildTestContextAndService();
            var plan = new Plan { PlanName = "OldPlan" };
            db.Plans.Add(plan);
            await db.SaveChangesAsync();

            var dto = new UpdatePlanDto { MinAge = 18, MaxAge = 65, MinCoverageAmount = 5000, MaxCoverageAmount = 1000 };

            await Assert.ThrowsAsync<BadRequestException>(() => service.UpdatePlanAsync(plan.Id, dto));
        }

        [Fact]
        public async Task UpdatePlanAsync_ShouldThrowBadRequestException_WhenMinTermIsGreaterThanOrEqualToMaxTermInUpdate()
        {
            var (db, service) = BuildTestContextAndService();
            var plan = new Plan { PlanName = "OldPlan" };
            db.Plans.Add(plan);
            await db.SaveChangesAsync();

            var dto = new UpdatePlanDto { MinAge = 18, MaxAge = 65, MinCoverageAmount = 1000, MaxCoverageAmount = 5000, MinTermYears = 5, MaxTermYears = 1 };

            await Assert.ThrowsAsync<BadRequestException>(() => service.UpdatePlanAsync(plan.Id, dto));
        }

        // --- 6. DeletePlanAsync (5 Tests) ---

        [Fact]
        public async Task DeletePlanAsync_ShouldSoftDeletePlan_WhenPlanExists()
        {
            var (db, service) = BuildTestContextAndService();
            var plan = new Plan { PlanName = "ToDelete", IsActive = true };
            db.Plans.Add(plan);
            await db.SaveChangesAsync();

            await service.DeletePlanAsync(plan.Id);

            var deletedPlan = await db.Plans.FindAsync(plan.Id);
            Assert.False(deletedPlan!.IsActive);
        }

        [Fact]
        public async Task DeletePlanAsync_ShouldThrowNotFoundException_WhenPlanDoesNotExist()
        {
            var (_, service) = BuildTestContextAndService();

            await Assert.ThrowsAsync<NotFoundException>(() => service.DeletePlanAsync(999));
        }

        [Fact]
        public async Task DeletePlanAsync_ShouldNotAffectOtherPlans_WhenOneIsDeleted()
        {
            var (db, service) = BuildTestContextAndService();
            var p1 = new Plan { PlanName = "P1", IsActive = true };
            var p2 = new Plan { PlanName = "P2", IsActive = true };
            db.Plans.AddRange(p1, p2);
            await db.SaveChangesAsync();

            await service.DeletePlanAsync(p1.Id);

            var checkP2 = await db.Plans.FindAsync(p2.Id);
            Assert.True(checkP2!.IsActive);
        }

        [Fact]
        public async Task DeletePlanAsync_ShouldRemainInactiveWithoutError_WhenCalledOnAlreadyInactivePlan()
        {
            var (db, service) = BuildTestContextAndService();
            var plan = new Plan { PlanName = "AlreadyDead", IsActive = false };
            db.Plans.Add(plan);
            await db.SaveChangesAsync();

            var ex = await Record.ExceptionAsync(() => service.DeletePlanAsync(plan.Id));

            Assert.Null(ex);
            var dbPlan = await db.Plans.FindAsync(plan.Id);
            Assert.False(dbPlan!.IsActive);
        }

        [Fact]
        public async Task DeletePlanAsync_ShouldThrowNotFoundException_ForNegativeInvalidIds()
        {
            var (_, service) = BuildTestContextAndService();

            await Assert.ThrowsAsync<NotFoundException>(() => service.DeletePlanAsync(-1));
        }

        // --- 7. GetFilteredPlansAsync (5 Tests) ---

        [Fact]
        public async Task GetFilteredPlansAsync_ShouldReturnMatchingPlans_ForAdmin()
        {
            var (db, service) = BuildTestContextAndService();
            db.Plans.Add(new Plan { PlanName = "PlanA", PlanType = PlanCategory.TermLife, IsActive = false });
            db.Plans.Add(new Plan { PlanName = "PlanB", PlanType = PlanCategory.TermLife, IsActive = true });
            db.Plans.Add(new Plan { PlanName = "PlanC", PlanType = PlanCategory.Endowment, IsActive = true });
            await db.SaveChangesAsync();

            var filter = new PlanFilterDto { PlanType = PlanCategory.TermLife };
            var plans = await service.GetFilteredPlansAsync(filter, "Admin");

            Assert.Equal(2, plans.Count());
        }

        [Fact]
        public async Task GetFilteredPlansAsync_ShouldReturnOnlyActiveMatchingPlans_ForCustomer()
        {
            var (db, service) = BuildTestContextAndService();
            db.Plans.Add(new Plan { PlanName = "PlanA", PlanType = PlanCategory.TermLife, IsActive = false });
            db.Plans.Add(new Plan { PlanName = "PlanB", PlanType = PlanCategory.TermLife, IsActive = true });
            await db.SaveChangesAsync();

            var filter = new PlanFilterDto { PlanType = PlanCategory.TermLife };
            var plans = await service.GetFilteredPlansAsync(filter, "Customer");

            Assert.Single(plans);
            Assert.Equal("PlanB", plans.First().PlanName);
        }

        [Fact]
        public async Task GetFilteredPlansAsync_ShouldThrowNotFoundException_WhenNoPlansMatchForAdmin()
        {
            var (db, service) = BuildTestContextAndService();
            db.Plans.Add(new Plan { PlanName = "PlanA", PlanType = PlanCategory.TermLife });
            await db.SaveChangesAsync();

            var filter = new PlanFilterDto { PlanType = PlanCategory.Savings };

            await Assert.ThrowsAsync<NotFoundException>(() => service.GetFilteredPlansAsync(filter, "Admin"));
        }

        [Fact]
        public async Task GetFilteredPlansAsync_ShouldThrowNotFoundException_WhenPlansMatchButAreInactiveForCustomer()
        {
            var (db, service) = BuildTestContextAndService();
            db.Plans.Add(new Plan { PlanName = "PlanA", PlanType = PlanCategory.TermLife, IsActive = false });
            await db.SaveChangesAsync();

            var filter = new PlanFilterDto { PlanType = PlanCategory.TermLife };

            // Throws because only inactive plans exist for this filter, and customer needs active ones
            await Assert.ThrowsAsync<NotFoundException>(() => service.GetFilteredPlansAsync(filter, "Customer"));
        }

        [Fact]
        public async Task GetFilteredPlansAsync_ShouldReturnAllActivePlans_WhenFilterIsEmptyForCustomer()
        {
            var (db, service) = BuildTestContextAndService();
            db.Plans.Add(new Plan { PlanName = "P1", IsActive = true });
            db.Plans.Add(new Plan { PlanName = "P2", IsActive = true });
            db.Plans.Add(new Plan { PlanName = "P3", IsActive = false });
            await db.SaveChangesAsync();

            var filter = new PlanFilterDto(); // Empty filter
            var plans = await service.GetFilteredPlansAsync(filter, "Customer");

            Assert.Equal(2, plans.Count());
        }
    }
}
