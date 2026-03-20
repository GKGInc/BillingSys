using System.Globalization;
using System.Net;
using System.Text.Json;
using BillingSys.Functions.Repositories;
using BillingSys.Functions.Services;
using BillingSys.Shared.Enums;
using BillingSys.Shared.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace BillingSys.Functions.Functions;

public class MigrationFunctions
{
    private readonly IEmployeeRepository _employees;
    private readonly ICustomerRepository _customers;
    private readonly IProjectRepository _projects;
    private readonly IServiceItemRepository _serviceItems;
    private readonly ITimeEntryRepository _timeEntries;
    private readonly AuthorizationService _authService;
    private readonly ILogger<MigrationFunctions> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public MigrationFunctions(
        IEmployeeRepository employees,
        ICustomerRepository customers,
        IProjectRepository projects,
        IServiceItemRepository serviceItems,
        ITimeEntryRepository timeEntries,
        AuthorizationService authService,
        ILogger<MigrationFunctions> logger)
    {
        _employees = employees;
        _customers = customers;
        _projects = projects;
        _serviceItems = serviceItems;
        _timeEntries = timeEntries;
        _authService = authService;
        _logger = logger;
    }

    [Function("ImportEmployees")]
    public async Task<HttpResponseData> ImportEmployees(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "migration/employees")] HttpRequestData req)
    {
        var authResult = await _authService.AuthorizeAsync(req, UserRole.Admin);
        if (!authResult.IsAuthorized) return await authResult.ToResponseAsync(req);

        try
        {
            var body = await req.ReadAsStringAsync();
            var employees = JsonSerializer.Deserialize<List<Employee>>(body!, JsonOptions);
            if (employees == null || !employees.Any())
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(ServiceResult<BatchResult>.Fail("No employees provided"));
                return badResponse;
            }

            var batchResult = new BatchResult();
            foreach (var employee in employees)
            {
                try
                {
                    employee.CreatedAt = DateTime.UtcNow;
                    var result = await _employees.UpsertAsync(employee);
                    if (result.Success)
                    {
                        batchResult.SuccessCount++;
                        _logger.LogInformation("[Migration] Imported employee {Id}: {Name}", employee.Id, employee.Name);
                    }
                    else
                    {
                        batchResult.FailureCount++;
                        batchResult.Errors.Add($"{employee.Id}: {result.ErrorMessage}");
                    }
                }
                catch (Exception ex)
                {
                    batchResult.FailureCount++;
                    batchResult.Errors.Add($"{employee.Id}: {ex.Message}");
                }
            }

            var response = req.CreateResponse();
            await response.WriteAsJsonAsync(ServiceResult<BatchResult>.Ok(batchResult));
            response.StatusCode = HttpStatusCode.OK;
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing employees");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(ServiceResult<BatchResult>.Fail(ex.Message));
            return errorResponse;
        }
    }

    [Function("ImportCustomers")]
    public async Task<HttpResponseData> ImportCustomers(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "migration/customers")] HttpRequestData req)
    {
        var authResult = await _authService.AuthorizeAsync(req, UserRole.Admin);
        if (!authResult.IsAuthorized) return await authResult.ToResponseAsync(req);

        try
        {
            var body = await req.ReadAsStringAsync();
            var customers = JsonSerializer.Deserialize<List<Customer>>(body!, JsonOptions);
            if (customers == null || !customers.Any())
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(ServiceResult<BatchResult>.Fail("No customers provided"));
                return badResponse;
            }

            var batchResult = new BatchResult();
            foreach (var customer in customers)
            {
                try
                {
                    customer.CreatedAt = DateTime.UtcNow;
                    var result = await _customers.UpsertAsync(customer);
                    if (result.Success)
                    {
                        batchResult.SuccessCount++;
                        _logger.LogInformation("[Migration] Imported customer {Id}: {Name}", customer.CustomerId, customer.Company);
                    }
                    else
                    {
                        batchResult.FailureCount++;
                        batchResult.Errors.Add($"{customer.CustomerId}: {result.ErrorMessage}");
                    }
                }
                catch (Exception ex)
                {
                    batchResult.FailureCount++;
                    batchResult.Errors.Add($"{customer.CustomerId}: {ex.Message}");
                }
            }

            var response = req.CreateResponse();
            await response.WriteAsJsonAsync(ServiceResult<BatchResult>.Ok(batchResult));
            response.StatusCode = HttpStatusCode.OK;
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing customers");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(ServiceResult<BatchResult>.Fail(ex.Message));
            return errorResponse;
        }
    }

    [Function("ImportProjects")]
    public async Task<HttpResponseData> ImportProjects(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "migration/projects")] HttpRequestData req)
    {
        var authResult = await _authService.AuthorizeAsync(req, UserRole.Admin);
        if (!authResult.IsAuthorized) return await authResult.ToResponseAsync(req);

        try
        {
            var body = await req.ReadAsStringAsync();
            var projects = JsonSerializer.Deserialize<List<Project>>(body!, JsonOptions);
            if (projects == null || !projects.Any())
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(ServiceResult<BatchResult>.Fail("No projects provided"));
                return badResponse;
            }

            var batchResult = new BatchResult();
            foreach (var project in projects)
            {
                try
                {
                    project.CreatedAt = DateTime.UtcNow;
                    var result = await _projects.UpsertAsync(project);
                    if (result.Success)
                    {
                        batchResult.SuccessCount++;
                        _logger.LogInformation("[Migration] Imported project {Code} for customer {CustomerId}", 
                            project.ProjectCode, project.CustomerId);
                    }
                    else
                    {
                        batchResult.FailureCount++;
                        batchResult.Errors.Add($"{project.ProjectCode}: {result.ErrorMessage}");
                    }
                }
                catch (Exception ex)
                {
                    batchResult.FailureCount++;
                    batchResult.Errors.Add($"{project.ProjectCode}: {ex.Message}");
                }
            }

            var response = req.CreateResponse();
            await response.WriteAsJsonAsync(ServiceResult<BatchResult>.Ok(batchResult));
            response.StatusCode = HttpStatusCode.OK;
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing projects");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(ServiceResult<BatchResult>.Fail(ex.Message));
            return errorResponse;
        }
    }

    [Function("ImportServiceItems")]
    public async Task<HttpResponseData> ImportServiceItems(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "migration/serviceitems")] HttpRequestData req)
    {
        var authResult = await _authService.AuthorizeAsync(req, UserRole.Admin);
        if (!authResult.IsAuthorized) return await authResult.ToResponseAsync(req);

        try
        {
            var body = await req.ReadAsStringAsync();
            var items = JsonSerializer.Deserialize<List<ServiceItem>>(body!, JsonOptions);
            if (items == null || !items.Any())
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(ServiceResult<BatchResult>.Fail("No service items provided"));
                return badResponse;
            }

            var batchResult = new BatchResult();
            foreach (var item in items)
            {
                try
                {
                    item.CreatedAt = DateTime.UtcNow;
                    var result = await _serviceItems.UpsertAsync(item);
                    if (result.Success)
                    {
                        batchResult.SuccessCount++;
                        _logger.LogInformation("[Migration] Imported service item {Code}: {Desc}", item.ItemCode, item.Description);
                    }
                    else
                    {
                        batchResult.FailureCount++;
                        batchResult.Errors.Add($"{item.ItemCode}: {result.ErrorMessage}");
                    }
                }
                catch (Exception ex)
                {
                    batchResult.FailureCount++;
                    batchResult.Errors.Add($"{item.ItemCode}: {ex.Message}");
                }
            }

            var response = req.CreateResponse();
            await response.WriteAsJsonAsync(ServiceResult<BatchResult>.Ok(batchResult));
            response.StatusCode = HttpStatusCode.OK;
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing service items");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(ServiceResult<BatchResult>.Fail(ex.Message));
            return errorResponse;
        }
    }

    [Function("ImportTimeEntriesCsv")]
    public async Task<HttpResponseData> ImportTimeEntriesCsv(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "migration/timeentries/csv")] HttpRequestData req)
    {
        var authResult = await _authService.AuthorizeAsync(req, UserRole.Admin);
        if (!authResult.IsAuthorized) return await authResult.ToResponseAsync(req);

        try
        {
            var csvContent = await req.ReadAsStringAsync();
            if (string.IsNullOrEmpty(csvContent))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(ServiceResult<BatchResult>.Fail("No CSV content provided"));
                return badResponse;
            }

            var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 2)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(ServiceResult<BatchResult>.Fail("CSV must have header and data rows"));
                return badResponse;
            }

            var headers = lines[0].Split(',').Select(h => h.Trim().Trim('"').ToLower()).ToArray();
            var batchResult = new BatchResult();

            for (int i = 1; i < lines.Length; i++)
            {
                try
                {
                    var values = ParseCsvLine(lines[i]);
                    if (values.Length < headers.Length) continue;

                    var entry = new TimeEntry
                    {
                        Id = Guid.NewGuid().ToString(),
                        EmployeeId = GetValue(headers, values, "id"),
                        EmployeeName = GetValue(headers, values, "name"),
                        Date = DateTime.TryParse(GetValue(headers, values, "date"), out var d) ? d : DateTime.Today,
                        Hours = decimal.TryParse(GetValue(headers, values, "hours"), out var h) ? h : 0,
                        Billable = GetValue(headers, values, "billable").ToLower() == "true" || GetValue(headers, values, "billable") == "1",
                        ProjectCode = GetValue(headers, values, "project"),
                        Comments = GetValue(headers, values, "comments"),
                        Status = TimeEntryStatus.Pending,
                        CreatedAt = DateTime.UtcNow
                    };

                    if (TimeSpan.TryParse(GetValue(headers, values, "start_time"), out var start))
                        entry.StartTime = start;
                    if (TimeSpan.TryParse(GetValue(headers, values, "end_time"), out var end))
                        entry.EndTime = end;
                    if (int.TryParse(GetValue(headers, values, "miles"), out var miles))
                        entry.Miles = miles;

                    var result = await _timeEntries.UpsertAsync(entry);
                    if (result.Success)
                    {
                        batchResult.SuccessCount++;
                    }
                    else
                    {
                        batchResult.FailureCount++;
                        batchResult.Errors.Add($"Row {i}: {result.ErrorMessage}");
                    }
                }
                catch (Exception ex)
                {
                    batchResult.FailureCount++;
                    batchResult.Errors.Add($"Row {i}: {ex.Message}");
                }
            }

            _logger.LogInformation("[Migration] Imported {Success} time entries, {Failures} failures", 
                batchResult.SuccessCount, batchResult.FailureCount);

            var response = req.CreateResponse();
            await response.WriteAsJsonAsync(ServiceResult<BatchResult>.Ok(batchResult));
            response.StatusCode = HttpStatusCode.OK;
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing time entries from CSV");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(ServiceResult<BatchResult>.Fail(ex.Message));
            return errorResponse;
        }
    }

    private static string[] ParseCsvLine(string line)
    {
        var values = new List<string>();
        var current = "";
        var inQuotes = false;

        foreach (var c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                values.Add(current.Trim());
                current = "";
            }
            else
            {
                current += c;
            }
        }
        values.Add(current.Trim());

        return values.ToArray();
    }

    private static string GetValue(string[] headers, string[] values, string header)
    {
        var index = Array.IndexOf(headers, header);
        return index >= 0 && index < values.Length ? values[index].Trim('"') : "";
    }
}
