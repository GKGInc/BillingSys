using System.Net;
using System.Text.Json;
using BillingSys.Functions.Repositories;
using BillingSys.Functions.Services;
using BillingSys.Functions.Validators;
using BillingSys.Shared.Enums;
using BillingSys.Shared.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace BillingSys.Functions.Functions;

public class ReferenceFunctions
{
    private readonly IEmployeeRepository _employees;
    private readonly ICustomerRepository _customers;
    private readonly IServiceItemRepository _serviceItems;
    private readonly ISystemConfigRepository _systemConfig;
    private readonly AuthorizationService _authService;
    private readonly ILogger<ReferenceFunctions> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ReferenceFunctions(
        IEmployeeRepository employees,
        ICustomerRepository customers,
        IServiceItemRepository serviceItems,
        ISystemConfigRepository systemConfig,
        AuthorizationService authService,
        ILogger<ReferenceFunctions> logger)
    {
        _employees = employees;
        _customers = customers;
        _serviceItems = serviceItems;
        _systemConfig = systemConfig;
        _authService = authService;
        _logger = logger;
    }

    #region Employees

    [Function("GetEmployees")]
    public async Task<HttpResponseData> GetEmployees(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "employees")] HttpRequestData req)
    {
        var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        var activeOnly = !bool.TryParse(query["includeInactive"], out var include) || !include;

        var result = await _employees.GetAllAsync(activeOnly);

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
        var result = await _employees.GetAsync(id);

        var response = req.CreateResponse();
        await response.WriteAsJsonAsync(result);
        response.StatusCode = result.Success ? HttpStatusCode.OK : HttpStatusCode.NotFound;
        return response;
    }

    [Function("UpsertEmployee")]
    public async Task<HttpResponseData> UpsertEmployee(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "employees")] HttpRequestData req)
    {
        var authResult = await _authService.AuthorizeAsync(req, UserRole.Admin);
        if (!authResult.IsAuthorized) return await authResult.ToResponseAsync(req);

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

            var validator = new EmployeeValidator();
            var validationResult = await validator.ValidateAsync(employee);
            if (!validationResult.IsValid)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(ServiceResult<Employee>.Fail(
                    string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage))));
                return badResponse;
            }

            var existing = await _employees.GetAsync(employee.Id);
            if (!existing.Success)
            {
                employee.CreatedAt = DateTime.UtcNow;
            }
            else
            {
                employee.CreatedAt = existing.Data!.CreatedAt;
            }
            employee.UpdatedAt = DateTime.UtcNow;

            var result = await _employees.UpsertAsync(employee);

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

        var result = await _customers.GetAllAsync(activeOnly);

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
        var result = await _customers.GetAsync(id);

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

            var validator = new CustomerValidator();
            var validationResult = await validator.ValidateAsync(customer);
            if (!validationResult.IsValid)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(ServiceResult<Customer>.Fail(
                    string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage))));
                return badResponse;
            }

            var existing = await _customers.GetAsync(customer.CustomerId);
            if (!existing.Success)
            {
                customer.CreatedAt = DateTime.UtcNow;
            }
            else
            {
                customer.CreatedAt = existing.Data!.CreatedAt;
            }
            customer.UpdatedAt = DateTime.UtcNow;

            var result = await _customers.UpsertAsync(customer);

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

        var result = await _serviceItems.GetAllAsync(activeOnly);

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
        var result = await _serviceItems.GetAsync(itemCode);

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

            var validator = new ServiceItemValidator();
            var validationResult = await validator.ValidateAsync(item);
            if (!validationResult.IsValid)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(ServiceResult<ServiceItem>.Fail(
                    string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage))));
                return badResponse;
            }

            var existing = await _serviceItems.GetAsync(item.ItemCode);
            if (!existing.Success)
            {
                item.CreatedAt = DateTime.UtcNow;
            }
            else
            {
                item.CreatedAt = existing.Data!.CreatedAt;
            }
            item.UpdatedAt = DateTime.UtcNow;

            var result = await _serviceItems.UpsertAsync(item);

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
            await _systemConfig.InitializeTablesAsync();

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
