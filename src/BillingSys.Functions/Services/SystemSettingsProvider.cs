using BillingSys.Functions.Repositories;
using BillingSys.Shared.Interfaces;
using BillingSys.Shared.Models;
using BillingSys.Shared.Services;
using Microsoft.Extensions.Logging;

namespace BillingSys.Functions.Services;

/// <summary>
/// Azure Functions implementation of ISystemSettingsProvider.
/// Loads and caches system configuration from Table Storage.
/// </summary>
// Old: depended on concrete TableStorageService
// New: depends on ISystemConfigRepository interface
public class SystemSettingsProvider : ISystemSettingsProvider
{
    #region Fields

    private readonly ISystemConfigRepository _configRepo;
    private readonly ILogger<SystemSettingsProvider> _logger;
    private SystemConfig? _cachedConfig;
    private readonly SemaphoreSlim _lock = new(1, 1);

    #endregion

    #region Constructor

    public SystemSettingsProvider(ISystemConfigRepository configRepo, ILogger<SystemSettingsProvider> logger)
    {
        _configRepo = configRepo;
        _logger = logger;
    }

    #endregion

    #region Public Methods

    public async Task<SystemConfig> GetConfigAsync()
    {
        if (_cachedConfig != null)
        {
            return _cachedConfig;
        }

        await _lock.WaitAsync();
        try
        {
            if (_cachedConfig != null)
            {
                return _cachedConfig;
            }

            var result = await _configRepo.GetAsync();
            if (result.Success && result.Data != null)
            {
                _cachedConfig = result.Data;
                SystemSettings.Load(_cachedConfig);
                _logger.LogInformation("System configuration loaded successfully");
            }
            else
            {
                _cachedConfig = SystemConfig.CreateDefault();
                SystemSettings.Load(_cachedConfig);
                _logger.LogWarning("Using default system configuration");
            }

            return _cachedConfig;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<ServiceResult> UpdateConfigAsync(SystemConfig config)
    {
        try
        {
            config.StampUpdated();
            var result = await _configRepo.UpsertAsync(config);

            if (result.Success)
            {
                _cachedConfig = config;
                SystemSettings.Load(config);
                _logger.LogInformation("System configuration updated successfully");
                return ServiceResult.Ok();
            }

            return ServiceResult.Fail(result.ErrorMessage ?? "Failed to update config");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating system configuration");
            return ServiceResult.Fail(ex.Message);
        }
    }

    public async Task ReloadAsync()
    {
        await _lock.WaitAsync();
        try
        {
            _cachedConfig = null;
            SystemSettings.Clear();
        }
        finally
        {
            _lock.Release();
        }

        await GetConfigAsync();
    }

    #endregion
}
