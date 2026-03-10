using Application.DTOs;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Repositories
{
    public interface IPlanRepository
    {
        Task<IEnumerable<Plan>> GetAllAsync();
        Task<IEnumerable<Plan>> GetAllActiveAsync();
        Task<Plan?> GetByIdAsync(int id);
        Task AddAsync(Plan plan);
        void Update(Plan plan);
        void Delete(Plan plan);
        Task<IEnumerable<Plan>> GetFilteredAsync(PlanFilterDto filter);
        Task SaveChangesAsync();
        Task<bool> ExistsByNameAsync(string planName);
    }
}