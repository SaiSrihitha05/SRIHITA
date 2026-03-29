using Domain.Entities;
using System.Threading.Tasks;

namespace Application.Interfaces.Services
{
    public interface IClaimsOfficerAssignmentService
    {
        Task<User?> AssignOfficerAsync();
    }
}
