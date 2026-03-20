using BillingSys.Shared.Models;

namespace BillingSys.Shared.Interfaces;

/// <summary>
/// Interface for accessing system-wide settings.
/// Implement this in platform-specific projects.
/// </summary>
public interface ISystemSettingsProvider
{
    /// <summary>
    /// Gets the current system configuration
    /// </summary>
    Task<SystemConfig> GetConfigAsync();

    /// <summary>
    /// Updates the system configuration
    /// </summary>
    Task<ServiceResult> UpdateConfigAsync(SystemConfig config);

    /// <summary>
    /// Forces a reload of the cached configuration
    /// </summary>
    Task ReloadAsync();
}
