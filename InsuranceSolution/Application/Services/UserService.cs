using Application.DTOs;
using Application.Exceptions;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPolicyRepository _policyRepository;

        public UserService(IUserRepository userRepository, IPolicyRepository policyRepository)
        {
            _userRepository = userRepository;
            _policyRepository = policyRepository;
        }

        //  Admin creates staff 

        public async Task<UserResponseDto> CreateAgentAsync(CreateAgentDto dto)
        {
            await EnsureEmailNotTaken(dto.Email);

            var user = BuildUser(
                dto.Name, dto.Email, dto.Password, dto.Phone, UserRole.Agent);

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();
            return MapToDto(user);
        }

        public async Task<UserResponseDto> CreateClaimsOfficerAsync(
            CreateClaimsOfficerDto dto)
        {
            await EnsureEmailNotTaken(dto.Email);

            var user = BuildUser(
                dto.Name, dto.Email, dto.Password, dto.Phone, UserRole.ClaimsOfficer);

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();
            return MapToDto(user);
        }

        //  Admin get by role 

        public async Task<IEnumerable<UserResponseDto>> GetAllCustomersAsync()
        {
            var users = await _userRepository.GetByRoleAsync(UserRole.Customer);
            return users.Select(MapToDto);
        }

        public async Task<IEnumerable<UserResponseDto>> GetAllAgentsAsync()
        {
            var users = await _userRepository.GetByRoleAsync(UserRole.Agent);
            return users.Select(MapToDto);
        }

        public async Task<IEnumerable<UserResponseDto>> GetAllClaimsOfficersAsync()
        {
            var users = await _userRepository.GetByRoleAsync(UserRole.ClaimsOfficer);
            return users.Select(MapToDto);
        }

        //  Admin general 

        public async Task<IEnumerable<UserResponseDto>> GetAllUsersAsync()
        {
            var users = await _userRepository.GetAllAsync();
            return users.Select(MapToDto);
        }

        public async Task<UserResponseDto> GetUserByIdAsync(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
                throw new NotFoundException("User", id);

            return MapToDto(user);
        }

        public async Task<UserResponseDto> UpdateUserAsync(int id, UpdateUserDto dto)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
                throw new NotFoundException("User", id);

            user.Name = dto.Name;
            user.Phone = dto.Phone;
            user.IsActive = dto.IsActive;

            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();
            return MapToDto(user);
        }

        public async Task DeleteUserAsync(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
                throw new NotFoundException("User", id);

            // Check active policies based on role
            if (user.Role == UserRole.Customer)
            {
                var hasPolicies = await _policyRepository
                    .HasActivePoliciesByCustomerAsync(id);
                if (hasPolicies)
                    throw new BadRequestException(
                        "Cannot delete customer with active policies");
            }

            if (user.Role == UserRole.Agent)
            {
                var hasPolicies = await _policyRepository
                    .HasActivePoliciesByAgentAsync(id);
                if (hasPolicies)
                    throw new BadRequestException(
                        "Cannot delete agent with active assigned policies");
            }

            _userRepository.Delete(user);
            await _userRepository.SaveChangesAsync();
        }

        //  Own profile 

        public async Task<UserResponseDto> GetProfileAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new NotFoundException("User", userId);

            return MapToDto(user);
        }

        public async Task UpdateProfileAsync(int userId, UpdateUserDto dto)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new NotFoundException("User", userId);

            user.Name = dto.Name;
            user.Phone = dto.Phone;

            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();
        }

        //  Private Helpers 

        private async Task EnsureEmailNotTaken(string email)
        {
            var existing = await _userRepository.GetByEmailAsync(email);
            if (existing != null)
                throw new ConflictException("Email already in use");
        }

        private static User BuildUser(
            string name, string email,
            string password, string phone, UserRole role) => new()
            {
                Name = name,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Phone = phone,
                Role = role,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

        private static UserResponseDto MapToDto(User user) => new()
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Phone = user.Phone,
            Role = user.Role.ToString(),
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };
    }
}