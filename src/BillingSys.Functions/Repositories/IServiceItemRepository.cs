using BillingSys.Shared.Models;

namespace BillingSys.Functions.Repositories;

public interface IServiceItemRepository
{
    Task<ServiceResult<ServiceItem>> GetAsync(string itemCode);
    Task<ServiceResult<List<ServiceItem>>> GetAllAsync(bool activeOnly = true);
    Task<ServiceResult<ServiceItem>> UpsertAsync(ServiceItem item);
}
