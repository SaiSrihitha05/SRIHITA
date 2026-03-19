using Domain.Entities;
using System.Threading.Tasks;

namespace Application.Interfaces.Repositories
{
    public interface ISystemConfigRepository
    {
        Task<SystemConfig> GetConfigAsync();
        void Update(SystemConfig config);
        Task SaveChangesAsync();
    }
}
