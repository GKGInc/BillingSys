namespace BillingSys.Shared.Models;

public class Customer
{
    public string CustomerId { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string PaymentTerms { get; set; } = "Net 30";
    public int PaymentNetDays { get; set; } = 30;
    public string? PrintCode { get; set; }
    public string? EmailCode { get; set; }
    public string? QboCustomerId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastSaleDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
