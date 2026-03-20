using BillingSys.Shared.Models;

namespace BillingSys.Functions.Repositories;

public interface IProjectRepository
{
    Task<ServiceResult<Project>> GetAsync(string customerId, string projectCode);
    Task<ServiceResult<List<Project>>> GetByCustomerAsync(string customerId);
    Task<ServiceResult<List<Project>>> GetAllAsync(ProjectStatus? status = null);
    Task<ServiceResult<Project>> UpsertAsync(Project project);
}
