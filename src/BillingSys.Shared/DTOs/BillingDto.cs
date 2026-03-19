namespace BillingSys.Shared.DTOs;

public class WeeklyBillingRequest
{
    public int WeekNumber { get; set; }
    public int Year { get; set; }
    public DateTime InvoiceDate { get; set; }
}

public class WeeklyBillingPreview
{
    public int WeekNumber { get; set; }
    public int Year { get; set; }
    public List<CustomerBillingPreview> Customers { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public int TotalHours { get; set; }
}

public class CustomerBillingPreview
{
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalHours { get; set; }
    public decimal TotalAmount { get; set; }
    public bool Selected { get; set; } = true;
    public List<BillingLinePreview> Lines { get; set; } = new();
}

public class BillingLinePreview
{
    public string ItemCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Memo { get; set; }
    public decimal Hours { get; set; }
    public decimal Price { get; set; }
    public decimal Amount => Math.Round(Hours * Price, 2);
    public DateTime? ServiceDate { get; set; }
    public string? EmployeeId { get; set; }
}

public class ProjectBillingRequest
{
    public string CustomerId { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public List<ProjectBillingLine> Projects { get; set; } = new();
}

public class ProjectBillingLine
{
    public string ProjectCode { get; set; } = string.Empty;
    public decimal HoursToBill { get; set; }
}

public class CreateInvoiceRequest
{
    public DateTime InvoiceDate { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public string? PurchaseOrderNumber { get; set; }
    public string? OrderNumber { get; set; }
    public List<CreateInvoiceLineRequest> Lines { get; set; } = new();
}

public class CreateInvoiceLineRequest
{
    public string ItemCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public string? Memo { get; set; }
    public DateTime? ServiceDate { get; set; }
}
