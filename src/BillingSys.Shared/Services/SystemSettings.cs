using BillingSys.Shared.Models;

namespace BillingSys.Shared.Services;

/// <summary>
/// Static wrapper for system-wide settings.
/// Provides easy access to configuration values with sensible defaults.
/// Must be initialized by calling Load() at application startup.
/// </summary>
public static class SystemSettings
{
    #region Private Fields

    private static SystemConfig? _config;
    private static readonly object _lock = new();

    #endregion

    #region Initialization

    /// <summary>
    /// Loads or reloads the system configuration
    /// </summary>
    public static void Load(SystemConfig config)
    {
        lock (_lock)
        {
            _config = config;
        }
    }

    /// <summary>
    /// Clears the cached configuration (useful for testing)
    /// </summary>
    public static void Clear()
    {
        lock (_lock)
        {
            _config = null;
        }
    }

    /// <summary>
    /// Indicates if settings have been loaded
    /// </summary>
    public static bool IsLoaded => _config != null;

    #endregion

    #region Company Information

    public static string CompanyName => _config?.CompanyName ?? "Tech85";
    public static string? Address => _config?.Address;
    public static string? City => _config?.City;
    public static string? State => _config?.State;
    public static string? ZipCode => _config?.ZipCode;
    public static string? Phone => _config?.Phone;
    public static string? Email => _config?.Email;

    public static string FullAddress
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(Address)) parts.Add(Address);
            var cityStateZip = string.Join(", ", 
                new[] { City, State }.Where(s => !string.IsNullOrEmpty(s)));
            if (!string.IsNullOrEmpty(ZipCode))
                cityStateZip = string.IsNullOrEmpty(cityStateZip) ? ZipCode : $"{cityStateZip} {ZipCode}";
            if (!string.IsNullOrEmpty(cityStateZip)) parts.Add(cityStateZip);
            return string.Join("\n", parts);
        }
    }

    #endregion

    #region Billing Settings

    public static int DefaultPaymentNetDays => _config?.DefaultPaymentNetDays ?? 30;
    public static string DefaultPaymentTerms => _config?.DefaultPaymentTerms ?? "Net 30";
    public static string InvoiceNumberPrefix => _config?.InvoiceNumberPrefix ?? "";
    public static int InvoiceNumberPadding => _config?.InvoiceNumberPadding ?? 6;

    /// <summary>
    /// Formats an invoice number with the configured prefix and padding
    /// </summary>
    public static string FormatInvoiceNumber(int sequenceNumber)
    {
        var paddedNumber = sequenceNumber.ToString().PadLeft(InvoiceNumberPadding, '0');
        return $"{InvoiceNumberPrefix}{paddedNumber}";
    }

    #endregion

    #region Integration Settings

    public static bool QuickBooksIntegrationEnabled => _config?.QuickBooksIntegrationEnabled ?? true;
    public static string? QuickBooksRealmId => _config?.QuickBooksRealmId;
    public static string? DefaultIncomeAccount => _config?.DefaultIncomeAccount;

    #endregion

    #region EDI Settings

    public static decimal DefaultEdiTradingPartnerFee => _config?.DefaultEdiTradingPartnerFee ?? 0;
    public static decimal DefaultNonEdiTradingPartnerFee => _config?.DefaultNonEdiTradingPartnerFee ?? 0;
    public static decimal DefaultPdfFee => _config?.DefaultPdfFee ?? 0;
    public static decimal DefaultKilocharRate => _config?.DefaultKilocharRate ?? 0.002m;

    #endregion

    #region Feature Flags

    public static bool EnableTimeEntryApproval => _config?.EnableTimeEntryApproval ?? true;
    public static bool EnableProjectBilling => _config?.EnableProjectBilling ?? true;
    public static bool EnableEdiBilling => _config?.EnableEdiBilling ?? true;

    #endregion

    #region Raw Config Access

    /// <summary>
    /// Gets the raw SystemConfig object (may be null if not loaded)
    /// </summary>
    public static SystemConfig? GetConfig() => _config;

    #endregion
}
