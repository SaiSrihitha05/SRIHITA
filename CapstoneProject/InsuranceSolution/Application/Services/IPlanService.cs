using Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public interface IPlanService
    {
        Task<IEnumerable<PlanResponseDto>> GetAllPlansAsync();      // Admin - all plans
        Task<IEnumerable<PlanResponseDto>> GetActivePlansAsync();   // Customer - active only
        Task<PlanResponseDto> GetPlanByIdAsync(int id,string role);             // Admin + Customer
        Task<PlanResponseDto> CreatePlanAsync(CreatePlanDto dto);   // Admin only
        Task<PlanResponseDto> UpdatePlanAsync(int id, UpdatePlanDto dto); // Admin only
        Task DeletePlanAsync(int id);                               // Admin only
        Task<IEnumerable<PlanResponseDto>> GetFilteredPlansAsync(
            PlanFilterDto filter, string role);  
    }
}
