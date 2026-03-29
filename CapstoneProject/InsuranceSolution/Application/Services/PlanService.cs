using Application.DTOs;
using Application.Exceptions;
using Application.Interfaces;
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
        private readonly IAiService _aiService;

        public PlanService(IPlanRepository planRepository, IAiService aiService)
        {
            _planRepository = planRepository;
            _aiService = aiService;
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

            if (!dto.IsCoverageUntilAge && dto.MinTermYears >= dto.MaxTermYears)
                throw new BadRequestException(
                    "MinTermYears must be less than MaxTermYears");

            if (dto.IsCoverageUntilAge && !dto.CoverageUntilAge.HasValue)
                throw new BadRequestException(
                    "CoverageUntilAge is required when lifelong coverage is enabled");


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
                MinTermYears = dto.IsCoverageUntilAge ? null : dto.MinTermYears,
                MaxTermYears = dto.IsCoverageUntilAge ? null : dto.MaxTermYears,
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
                IsCoverageUntilAge = dto.IsCoverageUntilAge,
                CoverageUntilAge = dto.IsCoverageUntilAge ? dto.CoverageUntilAge : null,
                LoanEligibleAfterYears = dto.LoanEligibleAfterYears,
                MaxLoanPercentage = dto.MaxLoanPercentage,
                LoanInterestRate = dto.LoanInterestRate,
                BonusRate = dto.BonusRate,
                TerminalBonusRate = dto.TerminalBonusRate,
                ReinstatementPenaltyAmount = dto.ReinstatementPenaltyAmount,
                ReinstatementDays = dto.ReinstatementDays
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

            if (!dto.IsCoverageUntilAge && dto.MinTermYears >= dto.MaxTermYears)
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
            plan.MinTermYears = dto.IsCoverageUntilAge ? null : dto.MinTermYears;
            plan.MaxTermYears = dto.IsCoverageUntilAge ? null : dto.MaxTermYears;
            plan.CoverageUntilAge = dto.IsCoverageUntilAge ? dto.CoverageUntilAge : null;
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
            plan.IsCoverageUntilAge = dto.IsCoverageUntilAge;
            plan.LoanEligibleAfterYears = dto.LoanEligibleAfterYears;
            plan.MaxLoanPercentage = dto.MaxLoanPercentage;
            plan.LoanInterestRate = dto.LoanInterestRate;
            plan.BonusRate = dto.BonusRate;
            plan.TerminalBonusRate = dto.TerminalBonusRate;
            plan.ReinstatementPenaltyAmount = dto.ReinstatementPenaltyAmount;
            plan.ReinstatementDays = dto.ReinstatementDays;

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
            IsCoverageUntilAge = plan.IsCoverageUntilAge,
            CoverageUntilAge = plan.CoverageUntilAge,
            LoanEligibleAfterYears = plan.LoanEligibleAfterYears,
            MaxLoanPercentage = plan.MaxLoanPercentage,
            LoanInterestRate = plan.LoanInterestRate,
            BonusRate = plan.BonusRate,
            TerminalBonusRate = plan.TerminalBonusRate,
            ReinstatementPenaltyAmount = plan.ReinstatementPenaltyAmount,
            ReinstatementDays = plan.ReinstatementDays
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

        public async Task<PlanComparisonResponseDto> ComparePlansAsync(ComparePlansDto dto)
        {
            var plans = await _planRepository.GetByIdsAsync(dto.PlanIds);
            if (!plans.Any()) throw new BadRequestException("No plans found for comparison");

            var planDetails = string.Join("\n\n", plans.Select(p => $@"
Plan: {p.PlanName}
Type: {p.PlanType}
Age Range: {p.MinAge} to {p.MaxAge} years
Coverage: ₹{p.MinCoverageAmount:N0} to ₹{p.MaxCoverageAmount:N0}
Maturity Benefit: {(new[] { "Endowment", "Savings", "WholeLife" }.Contains(p.PlanType.ToString()) ? "Yes" : "No")}
Loan Facility: {(p.HasLoanFacility ? "Yes" : "No")}
Bonus: {(p.HasBonus ? "Yes" : "No")}
Whole Life Cover: {(p.IsCoverageUntilAge ? "Yes" : "No")}
Description: {p.Description}"));

            string systemPrompt = "You are a helpful insurance advisor. Be concise and friendly.";
            string userPrompt = $@"Compare these insurance plans for a customer. 
Write a clear, friendly summary that:
1. Explains what makes each plan unique in 2 sentences
2. States who each plan is best suited for
3. Gives a final recommendation based on common needs
Keep it simple — no jargon. Use plain English.

PLANS:
{planDetails}";

            string summary = await _aiService.GetAiResponseAsync(systemPrompt, userPrompt, null);

            return new PlanComparisonResponseDto
            {
                Summary = summary,
                Plans = plans.Select(MapToDto)
            };
        }
    }
}