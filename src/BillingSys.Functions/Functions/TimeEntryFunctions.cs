using System.Net;
using System.Text.Json;
using BillingSys.Functions.Infrastructure;
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

public class TimeEntryFunctions
{
    private readonly ITimeEntryRepository _timeEntries;
    private readonly IEmployeeRepository _employees;
    private readonly AuthorizationService _authService;
    private readonly ILogger<TimeEntryFunctions> _logger;
    private static readonly JsonSerializerOptions JsonOptions = FunctionsJsonSerializerOptions.Default;

    public TimeEntryFunctions(
        ITimeEntryRepository timeEntries,
        IEmployeeRepository employees,
        AuthorizationService authService,
        ILogger<TimeEntryFunctions> logger)
    {
        _timeEntries = timeEntries;
        _employees = employees;
        _authService = authService;
        _logger = logger;
    }

    #region CRUD Operations

    [Function("GetTimeEntries")]
    public async Task<HttpResponseData> GetTimeEntries(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "timeentries")] HttpRequestData req)
    {
        var authResult = await _authService.AuthorizeAsync(req);
        if (!authResult.IsAuthorized) return await authResult.ToResponseAsync(req);

        var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        var year = int.TryParse(query["year"], out var y) ? y : DateTime.Today.Year;
        var week = int.TryParse(query["week"], out var w) ? w : GetIso8601WeekOfYear(DateTime.Today);
        var employeeId = query["employeeId"];

        var result = await _timeEntries.GetByWeekAsync(year, week, employeeId);

        var response = req.CreateResponse(result.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest);
        await response.WriteAsJsonAsync(result);
        return response;
    }

    [Function("GetTimeEntriesByDateRange")]
    public async Task<HttpResponseData> GetTimeEntriesByDateRange(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "timeentries/range")] HttpRequestData req)
    {
        var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);

        if (!DateTime.TryParse(query["startDate"], out var startDate) ||
            !DateTime.TryParse(query["endDate"], out var endDate))
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteAsJsonAsync(ServiceResult<List<TimeEntry>>.Fail("Invalid date range"));
            return badResponse;
        }

        var employeeId = query["employeeId"];
        var result = await _timeEntries.GetByDateRangeAsync(startDate, endDate, employeeId);

        var response = req.CreateResponse(result.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest);
        await response.WriteAsJsonAsync(result);
        return response;
    }

    [Function("GetTimeEntry")]
    public async Task<HttpResponseData> GetTimeEntry(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "timeentries/{yearWeek}/{id}")] HttpRequestData req,
        string yearWeek, string id)
    {
        var result = await _timeEntries.GetAsync(yearWeek, id);

        var response = req.CreateResponse(result.Success ? HttpStatusCode.OK : HttpStatusCode.NotFound);
        await response.WriteAsJsonAsync(result);
        return response;
    }

    [Function("CreateTimeEntry")]
    public async Task<HttpResponseData> CreateTimeEntry(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "timeentries")] HttpRequestData req)
    {
        try
        {
            var body = await req.ReadAsStringAsync();
            if (string.IsNullOrEmpty(body))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(ServiceResult<TimeEntry>.Fail("Request body is required"));
                return badResponse;
            }

            var request = JsonSerializer.Deserialize<CreateTimeEntryRequest>(body, JsonOptions);
            if (request == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(ServiceResult<TimeEntry>.Fail("Invalid request body"));
                return badResponse;
            }

            request.Date = DateTimeUtc.EnsureUtcDate(request.Date);

            var validator = new CreateTimeEntryValidator();
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(ServiceResult<TimeEntry>.Fail(
                    string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage))));
                return badResponse;
            }

            var employeeResult = await _employees.GetAsync(request.EmployeeId);
            var employeeName = employeeResult.Success ? employeeResult.Data!.Name : request.EmployeeId;

            var entry = new TimeEntry
            {
                Id = Guid.NewGuid().ToString(),
                EmployeeId = request.EmployeeId,
                EmployeeName = employeeName,
                Date = request.Date,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                Hours = request.Hours,
                Billable = request.Billable,
                ProjectCode = request.ProjectCode,
                Miles = request.Miles,
                Comments = request.Comments,
                Status = TimeEntryStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _timeEntries.UpsertAsync(entry);

            var response = req.CreateResponse(result.Success ? HttpStatusCode.Created : HttpStatusCode.BadRequest);
            await response.WriteAsJsonAsync(result);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating time entry");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(ServiceResult<TimeEntry>.Fail(ex.Message));
            return errorResponse;
        }
    }

    [Function("UpdateTimeEntry")]
    public async Task<HttpResponseData> UpdateTimeEntry(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "timeentries/{yearWeek}/{id}")] HttpRequestData req,
        string yearWeek, string id)
    {
        try
        {
            var existingResult = await _timeEntries.GetAsync(yearWeek, id);
            if (!existingResult.Success)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteAsJsonAsync(existingResult);
                return notFoundResponse;
            }

            var body = await req.ReadAsStringAsync();
            var request = JsonSerializer.Deserialize<UpdateTimeEntryRequest>(body!, JsonOptions);
            if (request == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(ServiceResult<TimeEntry>.Fail("Invalid request body"));
                return badResponse;
            }

            request.Date = DateTimeUtc.EnsureUtcDate(request.Date);

            var validator = new UpdateTimeEntryValidator();
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(ServiceResult<TimeEntry>.Fail(
                    string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage))));
                return badResponse;
            }

            var entry = existingResult.Data!;
            entry.Date = request.Date;
            entry.StartTime = request.StartTime;
            entry.EndTime = request.EndTime;
            entry.Hours = request.Hours;
            entry.Billable = request.Billable;
            entry.ProjectCode = request.ProjectCode;
            entry.Miles = request.Miles;
            entry.Comments = request.Comments;
            entry.UpdatedAt = DateTime.UtcNow;

            var result = await _timeEntries.UpsertAsync(entry);

            var response = req.CreateResponse(result.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest);
            await response.WriteAsJsonAsync(result);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating time entry {Id}", id);
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(ServiceResult<TimeEntry>.Fail(ex.Message));
            return errorResponse;
        }
    }

    [Function("DeleteTimeEntry")]
    public async Task<HttpResponseData> DeleteTimeEntry(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "timeentries/{yearWeek}/{id}")] HttpRequestData req,
        string yearWeek, string id)
    {
        var result = await _timeEntries.DeleteAsync(yearWeek, id);

        var response = req.CreateResponse(result.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest);
        await response.WriteAsJsonAsync(result);
        return response;
    }

    #endregion

    #region Bulk Operations

    [Function("CreateTimeEntries")]
    public async Task<HttpResponseData> CreateTimeEntries(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "timeentries/bulk")] HttpRequestData req)
    {
        try
        {
            var body = await req.ReadAsStringAsync();
            var requests = JsonSerializer.Deserialize<List<CreateTimeEntryRequest>>(body!, JsonOptions);
            if (requests == null || requests.Count == 0)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(ServiceResult<BatchResult>.Fail("Invalid request body"));
                return badResponse;
            }

            var validator = new CreateTimeEntryValidator();
            var batchResult = new BatchResult();

            foreach (var request in requests)
            {
                try
                {
                    request.Date = DateTimeUtc.EnsureUtcDate(request.Date);

                    var validationResult = await validator.ValidateAsync(request);
                    if (!validationResult.IsValid)
                    {
                        batchResult.FailureCount++;
                        batchResult.Errors.Add(string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)));
                        continue;
                    }

                    var employeeResult = await _employees.GetAsync(request.EmployeeId);
                    var employeeName = employeeResult.Success ? employeeResult.Data!.Name : request.EmployeeId;

                    var entry = new TimeEntry
                    {
                        Id = Guid.NewGuid().ToString(),
                        EmployeeId = request.EmployeeId,
                        EmployeeName = employeeName,
                        Date = request.Date,
                        StartTime = request.StartTime,
                        EndTime = request.EndTime,
                        Hours = request.Hours,
                        Billable = request.Billable,
                        ProjectCode = request.ProjectCode,
                        Miles = request.Miles,
                        Comments = request.Comments,
                        Status = TimeEntryStatus.Pending,
                        CreatedAt = DateTime.UtcNow
                    };

                    var result = await _timeEntries.UpsertAsync(entry);
                    if (result.Success)
                    {
                        batchResult.SuccessCount++;
                    }
                    else
                    {
                        batchResult.FailureCount++;
                        batchResult.Errors.Add(result.ErrorMessage ?? "Unknown error");
                    }
                }
                catch (Exception ex)
                {
                    batchResult.FailureCount++;
                    batchResult.Errors.Add(ex.Message);
                    _logger.LogError(ex, "Error creating time entry in bulk operation");
                }
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(ServiceResult<BatchResult>.Ok(batchResult));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk time entry creation");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(ServiceResult<BatchResult>.Fail(ex.Message));
            return errorResponse;
        }
    }

    [Function("ApproveTimeEntries")]
    public async Task<HttpResponseData> ApproveTimeEntries(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "timeentries/approve")] HttpRequestData req)
    {
        var authResult = await _authService.AuthorizeAsync(req, UserRole.Manager, UserRole.Admin);
        if (!authResult.IsAuthorized) return await authResult.ToResponseAsync(req);

        try
        {
            var body = await req.ReadAsStringAsync();
            var entryIds = JsonSerializer.Deserialize<List<TimeEntryIdentifier>>(body!, JsonOptions);
            if (entryIds == null || entryIds.Count == 0)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(ServiceResult<BatchResult>.Fail("Entry IDs are required"));
                return badResponse;
            }

            var batchResult = new BatchResult();
            foreach (var entryId in entryIds)
            {
                try
                {
                    var result = await _timeEntries.GetAsync(entryId.YearWeek, entryId.Id);
                    if (result.Success && result.Data != null)
                    {
                        result.Data.Status = TimeEntryStatus.Approved;
                        result.Data.UpdatedAt = DateTime.UtcNow;
                        var updateResult = await _timeEntries.UpsertAsync(result.Data);
                        if (updateResult.Success)
                        {
                            batchResult.SuccessCount++;
                        }
                        else
                        {
                            batchResult.FailureCount++;
                            batchResult.Errors.Add($"{entryId.Id}: {updateResult.ErrorMessage}");
                        }
                    }
                    else
                    {
                        batchResult.FailureCount++;
                        batchResult.Errors.Add($"{entryId.Id}: Not found");
                    }
                }
                catch (Exception ex)
                {
                    batchResult.FailureCount++;
                    batchResult.Errors.Add($"{entryId.Id}: {ex.Message}");
                }
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(ServiceResult<BatchResult>.Ok(batchResult));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving time entries");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(ServiceResult<BatchResult>.Fail(ex.Message));
            return errorResponse;
        }
    }

    #endregion

    #region Reports

    [Function("GetWeeklyHoursSummary")]
    public async Task<HttpResponseData> GetWeeklyHoursSummary(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "timeentries/summary/weekly")] HttpRequestData req)
    {
        var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        var year = int.TryParse(query["year"], out var y) ? y : DateTime.Today.Year;
        var week = int.TryParse(query["week"], out var w) ? w : GetIso8601WeekOfYear(DateTime.Today);

        var entriesResult = await _timeEntries.GetByWeekAsync(year, week);
        if (!entriesResult.Success)
        {
            var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await errorResponse.WriteAsJsonAsync(ServiceResult<List<WeeklyHoursSummary>>.Fail(entriesResult.ErrorMessage ?? "Failed to get entries"));
            return errorResponse;
        }

        var summaries = entriesResult.Data!
            .GroupBy(e => new { e.EmployeeId, e.EmployeeName })
            .Select(g => new WeeklyHoursSummary
            {
                EmployeeId = g.Key.EmployeeId,
                EmployeeName = g.Key.EmployeeName,
                WeekNumber = week,
                Year = year,
                TotalHours = g.Sum(e => e.Hours),
                BillableHours = g.Where(e => e.Billable).Sum(e => e.Hours),
                VacationHours = g.Where(e => e.ProjectCode == "GKGVAC").Sum(e => e.Hours),
                HolidayHours = g.Where(e => e.ProjectCode == "GKGHOL").Sum(e => e.Hours),
                SickHours = g.Where(e => e.ProjectCode == "GKGSIK").Sum(e => e.Hours),
                PersonalHours = g.Where(e => e.ProjectCode == "GKGPER").Sum(e => e.Hours)
            })
            .ToList();

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(ServiceResult<List<WeeklyHoursSummary>>.Ok(summaries));
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

public class TimeEntryIdentifier
{
    public string YearWeek { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
}
