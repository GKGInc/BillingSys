using Azure;
using BillingSys.Functions.Services;
using BillingSys.Shared.Models;
using Microsoft.Extensions.Logging;

namespace BillingSys.Functions.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly TableStorageContext _context;
    private readonly ILogger<CustomerRepository> _logger;

    public CustomerRepository(TableStorageContext context, ILogger<CustomerRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Public Methods

    public async Task<ServiceResult<Customer>> GetAsync(string id)
    {
        try
        {
            var table = _context.GetTable(TableStorageContext.CustomersTable);
            var response = await table.GetEntityAsync<CustomerEntity>("CUSTOMER", id);
            return ServiceResult<Customer>.Ok(response.Value.ToModel());
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return ServiceResult<Customer>.Fail($"Customer {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer {Id}", id);
            return ServiceResult<Customer>.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult<List<Customer>>> GetAllAsync(bool activeOnly = true)
    {
        try
        {
            var table = _context.GetTable(TableStorageContext.CustomersTable);
            var filter = activeOnly ? "PartitionKey eq 'CUSTOMER' and IsActive eq true" : "PartitionKey eq 'CUSTOMER'";
            var customers = new List<Customer>();
            await foreach (var entity in table.QueryAsync<CustomerEntity>(filter))
            {
                customers.Add(entity.ToModel());
            }
            return ServiceResult<List<Customer>>.Ok(customers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customers");
            return ServiceResult<List<Customer>>.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult<Customer>> UpsertAsync(Customer customer)
    {
        try
        {
            var table = _context.GetTable(TableStorageContext.CustomersTable);
            var entity = CustomerEntity.FromModel(customer);
            await table.UpsertEntityAsync(entity);
            return ServiceResult<Customer>.Ok(customer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting customer {Id}", customer.CustomerId);
            return ServiceResult<Customer>.Fail(ex.Message);
        }
    }

    #endregion
}
