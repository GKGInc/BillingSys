using BillingSys.Shared.Models;

namespace BillingSys.Functions.Repositories;

public interface ISystemConfigRepository
{
    Task<ServiceResult<SystemConfig>> GetAsync();
    Task<ServiceResult<SystemConfig>> UpsertAsync(SystemConfig config);
    Task InitializeTablesAsync();
}
