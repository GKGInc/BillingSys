namespace BillingSys.Shared.Models;

public class Project
{
    public string ProjectCode { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ServiceItemCode { get; set; } = string.Empty;
    public string? CustomerPO { get; set; }
    public string? ProgrammerId { get; set; }
    public decimal Price { get; set; }
    public decimal QuotedHours { get; set; }
    public decimal AdditionalHours { get; set; }
    public decimal BilledHours { get; set; }
    public bool PreBill { get; set; }
    public bool AddDetailToInvoice { get; set; }
    public ProjectStatus Status { get; set; } = ProjectStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public decimal RemainingHours => QuotedHours + AdditionalHours - BilledHours;
    public decimal TotalHours => QuotedHours + AdditionalHours;
}

public enum ProjectStatus
{
    Active,
    Completed,
    OnHold,
    Cancelled
}
