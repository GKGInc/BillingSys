using System.Net;
using System.Text.Json;
using BillingSys.Functions.Repositories;
using BillingSys.Functions.Services;
using BillingSys.Functions.Validators;
using BillingSys.Shared.DTOs;
using BillingSys.Shared.Enums;
using BillingSys.Shared.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace BillingSys.Functions.Functions;

public class BillingFunctions
{
    private readonly BillingService _billingService;
    private readonly IProjectRepository _projects;
    private readonly IInvoiceRepository _invoices;
    private readonly AuthorizationService _authService;
    private readonly ILogger<BillingFunctions> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public BillingFunctions(
        BillingService billingService,
        IProjectRepository projects,
        IInvoiceRepository invoices,
        AuthorizationService authService,
        ILogger<BillingFunctions> logger)
    {
        _billingService = billingService;
        _projects = projects;
        _invoices = invoices;
        _authService = authService;
        _logger = logger;
    }

    #region Weekly Billing

    [Function("GetWeeklyBillingPreview")]
    public async Task<HttpResponseData> GetWeeklyBillingPreview(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "billing/weekly/preview")] HttpRequestData req)
    {
        var authResult = await _authService.AuthorizeAsync(req, UserRole.Manager, UserRole.Admin);
        if (!authResult.IsAuthorized) return await authResult.ToResponseAsync(req);

        var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        var year = int.TryParse(query["year"], out var y) ? y : DateTime.Today.Year;
        var week = int.TryParse(query["week"], out var w) ? w : GetIso8601WeekOfYear(DateTime.Today);

        var result = await _billingService.GetWeeklyBillingPreviewAsync(year, week);

        var response = req.CreateResponse();
        await response.WriteAsJsonAsync(result);
        response.StatusCode = result.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
        return response;
    }

    [Function("ProcessWeeklyBilling")]
    public async Task<HttpResponseData> ProcessWeeklyBilling(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "billing/weekly/process")] HttpRequestData req)
    {
        var authResult = await _authService.AuthorizeAsync(req, UserRole.Manager, UserRole.Admin);
        if (!authResult.IsAuthorized) return await authResult.ToResponseAsync(req);

        try
        {
            var body = await req.ReadAsStringAsync();
            var request = JsonSerializer.Deserialize<ProcessWeeklyBillingRequest>(body!, JsonOptions);
            if (request == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(ServiceResult<List<Invoice>>.Fail("Invalid request body"));
                return badResponse;
            }

            var validator = new ProcessWeeklyBillingValidator();
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(ServiceResult<List<Invoice>>.Fail(
                    string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage))));
                return badResponse;
            }

            var billingRequest = new WeeklyBillingRequest
            {
                WeekNumber = request.WeekNumber,
                Year = request.Year,
                InvoiceDate = request.InvoiceDate
            };

            var result = await _billingService.ProcessWeeklyBillingAsync(billingRequest, request.SelectedCustomerIds);

            var response = req.CreateResponse();
            await response.WriteAsJsonAsync(result);
            response.StatusCode = result.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing weekly billing");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(ServiceResult<List<Invoice>>.Fail(ex.Message));
            return errorResponse;
        }
    }

    #endregion

    #region Project Billing

    [Function("GetProjectBillingPreview")]
    public async Task<HttpResponseData> GetProjectBillingPreview(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "billing/project/preview/{customerId}")] HttpRequestData req,
        string customerId)
    {
        var authResult = await _authService.AuthorizeAsync(req, UserRole.Manager, UserRole.Admin);
        if (!authResult.IsAuthorized) return await authResult.ToResponseAsync(req);

        try
        {
            var projectsResult = await _projects.GetByCustomerAsync(customerId);
            if (!projectsResult.Success)
            {
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteAsJsonAsync(projectsResult);
                return errorResponse;
            }

            var activeProjects = projectsResult.Data!
                .Where(p => p.Status == ProjectStatus.Active && p.RemainingHours > 0)
                .Select(p => new
                {
                    p.ProjectCode,
                    p.Description,
                    p.Price,
                    p.QuotedHours,
                    p.AdditionalHours,
                    p.BilledHours,
                    p.RemainingHours,
                    p.CustomerPO,
                    p.PreBill
                })
                .ToList();

            var response = req.CreateResponse();
            await response.WriteAsJsonAsync(ServiceResult<object>.Ok(activeProjects));
            response.StatusCode = HttpStatusCode.OK;
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting project billing preview for {CustomerId}", customerId);
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(ServiceResult<object>.Fail(ex.Message));
            return errorResponse;
        }
    }

    [Function("ProcessProjectBilling")]
    public async Task<HttpResponseData> ProcessProjectBilling(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "billing/project/process")] HttpRequestData req)
    {
        var authResult = await _authService.AuthorizeAsync(req, UserRole.Manager, UserRole.Admin);
        if (!authResult.IsAuthorized) return await authResult.ToResponseAsync(req);

        try
        {
            var body = await req.ReadAsStringAsync();
            var request = JsonSerializer.Deserialize<ProjectBillingRequest>(body!, JsonOptions);
            if (request == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(ServiceResult<Invoice>.Fail("Invalid request body"));
                return badResponse;
            }

            var validator = new ProjectBillingRequestValidator();
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(ServiceResult<Invoice>.Fail(
                    string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage))));
                return badResponse;
            }

            var result = await _billingService.CreateProjectInvoiceAsync(request);

            var response = req.CreateResponse();
            await response.WriteAsJsonAsync(result);
            response.StatusCode = result.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing project billing");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(ServiceResult<Invoice>.Fail(ex.Message));
            return errorResponse;
        }
    }

    #endregion

    #region Invoices

    [Function("GetInvoices")]
    public async Task<HttpResponseData> GetInvoices(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "invoices")] HttpRequestData req)
    {
        var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        var year = int.TryParse(query["year"], out var y) ? y : DateTime.Today.Year;
        var month = int.TryParse(query["month"], out var m) ? m : DateTime.Today.Month;

        var result = await _invoices.GetByMonthAsync(year, month);

        var response = req.CreateResponse();
        await response.WriteAsJsonAsync(result);
        response.StatusCode = result.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
        return response;
    }

    [Function("GetInvoice")]
    public async Task<HttpResponseData> GetInvoice(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "invoices/{yearMonth}/{invoiceNumber}")] HttpRequestData req,
        string yearMonth, string invoiceNumber)
    {
        var result = await _invoices.GetAsync(yearMonth, invoiceNumber);

        var response = req.CreateResponse();
        await response.WriteAsJsonAsync(result);
        response.StatusCode = result.Success ? HttpStatusCode.OK : HttpStatusCode.NotFound;
        return response;
    }

    #endregion

    #region Helper Methods

    private static int GetIso8601WeekOfYear(DateTime date)
    {
        var day = System.Globalization.CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(date);
        if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
        {
            date = date.AddDays(3);
        }
        return System.Globalization.CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
            date, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
    }

    #endregion
}

public class ProcessWeeklyBillingRequest
{
    public int WeekNumber { get; set; }
    public int Year { get; set; }
    public DateTime InvoiceDate { get; set; }
    public List<string> SelectedCustomerIds { get; set; } = new();
}
