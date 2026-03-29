using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class ClaimsOfficerAssignmentService : IClaimsOfficerAssignmentService
    {
        private readonly IUserRepository _userRepository;
        private readonly ISystemConfigRepository _systemConfigRepository;

        public ClaimsOfficerAssignmentService(
            IUserRepository userRepository,
            ISystemConfigRepository systemConfigRepository)
        {
            _userRepository = userRepository;
            _systemConfigRepository = systemConfigRepository;
        }

        public async Task<User?> AssignOfficerAsync()
        {
            var officers = await _userRepository.GetByRoleAsync(UserRole.ClaimsOfficer);
            if (!officers.Any()) return null;

            var config = await _systemConfigRepository.GetConfigAsync();
            // Using a separate index for officers would be better, but let's assume we can reuse or extend SystemConfig
            // For now, let's just use the same index logic but mod by officer count
            int index = (config.LastAgentAssignmentIndex + 1) % officers.Count();
            config.LastAgentAssignmentIndex = index;
            _systemConfigRepository.Update(config);
            await _systemConfigRepository.SaveChangesAsync();

            return officers.ElementAt(index);
        }
    }
}
