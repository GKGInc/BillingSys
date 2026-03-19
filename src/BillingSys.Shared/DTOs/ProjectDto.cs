namespace BillingSys.Shared.DTOs;

public class CreateProjectRequest
{
    public string ProjectCode { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ServiceItemCode { get; set; } = string.Empty;
    public string? CustomerPO { get; set; }
    public string? ProgrammerId { get; set; }
    public decimal Price { get; set; }
    public decimal QuotedHours { get; set; }
    public bool PreBill { get; set; }
    public bool AddDetailToInvoice { get; set; }
}

public class UpdateProjectRequest
{
    public string ProjectCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ServiceItemCode { get; set; } = string.Empty;
    public string? CustomerPO { get; set; }
    public string? ProgrammerId { get; set; }
    public decimal Price { get; set; }
    public decimal QuotedHours { get; set; }
    public decimal AdditionalHours { get; set; }
    public bool PreBill { get; set; }
    public bool AddDetailToInvoice { get; set; }
    public string Status { get; set; } = "Active";
}

public class ProjectSummary
{
    public string ProjectCode { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? ProgrammerId { get; set; }
    public string? ProgrammerName { get; set; }
    public decimal Price { get; set; }
    public decimal QuotedHours { get; set; }
    public decimal AdditionalHours { get; set; }
    public decimal BilledHours { get; set; }
    public decimal RemainingHours { get; set; }
    public decimal ActualHoursWorked { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class ProjectFilterRequest
{
    public string? CustomerId { get; set; }
    public string? ProgrammerId { get; set; }
    public string? Status { get; set; }
    public bool? HasRemainingHours { get; set; }
}
