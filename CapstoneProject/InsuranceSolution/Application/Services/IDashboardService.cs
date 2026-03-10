using Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public interface IDashboardService
    {
        Task<AdminDashboardDto> GetAdminDashboardAsync();
        Task<CustomerDashboardDto> GetCustomerDashboardAsync(int customerId);
        Task<AgentDashboardDto> GetAgentDashboard(int agentId);
        Task<ClaimsOfficerDashboardDto> GetClaimsOfficerDashboardAsync(int officerId);
    }
}