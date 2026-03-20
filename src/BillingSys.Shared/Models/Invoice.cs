namespace BillingSys.Shared.Models;

public class Invoice : AuditableEntity
{
    #region Identity

    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }

    #endregion

    #region Customer

    public string CustomerId { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? PurchaseOrderNumber { get; set; }
    public string? OrderNumber { get; set; }

    #endregion

    #region Amounts

    public decimal InvoiceAmount { get; set; }

    #endregion

    #region Status

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    #endregion

    #region Integration

    public string? QboInvoiceId { get; set; }
    public DateTime? QboSyncDate { get; set; }

    #endregion

    #region Line Items

    public List<InvoiceLine> Lines { get; set; } = new();

    #endregion
}

public enum InvoiceStatus
{
    Draft,
    Pending,
    Posted,
    SyncedToQbo,
    Paid,
    Cancelled
}
