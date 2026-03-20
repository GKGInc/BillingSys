namespace BillingSys.Shared.Models;

/// <summary>
/// Base class for all entities that require audit tracking.
/// Provides CreatedAt, CreatedBy, UpdatedAt, and UpdatedBy fields.
/// </summary>
public abstract class AuditableEntity
{
    #region Audit Fields

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    #endregion

    #region State Helpers

    /// <summary>
    /// Indicates if this entity has never been saved (CreatedAt is default)
    /// </summary>
    public bool IsNew => CreatedAt == default;

    /// <summary>
    /// Indicates if the entity has been modified (UpdatedAt has a value)
    /// </summary>
    public bool HasBeenModified => UpdatedAt.HasValue;

    #endregion

    #region Audit Methods

    /// <summary>
    /// Stamps the creation audit fields. Call this when creating a new entity.
    /// </summary>
    /// <param name="userId">The ID of the user creating the entity</param>
    public void StampCreated(string? userId = null)
    {
        if (CreatedAt == default)
        {
            CreatedAt = DateTime.UtcNow;
        }
        if (string.IsNullOrEmpty(CreatedBy) && !string.IsNullOrEmpty(userId))
        {
            CreatedBy = userId;
        }
    }

    /// <summary>
    /// Stamps the update audit fields. Call this when updating an entity.
    /// </summary>
    /// <param name="userId">The ID of the user updating the entity</param>
    public void StampUpdated(string? userId = null)
    {
        UpdatedAt = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(userId))
        {
            UpdatedBy = userId;
        }
    }

    /// <summary>
    /// Stamps the appropriate audit fields based on whether this is a new or existing entity.
    /// </summary>
    /// <param name="userId">The ID of the user performing the operation</param>
    public void StampAudit(string? userId = null)
    {
        if (IsNew)
        {
            StampCreated(userId);
        }
        else
        {
            StampUpdated(userId);
        }
    }

    #endregion
}
