namespace BillingSys.Shared.DTOs;

#region Pull

public class TogglPullRequest
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class TogglPullResult
{
    public string BatchId { get; set; } = string.Empty;
    public int TotalEntriesPulled { get; set; }
    public int BlankEntriesAbsorbed { get; set; }
    public int EntriesReadyForSummary { get; set; }
    public List<TogglImportGroup> Groups { get; set; } = new();
}

public class TogglImportGroup
{
    public string GroupKey { get; set; } = string.Empty;
    public string? TogglProjectName { get; set; }
    public string? TogglClientName { get; set; }
    public string? MappedProjectCode { get; set; }
    public string? MappedCustomerId { get; set; }
    public string? MappedCustomerName { get; set; }
    public decimal TotalHours { get; set; }
    public int EntryCount { get; set; }
    public List<TogglImportLine> Entries { get; set; } = new();
}

public class TogglImportLine
{
    public string Id { get; set; } = string.Empty;
    public long TogglEntryId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal Hours { get; set; }
    public decimal AbsorbedHours { get; set; }
    public string? OriginalDescription { get; set; }
    public string? SummarizedDescription { get; set; }
    public string Status { get; set; } = string.Empty;
}

#endregion

#region Summarize

public class TogglSummarizeRequest
{
    public string BatchId { get; set; } = string.Empty;
}

public class TogglSummaryResult
{
    public string BatchId { get; set; } = string.Empty;
    public List<TogglSummaryGroup> Groups { get; set; } = new();
}

public class TogglSummaryGroup
{
    public string GroupKey { get; set; } = string.Empty;
    public string? ProjectName { get; set; }
    public string? CustomerName { get; set; }
    public List<TogglSummaryLine> Lines { get; set; } = new();
}

public class TogglSummaryLine
{
    public string BillingGroupKey { get; set; } = string.Empty;
    public string SummarizedDescription { get; set; } = string.Empty;
    public decimal TotalHours { get; set; }
    public List<string> EntryIds { get; set; } = new();
    public List<string> OriginalDescriptions { get; set; } = new();
}

#endregion

#region Edit and Approve

public class TogglSummaryEdit
{
    public string EntryId { get; set; } = string.Empty;
    public string NewSummary { get; set; } = string.Empty;
}

public class TogglApproveRequest
{
    public string BatchId { get; set; } = string.Empty;
    public List<string> EntryIds { get; set; } = new();
}

public class TogglApproveResult
{
    public int EntriesApproved { get; set; }
    public int TimeEntriesCreated { get; set; }
}

public class TogglProjectMapping
{
    public string TogglProjectName { get; set; } = string.Empty;
    public string WrigleyProjectCode { get; set; } = string.Empty;
    public string WrigleyCustomerId { get; set; } = string.Empty;
}

#endregion

#region Invoice Preview

public class TogglInvoicePreviewRequest
{
    public string BatchId { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
}

public class TogglInvoicePreview
{
    public string BatchId { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public List<TogglCustomerInvoicePreview> Customers { get; set; } = new();
    public decimal GrandTotal { get; set; }
}

public class TogglCustomerInvoicePreview
{
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public bool Selected { get; set; } = true;
    public List<TogglInvoiceLine> Lines { get; set; } = new();
    public decimal TotalHours { get; set; }
    public decimal TotalAmount { get; set; }
}

public class TogglInvoiceLine
{
    public string BillingGroupKey { get; set; } = string.Empty;
    public string ProjectCode { get; set; } = string.Empty;
    public string ItemCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? FormattedDescription { get; set; }
    public decimal Hours { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount => Math.Round(Hours * Rate, 2);
    public DateTime? ServiceDate { get; set; }
    public List<string> OriginalDescriptions { get; set; } = new();
    public List<string> EntryIds { get; set; } = new();
}

#endregion

#region Post Invoices

public class TogglPostInvoicesRequest
{
    public string BatchId { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public List<string> SelectedCustomerIds { get; set; } = new();
}

public class TogglPostInvoicesResult
{
    public int InvoicesCreated { get; set; }
    public List<TogglPostedInvoice> Invoices { get; set; } = new();
}

public class TogglPostedInvoice
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
}

#endregion
