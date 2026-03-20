namespace BillingSys.Shared.Models;

/// <summary>
/// System-wide configuration settings stored in the database.
/// Uses a singleton pattern with a fixed ID to ensure only one record exists.
/// </summary>
public class SystemConfig : AuditableEntity
{
    #region Constants

    /// <summary>
    /// The fixed ID for the singleton SystemConfig record
    /// </summary>
    public const string SingletonId = "SYSTEM_CONFIG";

    #endregion

    #region Identity

    public string Id { get; set; } = SingletonId;

    #endregion

    #region Company Information

    public string CompanyName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }

    #endregion

    #region Billing Settings

    public int DefaultPaymentNetDays { get; set; } = 30;
    public string DefaultPaymentTerms { get; set; } = "Net 30";
    public string InvoiceNumberPrefix { get; set; } = "";
    public int InvoiceNumberPadding { get; set; } = 6;

    #endregion

    #region Integration Settings

    public bool QuickBooksIntegrationEnabled { get; set; } = true;
    public string? QuickBooksRealmId { get; set; }
    public string? DefaultIncomeAccount { get; set; }

    #endregion

    #region EDI Settings

    public decimal DefaultEdiTradingPartnerFee { get; set; }
    public decimal DefaultNonEdiTradingPartnerFee { get; set; }
    public decimal DefaultPdfFee { get; set; }
    public decimal DefaultKilocharRate { get; set; }

    #endregion

    #region Feature Flags

    public bool EnableTimeEntryApproval { get; set; } = true;
    public bool EnableProjectBilling { get; set; } = true;
    public bool EnableEdiBilling { get; set; } = true;

    #endregion

    #region Singleton Factory

    /// <summary>
    /// Creates a new SystemConfig with default values
    /// </summary>
    public static SystemConfig CreateDefault()
    {
        var config = new SystemConfig
        {
            Id = SingletonId
        };
        config.StampCreated();
        return config;
    }

    #endregion
}
