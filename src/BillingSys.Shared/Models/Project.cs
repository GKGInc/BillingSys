namespace BillingSys.Shared.Models;

public class Project : AuditableEntity
{
    #region Identity

    public string ProjectCode { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;

    #endregion

    #region Details

    public string Description { get; set; } = string.Empty;
    public string ServiceItemCode { get; set; } = string.Empty;
    public string? CustomerPO { get; set; }
    public string? ProgrammerId { get; set; }

    #endregion

    #region Billing

    public decimal Price { get; set; }
    public decimal QuotedHours { get; set; }
    public decimal AdditionalHours { get; set; }
    public decimal BilledHours { get; set; }
    public bool PreBill { get; set; }
    public bool AddDetailToInvoice { get; set; }

    #endregion

    #region Status

    public ProjectStatus Status { get; set; } = ProjectStatus.Active;

    #endregion

    #region Computed Properties

    public decimal RemainingHours => QuotedHours + AdditionalHours - BilledHours;
    public decimal TotalHours => QuotedHours + AdditionalHours;

    #endregion
}

public enum ProjectStatus
{
    Active,
    Completed,
    OnHold,
    Cancelled
}
