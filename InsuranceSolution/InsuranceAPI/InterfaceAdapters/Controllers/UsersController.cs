using Application.DTOs;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InsuranceAPI.InterfaceAdapters.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        //  Create Staff 
        [Authorize(Roles = "Admin")]
        [HttpPost("agents")]
        public async Task<IActionResult> CreateAgent(
            [FromBody] CreateAgentDto dto)
        {
            var result = await _userService.CreateAgentAsync(dto);
            return CreatedAtAction(nameof(GetUserById),
                new { id = result.Id }, result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("claims-officers")]
        public async Task<IActionResult> CreateClaimsOfficer(
            [FromBody] CreateClaimsOfficerDto dto)
        {
            var result = await _userService.CreateClaimsOfficerAsync(dto);
            return CreatedAtAction(nameof(GetUserById),
                new { id = result.Id }, result);
        }

        //  Get by Role 
        [Authorize(Roles = "Admin")]
        [HttpGet("customers")]
        public async Task<IActionResult> GetAllCustomers()
        {
            var result = await _userService.GetAllCustomersAsync();
            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("agents")]
        public async Task<IActionResult> GetAllAgents()
        {
            var result = await _userService.GetAllAgentsAsync();
            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("claims-officers")]
        public async Task<IActionResult> GetAllClaimsOfficers()
        {
            var result = await _userService.GetAllClaimsOfficersAsync();
            return Ok(result);
        }

        //  General 
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            var result = await _userService.GetAllUsersAsync();
            return Ok(result);
        }
        [Authorize(Roles = "Admin")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var result = await _userService.GetUserByIdAsync(id);
            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(
            int id, [FromBody] UpdateUserDto dto)
        {
            var result = await _userService.UpdateUserAsync(id, dto);
            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            await _userService.DeleteUserAsync(id);
            return NoContent();
        }

        //  Own Profile (any logged-in user) 

        [HttpGet("profile")]
        [Authorize(Roles = "Admin,Customer,Agent,ClaimsOfficer")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var result = await _userService.GetProfileAsync(userId);
            return Ok(result);
        }

        [HttpPut("profile")]
        [Authorize(Roles = "Admin,Customer,Agent,ClaimsOfficer")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserDto dto)
        {
            var userId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            await _userService.UpdateProfileAsync(userId, dto);
            return Ok(new { message = "Profile updated successfully" });
        }
    }
}