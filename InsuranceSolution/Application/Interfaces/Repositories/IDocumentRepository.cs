using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;

namespace Application.Interfaces.Repositories
{
    public interface IDocumentRepository
    {
        Task AddAsync(Document document);
        Task<IEnumerable<Document>> GetByPolicyIdAsync(int policyId);
        Task SaveChangesAsync();
        Task<Document?> GetByIdAsync(int id);
    }
}