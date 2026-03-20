using Azure;
using BillingSys.Functions.Services;
using BillingSys.Shared.Models;
using Microsoft.Extensions.Logging;

namespace BillingSys.Functions.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly TableStorageContext _context;
    private readonly ILogger<EmployeeRepository> _logger;

    public EmployeeRepository(TableStorageContext context, ILogger<EmployeeRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Public Methods

    public async Task<ServiceResult<Employee>> GetAsync(string id)
    {
        try
        {
            var table = _context.GetTable(TableStorageContext.EmployeesTable);
            var response = await table.GetEntityAsync<EmployeeEntity>("EMPLOYEE", id);
            return ServiceResult<Employee>.Ok(response.Value.ToModel());
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return ServiceResult<Employee>.Fail($"Employee {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting employee {Id}", id);
            return ServiceResult<Employee>.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult<Employee>> GetByEmailAsync(string email)
    {
        try
        {
            var table = _context.GetTable(TableStorageContext.EmployeesTable);
            var filter = $"PartitionKey eq 'EMPLOYEE' and Email eq '{email}'";
            await foreach (var entity in table.QueryAsync<EmployeeEntity>(filter))
            {
                return ServiceResult<Employee>.Ok(entity.ToModel());
            }
            return ServiceResult<Employee>.Fail($"Employee with email {email} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting employee by email {Email}", email);
            return ServiceResult<Employee>.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult<List<Employee>>> GetAllAsync(bool activeOnly = true)
    {
        try
        {
            var table = _context.GetTable(TableStorageContext.EmployeesTable);
            var filter = activeOnly ? "PartitionKey eq 'EMPLOYEE' and IsActive eq true" : "PartitionKey eq 'EMPLOYEE'";
            var employees = new List<Employee>();
            await foreach (var entity in table.QueryAsync<EmployeeEntity>(filter))
            {
                employees.Add(entity.ToModel());
            }
            return ServiceResult<List<Employee>>.Ok(employees);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting employees");
            return ServiceResult<List<Employee>>.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult<Employee>> UpsertAsync(Employee employee)
    {
        try
        {
            var table = _context.GetTable(TableStorageContext.EmployeesTable);
            var entity = EmployeeEntity.FromModel(employee);
            await table.UpsertEntityAsync(entity);
            return ServiceResult<Employee>.Ok(employee);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting employee {Id}", employee.Id);
            return ServiceResult<Employee>.Fail(ex.Message);
        }
    }

    #endregion
}
