using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class DocumentRepository : IDocumentRepository
    {
        private readonly InsuranceDbContext _context;

        public DocumentRepository(InsuranceDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Document document) =>
            await _context.Documents.AddAsync(document);

        public void Update(Document document) =>
            _context.Documents.Update(document);

        public async Task<IEnumerable<Document>> GetByPolicyIdAsync(int policyId) =>
            await _context.Documents
                .Where(d => d.PolicyAssignmentId == policyId)
                .ToListAsync();

        public async Task<IEnumerable<Document>> GetByClaimIdAsync(int claimId) =>
            await _context.Documents
                .Where(d => d.ClaimId == claimId)
                .ToListAsync();

        public async Task SaveChangesAsync() =>
            await _context.SaveChangesAsync();

        public async Task<Document?> GetByIdAsync(int id) =>
            await _context.Documents.FindAsync(id);
    }
}