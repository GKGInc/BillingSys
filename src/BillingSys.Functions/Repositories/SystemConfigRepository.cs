using Azure;
using BillingSys.Functions.Services;
using BillingSys.Shared.Models;
using Microsoft.Extensions.Logging;

namespace BillingSys.Functions.Repositories;

public class SystemConfigRepository : ISystemConfigRepository
{
    private readonly TableStorageContext _context;
    private readonly ILogger<SystemConfigRepository> _logger;

    public SystemConfigRepository(TableStorageContext context, ILogger<SystemConfigRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Public Methods

    public async Task<ServiceResult<SystemConfig>> GetAsync()
    {
        try
        {
            var table = _context.GetTable(TableStorageContext.SystemConfigTable);
            var response = await table.GetEntityAsync<SystemConfigEntity>("CONFIG", SystemConfig.SingletonId);
            return ServiceResult<SystemConfig>.Ok(response.Value.ToModel());
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            var defaultConfig = SystemConfig.CreateDefault();
            return ServiceResult<SystemConfig>.Ok(defaultConfig);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system config");
            return ServiceResult<SystemConfig>.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult<SystemConfig>> UpsertAsync(SystemConfig config)
    {
        try
        {
            var table = _context.GetTable(TableStorageContext.SystemConfigTable);
            var entity = SystemConfigEntity.FromModel(config);
            await table.UpsertEntityAsync(entity);
            return ServiceResult<SystemConfig>.Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting system config");
            return ServiceResult<SystemConfig>.Fail(ex.Message);
        }
    }

    public async Task InitializeTablesAsync()
    {
        await _context.InitializeTablesAsync();
    }

    #endregion
}
