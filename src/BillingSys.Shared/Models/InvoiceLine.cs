namespace BillingSys.Shared.Models;

public class InvoiceLine
{
    #region Identity

    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string InvoiceNumber { get; set; } = string.Empty;
    public int LineNumber { get; set; }

    #endregion

    #region Item Details

    public string ItemCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = "HR";

    #endregion

    #region Pricing

    public decimal Quantity { get; set; }
    public decimal Price { get; set; }

    #endregion

    #region Additional Info

    public string? Memo { get; set; }
    public DateTime? ServiceDate { get; set; }
    public string? EmployeeId { get; set; }

    #endregion

    #region Computed Properties

    public decimal ExtendedPrice => Math.Round(Quantity * Price, 2);

    #endregion
}
