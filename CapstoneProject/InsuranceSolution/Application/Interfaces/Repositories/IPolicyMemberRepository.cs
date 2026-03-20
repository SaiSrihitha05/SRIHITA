using Domain.Entities;
using System.Threading.Tasks;

namespace Application.Interfaces.Repositories
{
    public interface IPolicyMemberRepository
    {
        Task<PolicyMember?> GetByIdAsync(int id);
        void Update(PolicyMember member);
        Task SaveChangesAsync();
    }
}
