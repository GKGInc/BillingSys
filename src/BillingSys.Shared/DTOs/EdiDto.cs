namespace BillingSys.Shared.DTOs;

public class EdiMonthlyBillingRequest
{
    public int Year { get; set; }
    public int Month { get; set; }
    public DateTime InvoiceDate { get; set; }
}

public class EdiCustomerBillingPreview
{
    public string SiteId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public bool Selected { get; set; } = true;
    
    public int EdiTradingPartnerCount { get; set; }
    public decimal EdiTradingPartnerFee { get; set; }
    public string EdiTradingPartners { get; set; } = string.Empty;
    
    public int NonEdiTradingPartnerCount { get; set; }
    public decimal NonEdiTradingPartnerFee { get; set; }
    public string NonEdiTradingPartners { get; set; } = string.Empty;
    
    public int PdfCount { get; set; }
    public decimal PdfFee { get; set; }
    public string PdfTradingPartners { get; set; } = string.Empty;
    
    public int CatalogTradingPartnerCount { get; set; }
    public decimal CatalogFee { get; set; }
    public string CatalogTradingPartners { get; set; } = string.Empty;
    
    public long KilocharQuantity { get; set; }
    public decimal KilocharRate { get; set; }
    public decimal KilocharAmount { get; set; }
    
    public decimal MailboxFee { get; set; }
    public decimal MinimumFee { get; set; }
    
    public decimal TotalAmount { get; set; }
}

public class EdiTradingPartnerSummary
{
    public string CustomerNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string TradingPartnerCode { get; set; } = string.Empty;
    public string TradingPartnerName { get; set; } = string.Empty;
    public string BillType { get; set; } = string.Empty;
}

public class UpdateEdiCustomerRequest
{
    public string SiteId { get; set; } = string.Empty;
    public decimal EdiTradingPartnerFee { get; set; }
    public decimal NonEdiTradingPartnerFee { get; set; }
    public decimal PdfFee { get; set; }
    public decimal WarehouseTradingPartnerFee { get; set; }
    public decimal CatalogTradingPartnerFee { get; set; }
    public decimal MailboxFee { get; set; }
    public decimal MinimumFee { get; set; }
    public string KilocharBillingType { get; set; } = "Y";
    public decimal KilocharRate { get; set; }
    public decimal KilocharMinimumDollars { get; set; }
}
