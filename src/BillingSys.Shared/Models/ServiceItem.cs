namespace BillingSys.Shared.Models;

public class ServiceItem : AuditableEntity
{
    #region Identity

    public string ItemCode { get; set; } = string.Empty;

    #endregion

    #region Details

    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = "Service";

    #endregion

    #region Integration

    public string? IncomeAccount { get; set; }
    public string? QboItemId { get; set; }

    #endregion

    #region Status

    public bool IsActive { get; set; } = true;

    #endregion
}
