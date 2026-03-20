namespace BillingSys.Shared.Models;

public class EdiCustomer : AuditableEntity
{
    #region Identity

    public string SiteId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;

    #endregion

    #region Configuration

    public string VanGroup { get; set; } = string.Empty;
    public bool IsMultiCompany { get; set; }

    #endregion

    #region Fee Schedule

    public decimal EdiTradingPartnerFee { get; set; }
    public decimal NonEdiTradingPartnerFee { get; set; }
    public decimal PdfFee { get; set; }
    public decimal WarehouseTradingPartnerFee { get; set; }
    public decimal CatalogTradingPartnerFee { get; set; }
    public decimal MailboxFee { get; set; }
    public decimal MinimumFee { get; set; }

    #endregion

    #region Kilochar Settings

    public string KilocharBillingType { get; set; } = "Y";
    public decimal KilocharRate { get; set; }
    public decimal KilocharMinimumDollars { get; set; }

    #endregion

    #region Status

    public bool IsActive { get; set; } = true;

    #endregion
}

public class EdiRate
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string CustomerId { get; set; } = string.Empty;
    public int KilocharFrom { get; set; }
    public int KilocharTo { get; set; }
    public decimal Rate { get; set; }
}

public class EdiTransaction
{
    public string CustomerNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string TradingPartnerCode { get; set; } = string.Empty;
    public string? TradingPartnerName { get; set; }
    public string BillType { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
}

public class EdiMonthlyBilling
{
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    
    public int EdiTradingPartnerCount { get; set; }
    public string EdiTradingPartnerList { get; set; } = string.Empty;
    
    public int NonEdiTradingPartnerCount { get; set; }
    public string NonEdiTradingPartnerList { get; set; } = string.Empty;
    
    public int PdfCount { get; set; }
    public string PdfTradingPartnerList { get; set; } = string.Empty;
    
    public int CatalogTradingPartnerCount { get; set; }
    public string CatalogTradingPartnerList { get; set; } = string.Empty;
    
    public long KilocharQuantity { get; set; }
    public decimal KilocharAmount { get; set; }
    
    public decimal TotalAmount { get; set; }
    public bool IsBilled { get; set; }
    public string? InvoiceNumber { get; set; }
}
