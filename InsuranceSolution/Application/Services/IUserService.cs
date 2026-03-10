using Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public interface IUserService
    {
        //  Admin creates staff 
        Task<UserResponseDto> CreateAgentAsync(CreateAgentDto dto);
        Task<UserResponseDto> CreateClaimsOfficerAsync(CreateClaimsOfficerDto dto);

        //  Admin get by role 
        Task<IEnumerable<UserResponseDto>> GetAllCustomersAsync();
        Task<IEnumerable<UserResponseDto>> GetAllAgentsAsync();
        Task<IEnumerable<UserResponseDto>> GetAllClaimsOfficersAsync();

        //  Admin general 
        Task<IEnumerable<UserResponseDto>> GetAllUsersAsync();
        Task<UserResponseDto> GetUserByIdAsync(int id);
        Task<UserResponseDto> UpdateUserAsync(int id, UpdateUserDto dto);
        Task DeleteUserAsync(int id);

        //  Own profile 
        Task<UserResponseDto> GetProfileAsync(int userId);
        Task UpdateProfileAsync(int userId, UpdateUserDto dto);
    }
}