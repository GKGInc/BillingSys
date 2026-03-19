using System.Net;
using System.Text.Json;
using BillingSys.Functions.Services;
using BillingSys.Shared.DTOs;
using BillingSys.Shared.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace BillingSys.Functions.Functions;

public class EdiFunctions
{
    private readonly EdiService _ediService;
    private readonly BillingService _billingService;
    private readonly TableStorageService _storage;
    private readonly ILogger<EdiFunctions> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public EdiFunctions(EdiService ediService, BillingService billingService, TableStorageService storage, ILogger<EdiFunctions> logger)
    {
        _ediService = ediService;
        _billingService = billingService;
        _storage = storage;
        _logger = logger;
    }

    [Function("GetEdiTradingPartners")]
    public async Task<HttpResponseData> GetEdiTradingPartners(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "edi/tradingpartners")] HttpRequestData req)
    {
        var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        var year = int.TryParse(query["year"], out var y) ? y : DateTime.Today.Year;
        var month = int.TryParse(query["month"], out var m) ? m : DateTime.Today.Month;

        var result = await _ediService.GetTradingPartnersByMonthAsync(year, month);
        
        var response = req.CreateResponse();
        await response.WriteAsJsonAsync(result);
        response.StatusCode = result.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
        return response;
    }

    [Function("GetEdiBillingPreview")]
    public async Task<HttpResponseData> GetEdiBillingPreview(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "edi/billing/preview")] HttpRequestData req)
    {
        var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        var year = int.TryParse(query["year"], out var y) ? y : DateTime.Today.Year;
        var month = int.TryParse(query["month"], out var m) ? m : DateTime.Today.Month;

        var ediCustomers = GetEdiCustomers();
        var result = await _ediService.GetEdiMonthlyBillingPreviewAsync(year, month, ediCustomers);
        
        var response = req.CreateResponse();
        await response.WriteAsJsonAsync(result);
        response.StatusCode = result.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
        return response;
    }

    [Function("ProcessEdiBilling")]
    public async Task<HttpResponseData> ProcessEdiBilling(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "edi/billing/process")] HttpRequestData req)
    {
        try
        {
            var body = await req.ReadAsStringAsync();
            var request = JsonSerializer.Deserialize<ProcessEdiBillingRequest>(body!, JsonOptions);
            if (request == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(ServiceResult<List<Invoice>>.Fail("Invalid request body"));
                return badResponse;
            }

            var ediCustomers = GetEdiCustomers();
            var previewResult = await _ediService.GetEdiMonthlyBillingPreviewAsync(request.Year, request.Month, ediCustomers);
            if (!previewResult.Success)
            {
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteAsJsonAsync(ServiceResult<List<Invoice>>.Fail(previewResult.ErrorMessage ?? "Failed to get preview"));
                return errorResponse;
            }

            var selectedPreviews = previewResult.Data!
                .Where(p => request.SelectedSiteIds.Contains(p.SiteId))
                .ToList();

            var invoices = new List<Invoice>();

            foreach (var preview in selectedPreviews)
            {
                var invoice = await CreateEdiInvoiceAsync(preview, request.InvoiceDate, request.Year, request.Month);
                if (invoice != null)
                {
                    invoices.Add(invoice);
                }
            }

            var response = req.CreateResponse();
            await response.WriteAsJsonAsync(ServiceResult<List<Invoice>>.Ok(invoices));
            response.StatusCode = HttpStatusCode.OK;
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing EDI billing");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(ServiceResult<List<Invoice>>.Fail(ex.Message));
            return errorResponse;
        }
    }

    #region Private Methods

    private async Task<Invoice?> CreateEdiInvoiceAsync(EdiCustomerBillingPreview preview, DateTime invoiceDate, int year, int month)
    {
        try
        {
            var invoiceNumberResult = await _storage.GetNextInvoiceNumberAsync(invoiceDate);
            if (!invoiceNumberResult.Success)
            {
                _logger.LogError("Failed to get invoice number for EDI customer {CustomerId}", preview.CustomerId);
                return null;
            }

            var invoice = new Invoice
            {
                InvoiceNumber = invoiceNumberResult.Data!,
                InvoiceDate = invoiceDate,
                CustomerId = preview.CustomerId,
                CustomerName = preview.CustomerName,
                PurchaseOrderNumber = "Verbal",
                OrderNumber = "None",
                Status = InvoiceStatus.Posted,
                CreatedAt = DateTime.UtcNow
            };

            var lineNumber = 0;
            var monthYear = $"{month:D2}/{year}";

            if (preview.MinimumFee > 0)
            {
                lineNumber++;
                invoice.Lines.Add(new InvoiceLine
                {
                    InvoiceNumber = invoice.InvoiceNumber,
                    LineNumber = lineNumber,
                    ItemCode = "EDI-SERVICECHG",
                    Description = $"EDI FOR {monthYear}",
                    UnitOfMeasure = "EACH",
                    Quantity = 1,
                    Price = preview.MinimumFee
                });
            }
            else
            {
                if (preview.EdiTradingPartnerCount > 0)
                {
                    lineNumber++;
                    invoice.Lines.Add(new InvoiceLine
                    {
                        InvoiceNumber = invoice.InvoiceNumber,
                        LineNumber = lineNumber,
                        ItemCode = "EDI-SERVICECHG",
                        Description = $"EDI TRADING PARTNERS FOR {monthYear}",
                        UnitOfMeasure = "EACH",
                        Quantity = preview.EdiTradingPartnerCount,
                        Price = preview.EdiTradingPartnerFee,
                        Memo = preview.EdiTradingPartners
                    });
                }

                if (preview.NonEdiTradingPartnerCount > 0)
                {
                    lineNumber++;
                    invoice.Lines.Add(new InvoiceLine
                    {
                        InvoiceNumber = invoice.InvoiceNumber,
                        LineNumber = lineNumber,
                        ItemCode = "EDI-SERVICECHG",
                        Description = $"NON-EDI TRADING PARTNERS",
                        UnitOfMeasure = "EACH",
                        Quantity = preview.NonEdiTradingPartnerCount,
                        Price = preview.NonEdiTradingPartnerFee,
                        Memo = preview.NonEdiTradingPartners
                    });
                }

                if (preview.PdfCount > 0)
                {
                    lineNumber++;
                    invoice.Lines.Add(new InvoiceLine
                    {
                        InvoiceNumber = invoice.InvoiceNumber,
                        LineNumber = lineNumber,
                        ItemCode = "EDI-SERVICECHG",
                        Description = $"PDF TRADING PARTNERS",
                        UnitOfMeasure = "EACH",
                        Quantity = 1,
                        Price = preview.PdfFee,
                        Memo = preview.PdfTradingPartners
                    });
                }

                if (preview.CatalogTradingPartnerCount > 0)
                {
                    lineNumber++;
                    invoice.Lines.Add(new InvoiceLine
                    {
                        InvoiceNumber = invoice.InvoiceNumber,
                        LineNumber = lineNumber,
                        ItemCode = "EDI-SERVICECHG",
                        Description = $"CATALOG TRADING PARTNERS",
                        UnitOfMeasure = "EACH",
                        Quantity = preview.CatalogTradingPartnerCount,
                        Price = preview.CatalogFee,
                        Memo = preview.CatalogTradingPartners
                    });
                }

                if (preview.KilocharAmount > 0)
                {
                    lineNumber++;
                    invoice.Lines.Add(new InvoiceLine
                    {
                        InvoiceNumber = invoice.InvoiceNumber,
                        LineNumber = lineNumber,
                        ItemCode = "EDI-DATACTIVITY",
                        Description = "DATA ACTIVITY (PER KILOCHAR)",
                        UnitOfMeasure = "EACH",
                        Quantity = preview.KilocharQuantity,
                        Price = preview.KilocharRate
                    });
                }

                if (preview.MailboxFee > 0)
                {
                    lineNumber++;
                    invoice.Lines.Add(new InvoiceLine
                    {
                        InvoiceNumber = invoice.InvoiceNumber,
                        LineNumber = lineNumber,
                        ItemCode = "EDI-MAILBOXFEE",
                        Description = "MAILBOX FEE",
                        UnitOfMeasure = "EACH",
                        Quantity = 1,
                        Price = preview.MailboxFee
                    });
                }
            }

            invoice.InvoiceAmount = invoice.Lines.Sum(l => l.ExtendedPrice);

            var result = await _storage.UpsertInvoiceAsync(invoice);
            if (!result.Success)
            {
                _logger.LogError("Failed to save EDI invoice for customer {CustomerId}: {Error}", 
                    preview.CustomerId, result.ErrorMessage);
                return null;
            }

            return invoice;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating EDI invoice for customer {CustomerId}", preview.CustomerId);
            return null;
        }
    }

    private static List<EdiCustomer> GetEdiCustomers()
    {
        return new List<EdiCustomer>
        {
            new() { SiteId = "10272", CustomerId = "10272", CustomerName = "Patchology", EdiTradingPartnerFee = 50, NonEdiTradingPartnerFee = 25, KilocharRate = 0.001m, KilocharBillingType = "Y" },
            new() { SiteId = "08847", CustomerId = "08847", CustomerName = "Coast To Coast", EdiTradingPartnerFee = 50, NonEdiTradingPartnerFee = 25, KilocharRate = 0.001m, KilocharBillingType = "Y" },
            new() { SiteId = "06353", CustomerId = "06353", CustomerName = "Tanya Creations", EdiTradingPartnerFee = 50, KilocharRate = 0.001m, KilocharBillingType = "Y" }
        };
    }

    #endregion
}

public class ProcessEdiBillingRequest
{
    public int Year { get; set; }
    public int Month { get; set; }
    public DateTime InvoiceDate { get; set; }
    public List<string> SelectedSiteIds { get; set; } = new();
}
