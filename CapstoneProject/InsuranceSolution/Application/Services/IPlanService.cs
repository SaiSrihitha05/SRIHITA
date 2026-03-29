using Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Services
{
    public interface IPlanService
    {
        Task<IEnumerable<PlanResponseDto>> GetAllPlansAsync();
        Task<IEnumerable<PlanResponseDto>> GetActivePlansAsync();
        Task<PlanResponseDto> GetPlanByIdAsync(int id, string role);
        Task<PlanResponseDto> CreatePlanAsync(CreatePlanDto dto);
        Task<PlanResponseDto> UpdatePlanAsync(int id, UpdatePlanDto dto);
        Task DeletePlanAsync(int id);
        Task<IEnumerable<PlanResponseDto>> GetFilteredPlansAsync(
            PlanFilterDto filter, string role);
        Task<PlanComparisonResponseDto> ComparePlansAsync(ComparePlansDto dto);
    }
}
