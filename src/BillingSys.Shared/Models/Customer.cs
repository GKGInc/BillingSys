namespace BillingSys.Shared.Models;

public class Customer : AuditableEntity
{
    #region Identity

    public string CustomerId { get; set; } = string.Empty;

    #endregion

    #region Contact Information

    public string Company { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }

    #endregion

    #region Billing Settings

    public string PaymentTerms { get; set; } = "Net 30";
    public int PaymentNetDays { get; set; } = 30;
    public string? PrintCode { get; set; }
    public string? EmailCode { get; set; }

    #endregion

    #region Integration

    public string? QboCustomerId { get; set; }

    #endregion

    #region Status

    public bool IsActive { get; set; } = true;
    public DateTime? LastSaleDate { get; set; }

    #endregion
}
