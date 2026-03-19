using System.Net;
using System.Text.Json;
using BillingSys.Functions.Services;
using BillingSys.Shared.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace BillingSys.Functions.Functions;

public class ReferenceFunctions
{
    private readonly TableStorageService _storage;
    private readonly ILogger<ReferenceFunctions> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ReferenceFunctions(TableStorageService storage, ILogger<ReferenceFunctions> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    #region Employees

    [Function("GetEmployees")]
    public async Task<HttpResponseData> GetEmployees(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "employees")] HttpRequestData req)
    {
        var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        var activeOnly = !bool.TryParse(query["includeInactive"], out var include) || !include;

        var result = await _storage.GetAllEmployeesAsync(activeOnly);
        
        var response = req.CreateResponse();
        await response.WriteAsJsonAsync(result);
        response.StatusCode = result.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
        return response;
    }

    [Function("GetEmployee")]
    public async Task<HttpResponseData> GetEmployee(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "employees/{id}")] HttpRequestData req,
        string id)
    {
        var result = await _storage.GetEmployeeAsync(id);
        
        var response = req.CreateResponse();
        await response.WriteAsJsonAsync(result);
        response.StatusCode = result.Success ? HttpStatusCode.OK : HttpStatusCode.NotFound;
        return response;
    }

    [Function("UpsertEmployee")]
    public async Task<HttpResponseData> UpsertEmployee(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "employees")] HttpRequestData req)
    {
        try
        {
            var body = await req.ReadAsStringAsync();
            var employee = JsonSerializer.Deserialize<Employee>(body!, JsonOptions);
            if (employee == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(ServiceResult<Employee>.Fail("Invalid request body"));
                return badResponse;
            }

            var existing = await _storage.GetEmployeeAsync(employee.Id);
            if (!existing.Success)
            {
                employee.CreatedAt = DateTime.UtcNow;
            }
            else
            {
                employee.CreatedAt = existing.Data!.CreatedAt;
            }
            employee.UpdatedAt = DateTime.UtcNow;

            var result = await _storage.UpsertEmployeeAsync(employee);
            
            var response = req.CreateResponse();
            await response.WriteAsJsonAsync(result);
            response.StatusCode = result.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting employee");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(ServiceResult<Employee>.Fail(ex.Message));
            return errorResponse;
        }
    }

    #endregion

    #region Customers

    [Function("GetCustomers")]
    public async Task<HttpResponseData> GetCustomers(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "customers")] HttpRequestData req)
    {
        var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        var activeOnly = !bool.TryParse(query["includeInactive"], out var include) || !include;

        var result = await _storage.GetAllCustomersAsync(activeOnly);
        
        var response = req.CreateResponse();
        await response.WriteAsJsonAsync(result);
        response.StatusCode = result.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
        return response;
    }

    [Function("GetCustomer")]
    public async Task<HttpResponseData> GetCustomer(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "customers/{id}")] HttpRequestData req,
        string id)
    {
        var result = await _storage.GetCustomerAsync(id);
        
        var response = req.CreateResponse();
        await response.WriteAsJsonAsync(result);
        response.StatusCode = result.Success ? HttpStatusCode.OK : HttpStatusCode.NotFound;
        return response;
    }

    [Function("UpsertCustomer")]
    public async Task<HttpResponseData> UpsertCustomer(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "customers")] HttpRequestData req)
    {
        try
        {
            var body = await req.ReadAsStringAsync();
            var customer = JsonSerializer.Deserialize<Customer>(body!, JsonOptions);
            if (customer == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(ServiceResult<Customer>.Fail("Invalid request body"));
                return badResponse;
            }

            var existing = await _storage.GetCustomerAsync(customer.CustomerId);
            if (!existing.Success)
            {
                customer.CreatedAt = DateTime.UtcNow;
            }
            else
            {
                customer.CreatedAt = existing.Data!.CreatedAt;
            }
            customer.UpdatedAt = DateTime.UtcNow;

            var result = await _storage.UpsertCustomerAsync(customer);
            
            var response = req.CreateResponse();
            await response.WriteAsJsonAsync(result);
            response.StatusCode = result.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting customer");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(ServiceResult<Customer>.Fail(ex.Message));
            return errorResponse;
        }
    }

    #endregion

    #region Service Items

    [Function("GetServiceItems")]
    public async Task<HttpResponseData> GetServiceItems(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "serviceitems")] HttpRequestData req)
    {
        var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        var activeOnly = !bool.TryParse(query["includeInactive"], out var include) || !include;

        var result = await _storage.GetAllServiceItemsAsync(activeOnly);
        
        var response = req.CreateResponse();
        await response.WriteAsJsonAsync(result);
        response.StatusCode = result.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
        return response;
    }

    [Function("GetServiceItem")]
    public async Task<HttpResponseData> GetServiceItem(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "serviceitems/{itemCode}")] HttpRequestData req,
        string itemCode)
    {
        var result = await _storage.GetServiceItemAsync(itemCode);
        
        var response = req.CreateResponse();
        await response.WriteAsJsonAsync(result);
        response.StatusCode = result.Success ? HttpStatusCode.OK : HttpStatusCode.NotFound;
        return response;
    }

    [Function("UpsertServiceItem")]
    public async Task<HttpResponseData> UpsertServiceItem(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "serviceitems")] HttpRequestData req)
    {
        try
        {
            var body = await req.ReadAsStringAsync();
            var item = JsonSerializer.Deserialize<ServiceItem>(body!, JsonOptions);
            if (item == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(ServiceResult<ServiceItem>.Fail("Invalid request body"));
                return badResponse;
            }

            var existing = await _storage.GetServiceItemAsync(item.ItemCode);
            if (!existing.Success)
            {
                item.CreatedAt = DateTime.UtcNow;
            }
            else
            {
                item.CreatedAt = existing.Data!.CreatedAt;
            }
            item.UpdatedAt = DateTime.UtcNow;

            var result = await _storage.UpsertServiceItemAsync(item);
            
            var response = req.CreateResponse();
            await response.WriteAsJsonAsync(result);
            response.StatusCode = result.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting service item");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(ServiceResult<ServiceItem>.Fail(ex.Message));
            return errorResponse;
        }
    }

    #endregion

    #region Initialization

    [Function("InitializeTables")]
    public async Task<HttpResponseData> InitializeTables(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "admin/initialize")] HttpRequestData req)
    {
        try
        {
            await _storage.InitializeTablesAsync();
            
            var response = req.CreateResponse();
            await response.WriteAsJsonAsync(ServiceResult.Ok());
            response.StatusCode = HttpStatusCode.OK;
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing tables");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(ServiceResult.Fail(ex.Message));
            return errorResponse;
        }
    }

    #endregion
}
