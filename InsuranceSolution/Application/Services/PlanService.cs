using Application.DTOs;
using Application.Exceptions;
using Application.Interfaces.Repositories;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class PlanService : IPlanService
    {
        private readonly IPlanRepository _planRepository;

        public PlanService(IPlanRepository planRepository)
        {
            _planRepository = planRepository;
        }

        public async Task<IEnumerable<PlanResponseDto>> GetAllPlansAsync()
        {
            var plans = await _planRepository.GetAllAsync();
            return plans.Select(MapToDto);
        }

        public async Task<IEnumerable<PlanResponseDto>> GetActivePlansAsync()
        {
            var plans = await _planRepository.GetAllActiveAsync();
            return plans.Select(MapToDto);
        }

        public async Task<PlanResponseDto> GetPlanByIdAsync(int id, string role)
        {
            var plan = await _planRepository.GetByIdAsync(id);
            if (plan == null)
                throw new NotFoundException("Plan", id);
            if (role == "Customer" && !plan.IsActive)
                throw new NotFoundException("Plan", id);
            return MapToDto(plan);
        }

        public async Task<PlanResponseDto> CreatePlanAsync(CreatePlanDto dto)
        {
            if (await _planRepository.ExistsByNameAsync(dto.PlanName))
                throw new ConflictException(
                    $"A plan with name '{dto.PlanName}' already exists");

            if (dto.MinAge >= dto.MaxAge)
                throw new BadRequestException(
                    "MinAge must be less than MaxAge");

            if (dto.MinCoverageAmount >= dto.MaxCoverageAmount)
                throw new BadRequestException(
                    "MinCoverageAmount must be less than MaxCoverageAmount");

            if (dto.MinTermYears >= dto.MaxTermYears)
                throw new BadRequestException(
                    "MinTermYears must be less than MaxTermYears");


            var plan = new Plan
            {
                PlanName = dto.PlanName,
                PlanType = dto.PlanType,
                Description = dto.Description,
                BaseRate = dto.BaseRate,
                MinAge = dto.MinAge,
                MaxAge = dto.MaxAge,
                MinCoverageAmount = dto.MinCoverageAmount,
                MaxCoverageAmount = dto.MaxCoverageAmount,
                MinTermYears = dto.MinTermYears,
                MaxTermYears = dto.MaxTermYears,
                MinNominees = dto.MinNominees,
                MaxNominees = dto.MaxNominees,
                GracePeriodDays = dto.GracePeriodDays,
                HasMaturityBenefit = dto.HasMaturityBenefit,
                IsReturnOfPremium = dto.IsReturnOfPremium,
                MaxPolicyMembersAllowed = dto.MaxPolicyMembersAllowed,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                CommissionRate = dto.CommissionRate,
                HasDeathBenefit = dto.HasDeathBenefit,
                HasBonus = dto.HasBonus,
                HasLoanFacility = dto.HasLoanFacility,
                CoverageIncreasing = dto.CoverageIncreasing,
                CoverageIncreaseRate = dto.CoverageIncreaseRate,
                CoverageUntilAge = dto.CoverageUntilAge,
                LoanEligibleAfterYears = dto.LoanEligibleAfterYears,
                MaxLoanPercentage = dto.MaxLoanPercentage,
                LoanInterestRate = dto.LoanInterestRate
            };

            await _planRepository.AddAsync(plan);
            await _planRepository.SaveChangesAsync();
            return MapToDto(plan);
        }

        public async Task<PlanResponseDto> UpdatePlanAsync(int id, UpdatePlanDto dto)
        {
            var plan = await _planRepository.GetByIdAsync(id);
            if (plan == null)
                throw new NotFoundException("Plan", id);

            if (dto.MinAge >= dto.MaxAge)
                throw new BadRequestException(
                    "MinAge must be less than MaxAge");

            if (dto.MinCoverageAmount >= dto.MaxCoverageAmount)
                throw new BadRequestException(
                    "MinCoverageAmount must be less than MaxCoverageAmount");

            if (dto.MinTermYears >= dto.MaxTermYears)
                throw new BadRequestException(
                    "MinTermYears must be less than MaxTermYears");

            plan.PlanName = dto.PlanName;
            plan.PlanType = dto.PlanType;
            plan.Description = dto.Description;
            plan.BaseRate = dto.BaseRate;
            plan.MinAge = dto.MinAge;
            plan.MaxAge = dto.MaxAge;
            plan.MinCoverageAmount = dto.MinCoverageAmount;
            plan.MaxCoverageAmount = dto.MaxCoverageAmount;
            plan.MinTermYears = dto.MinTermYears;
            plan.MaxTermYears = dto.MaxTermYears;
            plan.MinNominees = dto.MinNominees;
            plan.MaxNominees = dto.MaxNominees;
            plan.GracePeriodDays = dto.GracePeriodDays;
            plan.HasMaturityBenefit = dto.HasMaturityBenefit;
            plan.IsReturnOfPremium = dto.IsReturnOfPremium;
            plan.MaxPolicyMembersAllowed = dto.MaxPolicyMembersAllowed;
            plan.IsActive = dto.IsActive;
            plan.CommissionRate = dto.CommissionRate;
            plan.HasDeathBenefit = dto.HasDeathBenefit;
            plan.HasBonus = dto.HasBonus;
            plan.HasLoanFacility = dto.HasLoanFacility;
            plan.CoverageIncreasing = dto.CoverageIncreasing;
            plan.CoverageIncreaseRate = dto.CoverageIncreaseRate;
            plan.CoverageUntilAge = dto.CoverageUntilAge;
            plan.LoanEligibleAfterYears = dto.LoanEligibleAfterYears;
            plan.MaxLoanPercentage = dto.MaxLoanPercentage;
            plan.LoanInterestRate = dto.LoanInterestRate;

            _planRepository.Update(plan);
            await _planRepository.SaveChangesAsync();
            return MapToDto(plan);
        }

        public async Task DeletePlanAsync(int id)
        {
            var plan = await _planRepository.GetByIdAsync(id);
            if (plan == null)
                throw new NotFoundException("Plan", id);
            plan.IsActive = false;
            _planRepository.Update(plan);
            await _planRepository.SaveChangesAsync();
        }

        private static PlanResponseDto MapToDto(Plan plan) => new()
        {
            Id = plan.Id,
            PlanName = plan.PlanName,
            PlanType = plan.PlanType,
            Description = plan.Description,
            BaseRate = plan.BaseRate,
            MinAge = plan.MinAge,
            MaxAge = plan.MaxAge,
            MinCoverageAmount = plan.MinCoverageAmount,
            MaxCoverageAmount = plan.MaxCoverageAmount,
            MinTermYears = plan.MinTermYears,
            MaxTermYears = plan.MaxTermYears,
            MinNominees = plan.MinNominees,
            MaxNominees = plan.MaxNominees,
            GracePeriodDays = plan.GracePeriodDays,
            HasMaturityBenefit = plan.HasMaturityBenefit,
            IsReturnOfPremium = plan.IsReturnOfPremium,
            MaxPolicyMembersAllowed = plan.MaxPolicyMembersAllowed,
            CreatedAt = plan.CreatedAt,
            IsActive = plan.IsActive,
            CommissionRate = plan.CommissionRate,
            HasDeathBenefit = plan.HasDeathBenefit,
            HasBonus = plan.HasBonus,
            HasLoanFacility = plan.HasLoanFacility,
            CoverageIncreasing = plan.CoverageIncreasing,
            CoverageIncreaseRate = plan.CoverageIncreaseRate,
            CoverageUntilAge = plan.CoverageUntilAge,
            LoanEligibleAfterYears = plan.LoanEligibleAfterYears,
            MaxLoanPercentage = plan.MaxLoanPercentage,
            LoanInterestRate = plan.LoanInterestRate
        };
        public async Task<IEnumerable<PlanResponseDto>> GetFilteredPlansAsync(
            PlanFilterDto filter, string role)
        {
            var plans = await _planRepository.GetFilteredAsync(filter);

            // Customers should only see active plans
            if (role == "Customer")
                plans = plans.Where(p => p.IsActive);

            var result = plans.Select(MapToDto).ToList();
            return result;
        }
    }
}