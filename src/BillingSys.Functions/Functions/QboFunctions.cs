using System.Net;
using System.Text.Json;
using BillingSys.Functions.Repositories;
using BillingSys.Functions.Services;
using BillingSys.Shared.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace BillingSys.Functions.Functions;

public class QboFunctions
{
    private readonly IInvoiceRepository _invoices;
    private readonly TableStorageService _storage;
    private readonly ILogger<QboFunctions> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public QboFunctions(IInvoiceRepository invoices, TableStorageService storage, ILogger<QboFunctions> logger)
    {
        _invoices = invoices;
        _storage = storage;
        _logger = logger;
    }

    [Function("SyncInvoiceToQbo")]
    public async Task<HttpResponseData> SyncInvoiceToQbo(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "qbo/sync/invoice")] HttpRequestData req)
    {
        try
        {
            var body = await req.ReadAsStringAsync();
            var request = JsonSerializer.Deserialize<SyncInvoiceRequest>(body!, JsonOptions);
            if (request == null || string.IsNullOrEmpty(request.InvoiceNumber))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(ServiceResult<string>.Fail("Invoice number is required"));
                return badResponse;
            }

            var accessToken = req.Headers.TryGetValues("X-QBO-Access-Token", out var tokenValues) 
                ? tokenValues.FirstOrDefault() 
                : null;

            if (string.IsNullOrEmpty(accessToken))
            {
                var authResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                await authResponse.WriteAsJsonAsync(ServiceResult<string>.Fail("QBO access token is required"));
                return authResponse;
            }

            var invoiceResult = await _invoices.GetAsync(request.YearMonth, request.InvoiceNumber);
            if (!invoiceResult.Success || invoiceResult.Data == null)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteAsJsonAsync(invoiceResult);
                return notFoundResponse;
            }

            var qboService = CreateQboService();
            var result = await qboService.CreateInvoiceAsync(invoiceResult.Data, accessToken);

            var response = req.CreateResponse();
            await response.WriteAsJsonAsync(result);
            response.StatusCode = result.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing invoice to QBO");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(ServiceResult<string>.Fail(ex.Message));
            return errorResponse;
        }
    }

    [Function("SyncInvoicesToQbo")]
    public async Task<HttpResponseData> SyncInvoicesToQbo(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "qbo/sync/invoices")] HttpRequestData req)
    {
        try
        {
            var body = await req.ReadAsStringAsync();
            var request = JsonSerializer.Deserialize<SyncInvoicesRequest>(body!, JsonOptions);
            if (request == null || !request.InvoiceIds.Any())
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(ServiceResult<BatchResult>.Fail("Invoice IDs are required"));
                return badResponse;
            }

            var accessToken = req.Headers.TryGetValues("X-QBO-Access-Token", out var tokenValues) 
                ? tokenValues.FirstOrDefault() 
                : null;

            if (string.IsNullOrEmpty(accessToken))
            {
                var authResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                await authResponse.WriteAsJsonAsync(ServiceResult<BatchResult>.Fail("QBO access token is required"));
                return authResponse;
            }

            var invoices = new List<Invoice>();
            foreach (var invoiceId in request.InvoiceIds)
            {
                var result = await _invoices.GetAsync(invoiceId.YearMonth, invoiceId.InvoiceNumber);
                if (result.Success && result.Data != null)
                {
                    invoices.Add(result.Data);
                }
            }

            var qboService = CreateQboService();
            var batchResult = await qboService.SyncInvoicesToQboAsync(invoices, accessToken);

            var response = req.CreateResponse();
            await response.WriteAsJsonAsync(batchResult);
            response.StatusCode = HttpStatusCode.OK;
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing invoices to QBO");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(ServiceResult<BatchResult>.Fail(ex.Message));
            return errorResponse;
        }
    }

    [Function("GetQboCustomer")]
    public async Task<HttpResponseData> GetQboCustomer(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "qbo/customer/{customerName}")] HttpRequestData req,
        string customerName)
    {
        try
        {
            var accessToken = req.Headers.TryGetValues("X-QBO-Access-Token", out var tokenValues) 
                ? tokenValues.FirstOrDefault() 
                : null;

            if (string.IsNullOrEmpty(accessToken))
            {
                var authResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                await authResponse.WriteAsJsonAsync(ServiceResult<string>.Fail("QBO access token is required"));
                return authResponse;
            }

            var qboService = CreateQboService();
            var result = await qboService.GetCustomerIdByNameAsync(customerName, accessToken);

            var response = req.CreateResponse();
            await response.WriteAsJsonAsync(result);
            response.StatusCode = result.Success ? HttpStatusCode.OK : HttpStatusCode.NotFound;
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting QBO customer");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(ServiceResult<string>.Fail(ex.Message));
            return errorResponse;
        }
    }

    private QuickBooksService CreateQboService()
    {
        var realmId = Environment.GetEnvironmentVariable("QBO_REALM_ID") ?? "";
        var clientId = Environment.GetEnvironmentVariable("QBO_CLIENT_ID") ?? "";
        var clientSecret = Environment.GetEnvironmentVariable("QBO_CLIENT_SECRET") ?? "";

        return new QuickBooksService(
            new HttpClient(),
            _storage,
            _logger as ILogger<QuickBooksService> ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<QuickBooksService>.Instance,
            realmId,
            clientId,
            clientSecret
        );
    }
}

public class SyncInvoiceRequest
{
    public string YearMonth { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
}

public class SyncInvoicesRequest
{
    public List<SyncInvoiceRequest> InvoiceIds { get; set; } = new();
}
