using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BillingSys.Shared.Models;
using Microsoft.Extensions.Logging;

namespace BillingSys.Functions.Services;

public class QuickBooksService
{
    private readonly HttpClient _httpClient;
    private readonly TableStorageService _storage;
    private readonly ILogger<QuickBooksService> _logger;
    private readonly string _realmId;
    private readonly string _clientId;
    private readonly string _clientSecret;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public QuickBooksService(
        HttpClient httpClient,
        TableStorageService storage,
        ILogger<QuickBooksService> logger,
        string realmId,
        string clientId,
        string clientSecret)
    {
        _httpClient = httpClient;
        _storage = storage;
        _logger = logger;
        _realmId = realmId;
        _clientId = clientId;
        _clientSecret = clientSecret;
    }

    #region Public Methods

    public async Task<ServiceResult<string>> CreateInvoiceAsync(Invoice invoice, string accessToken)
    {
        try
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                return ServiceResult<string>.Fail("Access token is required");
            }

            var qboInvoice = MapToQboInvoice(invoice);
            
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var url = $"https://quickbooks.api.intuit.com/v3/company/{_realmId}/invoice?minorversion=65";
            var response = await _httpClient.PostAsJsonAsync(url, qboInvoice, JsonOptions);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("QBO invoice creation failed: {StatusCode} - {Error}", response.StatusCode, errorContent);
                return ServiceResult<string>.Fail($"QBO API error: {response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<QboInvoiceResponse>(JsonOptions);
            if (result?.Invoice?.Id == null)
            {
                return ServiceResult<string>.Fail("Failed to get invoice ID from QBO response");
            }

            invoice.QboInvoiceId = result.Invoice.Id;
            invoice.QboSyncDate = DateTime.UtcNow;
            invoice.Status = InvoiceStatus.SyncedToQbo;
            invoice.UpdatedAt = DateTime.UtcNow;
            await _storage.UpsertInvoiceAsync(invoice);

            _logger.LogInformation("Created QBO invoice {QboInvoiceId} for invoice {InvoiceNumber}", 
                result.Invoice.Id, invoice.InvoiceNumber);

            return ServiceResult<string>.Ok(result.Invoice.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating QBO invoice for {InvoiceNumber}", invoice.InvoiceNumber);
            return ServiceResult<string>.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult<BatchResult>> SyncInvoicesToQboAsync(List<Invoice> invoices, string accessToken)
    {
        var batchResult = new BatchResult();

        foreach (var invoice in invoices)
        {
            try
            {
                if (!string.IsNullOrEmpty(invoice.QboInvoiceId))
                {
                    _logger.LogInformation("Invoice {InvoiceNumber} already synced to QBO", invoice.InvoiceNumber);
                    continue;
                }

                var result = await CreateInvoiceAsync(invoice, accessToken);
                if (result.Success)
                {
                    batchResult.SuccessCount++;
                }
                else
                {
                    batchResult.FailureCount++;
                    batchResult.Errors.Add($"{invoice.InvoiceNumber}: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                batchResult.FailureCount++;
                batchResult.Errors.Add($"{invoice.InvoiceNumber}: {ex.Message}");
                _logger.LogError(ex, "Error syncing invoice {InvoiceNumber}", invoice.InvoiceNumber);
            }
        }

        return ServiceResult<BatchResult>.Ok(batchResult);
    }

    public async Task<ServiceResult<string>> GetCustomerIdByNameAsync(string customerName, string accessToken)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            
            var query = Uri.EscapeDataString($"SELECT * FROM Customer WHERE DisplayName = '{customerName}'");
            var url = $"https://quickbooks.api.intuit.com/v3/company/{_realmId}/query?query={query}&minorversion=65";
            
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return ServiceResult<string>.Fail($"QBO API error: {response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<QboQueryResponse>(JsonOptions);
            var customer = result?.QueryResponse?.Customer?.FirstOrDefault();
            
            return customer?.Id != null 
                ? ServiceResult<string>.Ok(customer.Id) 
                : ServiceResult<string>.Fail("Customer not found in QBO");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying QBO customer");
            return ServiceResult<string>.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult<string>> GetItemIdByNameAsync(string itemName, string accessToken)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            
            var query = Uri.EscapeDataString($"SELECT * FROM Item WHERE Name = '{itemName}'");
            var url = $"https://quickbooks.api.intuit.com/v3/company/{_realmId}/query?query={query}&minorversion=65";
            
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return ServiceResult<string>.Fail($"QBO API error: {response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<QboQueryResponse>(JsonOptions);
            var item = result?.QueryResponse?.Item?.FirstOrDefault();
            
            return item?.Id != null 
                ? ServiceResult<string>.Ok(item.Id) 
                : ServiceResult<string>.Fail("Item not found in QBO");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying QBO item");
            return ServiceResult<string>.Fail(ex.Message);
        }
    }

    #endregion

    #region Private Methods

    private QboInvoice MapToQboInvoice(Invoice invoice)
    {
        var lines = invoice.Lines.Select((line, index) => new QboLine
        {
            LineNum = index + 1,
            Amount = line.ExtendedPrice,
            DetailType = "SalesItemLineDetail",
            Description = line.Description,
            SalesItemLineDetail = new QboSalesItemLineDetail
            {
                ItemRef = new QboRef { Value = line.ItemCode },
                Qty = line.Quantity,
                UnitPrice = line.Price,
                ServiceDate = line.ServiceDate?.ToString("yyyy-MM-dd")
            }
        }).ToList();

        return new QboInvoice
        {
            CustomerRef = new QboRef { Value = invoice.CustomerId },
            TxnDate = invoice.InvoiceDate.ToString("yyyy-MM-dd"),
            DueDate = invoice.InvoiceDate.AddDays(30).ToString("yyyy-MM-dd"),
            DocNumber = invoice.InvoiceNumber,
            CustomerMemo = new QboMemoRef { Value = invoice.PurchaseOrderNumber ?? "" },
            Line = lines
        };
    }

    #endregion
}

#region QBO Models

public class QboInvoice
{
    public QboRef? CustomerRef { get; set; }
    public string? TxnDate { get; set; }
    public string? DueDate { get; set; }
    public string? DocNumber { get; set; }
    public QboMemoRef? CustomerMemo { get; set; }
    public List<QboLine>? Line { get; set; }
    public string? Id { get; set; }
}

public class QboRef
{
    public string? Value { get; set; }
    public string? Name { get; set; }
}

public class QboMemoRef
{
    public string? Value { get; set; }
}

public class QboLine
{
    public int LineNum { get; set; }
    public decimal Amount { get; set; }
    public string? DetailType { get; set; }
    public string? Description { get; set; }
    public QboSalesItemLineDetail? SalesItemLineDetail { get; set; }
}

public class QboSalesItemLineDetail
{
    public QboRef? ItemRef { get; set; }
    public decimal Qty { get; set; }
    public decimal UnitPrice { get; set; }
    public string? ServiceDate { get; set; }
}

public class QboInvoiceResponse
{
    public QboInvoice? Invoice { get; set; }
}

public class QboQueryResponse
{
    public QboQueryResponseInner? QueryResponse { get; set; }
}

public class QboQueryResponseInner
{
    public List<QboCustomer>? Customer { get; set; }
    public List<QboItem>? Item { get; set; }
}

public class QboCustomer
{
    public string? Id { get; set; }
    public string? DisplayName { get; set; }
}

public class QboItem
{
    public string? Id { get; set; }
    public string? Name { get; set; }
}

#endregion
