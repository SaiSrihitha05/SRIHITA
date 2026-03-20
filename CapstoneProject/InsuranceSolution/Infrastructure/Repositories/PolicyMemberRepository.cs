using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class PolicyMemberRepository : IPolicyMemberRepository
    {
        private readonly InsuranceDbContext _context;

        public PolicyMemberRepository(InsuranceDbContext context)
        {
            _context = context;
        }

        public async Task<PolicyMember?> GetByIdAsync(int id) =>
            await _context.PolicyMembers.FindAsync(id);

        public void Update(PolicyMember member) =>
            _context.PolicyMembers.Update(member);

        public async Task SaveChangesAsync() =>
            await _context.SaveChangesAsync();
    }
}
