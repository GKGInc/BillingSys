using BillingSys.Shared.Models;

namespace BillingSys.Functions.Repositories;

public interface ICustomerRepository
{
    Task<ServiceResult<Customer>> GetAsync(string id);
    Task<ServiceResult<List<Customer>>> GetAllAsync(bool activeOnly = true);
    Task<ServiceResult<Customer>> UpsertAsync(Customer customer);
}
