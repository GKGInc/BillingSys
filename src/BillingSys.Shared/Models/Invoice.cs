namespace BillingSys.Shared.Models;

public class Invoice
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? PurchaseOrderNumber { get; set; }
    public decimal InvoiceAmount { get; set; }
    public string? OrderNumber { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public string? QboInvoiceId { get; set; }
    public DateTime? QboSyncDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public List<InvoiceLine> Lines { get; set; } = new();
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
