using Application.DTOs;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InsuranceAPI.InterfaceAdapters.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlansController : ControllerBase
    {
        private readonly IPlanService _planService;

        public PlansController(IPlanService planService)
        {
            _planService = planService;
        }

        //  Public to Admin and Customer 

        [HttpGet]
        [Authorize(Roles = "Admin,Customer")]
        public async Task<IActionResult> GetPlans()
        {
            // Admin sees all, Customer sees active only
            var isAdmin = User.IsInRole("Admin");

            var result = isAdmin
                ? await _planService.GetAllPlansAsync()
                : await _planService.GetActivePlansAsync();

            return Ok(result);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Customer")]
        public async Task<IActionResult> GetPlanById(int id)
        {
            var role = User.FindFirst(ClaimTypes.Role)!.Value;
            var result = await _planService.GetPlanByIdAsync(id, role);
            return Ok(result);
        }

        // Admin Only 

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreatePlan([FromBody] CreatePlanDto dto)
        {
            var result = await _planService.CreatePlanAsync(dto);
            return CreatedAtAction(nameof(GetPlanById),
                new { id = result.Id }, result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdatePlan(int id, [FromBody] UpdatePlanDto dto)
        {
            var result = await _planService.UpdatePlanAsync(id, dto);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeletePlan(int id)
        {
            await _planService.DeletePlanAsync(id);
            return NoContent();
        }

        [HttpGet("filter")]
        [Authorize(Roles = "Admin,Customer")]
        public async Task<IActionResult> GetFilteredPlans([FromQuery] PlanFilterDto filter)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "Customer";
            var result = await _planService.GetFilteredPlansAsync(filter, role);
            return Ok(result);
        }
    }
}
