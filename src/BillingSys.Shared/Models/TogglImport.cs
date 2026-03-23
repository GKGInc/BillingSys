namespace BillingSys.Shared.Models;

public class TogglImport : AuditableEntity
{
    #region Identity

    public string Id { get; set; } = Guid.NewGuid().ToString();
    public long TogglEntryId { get; set; }

    #endregion

    #region Toggl Source Data

    public string? OriginalDescription { get; set; }
    public string? TogglProjectName { get; set; }
    public string? TogglClientName { get; set; }
    public string EmployeeId { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal Hours { get; set; }
    public bool Billable { get; set; }
    public string? TogglTags { get; set; }

    #endregion

    #region Summarization

    public string? SummarizedDescription { get; set; }
    public string? BillingGroupKey { get; set; }

    #endregion

    #region Mapping

    public string? MappedProjectCode { get; set; }
    public string? MappedCustomerId { get; set; }

    #endregion

    #region Absorption

    /// <summary>
    /// If this blank entry was absorbed into another entry, this holds the parent's Id.
    /// </summary>
    public string? AbsorbedIntoId { get; set; }

    /// <summary>
    /// Hours absorbed from blank entries below this one.
    /// </summary>
    public decimal AbsorbedHours { get; set; }

    #endregion

    #region Status

    public TogglImportStatus Status { get; set; } = TogglImportStatus.Raw;

    /// <summary>
    /// The Wrigley TimeEntry.Id created when this import is approved.
    /// </summary>
    public string? TimeEntryId { get; set; }

    #endregion

    #region Batch Tracking

    /// <summary>
    /// Groups entries from the same pull/import operation.
    /// </summary>
    public string BatchId { get; set; } = string.Empty;

    #endregion
}

public enum TogglImportStatus
{
    Raw,
    Absorbed,
    Summarized,
    Approved,
    Imported,
    Skipped
}
