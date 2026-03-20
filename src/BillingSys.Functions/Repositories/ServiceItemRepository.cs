using Azure;
using BillingSys.Functions.Services;
using BillingSys.Shared.Models;
using Microsoft.Extensions.Logging;

namespace BillingSys.Functions.Repositories;

public class ServiceItemRepository : IServiceItemRepository
{
    private readonly TableStorageContext _context;
    private readonly ILogger<ServiceItemRepository> _logger;

    public ServiceItemRepository(TableStorageContext context, ILogger<ServiceItemRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Public Methods

    public async Task<ServiceResult<ServiceItem>> GetAsync(string itemCode)
    {
        try
        {
            var table = _context.GetTable(TableStorageContext.ServiceItemsTable);
            var response = await table.GetEntityAsync<ServiceItemEntity>("ITEM", itemCode);
            return ServiceResult<ServiceItem>.Ok(response.Value.ToModel());
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return ServiceResult<ServiceItem>.Fail($"Service item {itemCode} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting service item {ItemCode}", itemCode);
            return ServiceResult<ServiceItem>.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult<List<ServiceItem>>> GetAllAsync(bool activeOnly = true)
    {
        try
        {
            var table = _context.GetTable(TableStorageContext.ServiceItemsTable);
            var filter = activeOnly ? "PartitionKey eq 'ITEM' and IsActive eq true" : "PartitionKey eq 'ITEM'";
            var items = new List<ServiceItem>();
            await foreach (var entity in table.QueryAsync<ServiceItemEntity>(filter))
            {
                items.Add(entity.ToModel());
            }
            return ServiceResult<List<ServiceItem>>.Ok(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting service items");
            return ServiceResult<List<ServiceItem>>.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult<ServiceItem>> UpsertAsync(ServiceItem item)
    {
        try
        {
            var table = _context.GetTable(TableStorageContext.ServiceItemsTable);
            var entity = ServiceItemEntity.FromModel(item);
            await table.UpsertEntityAsync(entity);
            return ServiceResult<ServiceItem>.Ok(item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting service item {ItemCode}", item.ItemCode);
            return ServiceResult<ServiceItem>.Fail(ex.Message);
        }
    }

    #endregion
}
