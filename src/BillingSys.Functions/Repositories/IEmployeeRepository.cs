using BillingSys.Shared.Models;

namespace BillingSys.Functions.Repositories;

public interface IEmployeeRepository
{
    Task<ServiceResult<Employee>> GetAsync(string id);
    Task<ServiceResult<Employee>> GetByEmailAsync(string email);
    Task<ServiceResult<List<Employee>>> GetAllAsync(bool activeOnly = true);
    Task<ServiceResult<Employee>> UpsertAsync(Employee employee);
}
