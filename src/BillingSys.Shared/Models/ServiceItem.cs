namespace BillingSys.Shared.Models;

public class ServiceItem
{
    public string ItemCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? IncomeAccount { get; set; }
    public string? QboItemId { get; set; }
    public string Category { get; set; } = "Service";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
