using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BillingSys.Shared.DTOs;
using BillingSys.Shared.Models;

namespace BillingSys.Client.Services;

public class ApiService
{
    private readonly HttpClient _http;

    public ApiService(HttpClient http)
    {
        _http = http;
    }

    #region Time Entries

    public async Task<ServiceResult<List<TimeEntry>>?> GetTimeEntriesAsync(int year, int week, string? employeeId = null)
    {
        var url = $"api/timeentries?year={year}&week={week}";
        if (!string.IsNullOrEmpty(employeeId))
            url += $"&employeeId={employeeId}";
        return await GetFromApiAsync<List<TimeEntry>>(url);
    }

    public async Task<ServiceResult<List<TimeEntry>>?> GetTimeEntriesByDateRangeAsync(DateTime startDate, DateTime endDate, string? employeeId = null)
    {
        var url = $"api/timeentries/range?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}";
        if (!string.IsNullOrEmpty(employeeId))
            url += $"&employeeId={employeeId}";
        return await GetFromApiAsync<List<TimeEntry>>(url);
    }

    public async Task<ServiceResult<TimeEntry>?> CreateTimeEntryAsync(CreateTimeEntryRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/timeentries", request);
        return await ReadFromApiResponseAsync<TimeEntry>(response);
    }

    public async Task<ServiceResult<TimeEntry>?> UpdateTimeEntryAsync(string yearWeek, string id, UpdateTimeEntryRequest request)
    {
        var response = await _http.PutAsJsonAsync($"api/timeentries/{yearWeek}/{id}", request);
        return await ReadFromApiResponseAsync<TimeEntry>(response);
    }

    public async Task<ServiceResult?> DeleteTimeEntryAsync(string yearWeek, string id)
    {
        var response = await _http.DeleteAsync($"api/timeentries/{yearWeek}/{id}");
        return await ReadVoidApiResponseAsync(response);
    }

    public async Task<ServiceResult<List<WeeklyHoursSummary>>?> GetWeeklyHoursSummaryAsync(int year, int week)
    {
        // Was: api/timeentries/reports/weekly-summary — backend route conflicted with timeentries/{yearWeek}/{id}.
        return await GetFromApiAsync<List<WeeklyHoursSummary>>($"api/timeentries/weekly-summary?year={year}&week={week}");
    }

    #endregion

    #region Employees

    public async Task<ServiceResult<List<Employee>>?> GetEmployeesAsync(bool includeInactive = false)
    {
        return await GetFromApiAsync<List<Employee>>($"api/employees?includeInactive={includeInactive}");
    }

    public async Task<ServiceResult<Employee>?> UpsertEmployeeAsync(Employee employee)
    {
        var response = await _http.PostAsJsonAsync("api/employees", employee);
        return await ReadFromApiResponseAsync<Employee>(response);
    }

    #endregion

    #region Customers

    public async Task<ServiceResult<List<Customer>>?> GetCustomersAsync(bool includeInactive = false)
    {
        return await GetFromApiAsync<List<Customer>>($"api/customers?includeInactive={includeInactive}");
    }

    public async Task<ServiceResult<Customer>?> UpsertCustomerAsync(Customer customer)
    {
        var response = await _http.PostAsJsonAsync("api/customers", customer);
        return await ReadFromApiResponseAsync<Customer>(response);
    }

    #endregion

    #region Projects

    public async Task<ServiceResult<List<Project>>?> GetProjectsAsync(string? status = null)
    {
        var url = "api/projects";
        if (!string.IsNullOrEmpty(status))
            url += $"?status={status}";
        return await GetFromApiAsync<List<Project>>(url);
    }

    public async Task<ServiceResult<List<Project>>?> GetProjectsByCustomerAsync(string customerId)
    {
        return await GetFromApiAsync<List<Project>>($"api/projects/by-customer/{customerId}");
    }

    public async Task<ServiceResult<List<ProjectSummary>>?> GetProjectSummariesAsync(string? status = null)
    {
        var url = "api/projects/summaries";
        if (!string.IsNullOrEmpty(status))
            url += $"?status={status}";
        return await GetFromApiAsync<List<ProjectSummary>>(url);
    }

    public async Task<ServiceResult<Project>?> CreateProjectAsync(CreateProjectRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/projects", request);
        return await ReadFromApiResponseAsync<Project>(response);
    }

    public async Task<ServiceResult<Project>?> UpdateProjectAsync(string customerId, string projectCode, UpdateProjectRequest request)
    {
        var response = await _http.PutAsJsonAsync($"api/projects/{customerId}/{projectCode}", request);
        return await ReadFromApiResponseAsync<Project>(response);
    }

    #endregion

    #region Service Items

    public async Task<ServiceResult<List<ServiceItem>>?> GetServiceItemsAsync(bool includeInactive = false)
    {
        return await GetFromApiAsync<List<ServiceItem>>($"api/serviceitems?includeInactive={includeInactive}");
    }

    #endregion

    #region Billing

    public async Task<ServiceResult<WeeklyBillingPreview>?> GetWeeklyBillingPreviewAsync(int year, int week)
    {
        return await GetFromApiAsync<WeeklyBillingPreview>($"api/billing/weekly/preview?year={year}&week={week}");
    }

    public async Task<ServiceResult<List<Invoice>>?> ProcessWeeklyBillingAsync(int year, int week, DateTime invoiceDate, List<string> customerIds)
    {
        var request = new { Year = year, WeekNumber = week, InvoiceDate = invoiceDate, SelectedCustomerIds = customerIds };
        var response = await _http.PostAsJsonAsync("api/billing/weekly/process", request);
        return await ReadFromApiResponseAsync<List<Invoice>>(response);
    }

    public async Task<ServiceResult<Invoice>?> ProcessProjectBillingAsync(ProjectBillingRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/billing/project/process", request);
        return await ReadFromApiResponseAsync<Invoice>(response);
    }

    #endregion

    #region Invoices

    public async Task<ServiceResult<List<Invoice>>?> GetInvoicesAsync(int year, int month)
    {
        return await GetFromApiAsync<List<Invoice>>($"api/invoices?year={year}&month={month}");
    }

    #endregion

    #region EDI

    public async Task<ServiceResult<List<EdiCustomerBillingPreview>>?> GetEdiBillingPreviewAsync(int year, int month)
    {
        return await GetFromApiAsync<List<EdiCustomerBillingPreview>>($"api/edi/billing/preview?year={year}&month={month}");
    }

    public async Task<ServiceResult<List<Invoice>>?> ProcessEdiBillingAsync(int year, int month, DateTime invoiceDate, List<string> siteIds)
    {
        var request = new { Year = year, Month = month, InvoiceDate = invoiceDate, SelectedSiteIds = siteIds };
        var response = await _http.PostAsJsonAsync("api/edi/billing/process", request);
        return await ReadFromApiResponseAsync<List<Invoice>>(response);
    }

    #endregion

    #region Toggl Import

    public async Task<ServiceResult<TogglPullResult>?> PullFromTogglAsync(DateTime startDate, DateTime endDate)
    {
        var request = new { StartDate = startDate, EndDate = endDate };
        var response = await _http.PostAsJsonAsync("api/toggl/pull", request);
        return await ReadFromApiResponseAsync<TogglPullResult>(response);
    }

    public async Task<ServiceResult<TogglPullResult>?> LoadTogglBatchAsync(string batchId)
    {
        return await GetFromApiAsync<TogglPullResult>($"api/toggl/batch/{batchId}");
    }

    public async Task<ServiceResult<TogglSummaryResult>?> SummarizeTogglBatchAsync(string batchId)
    {
        var request = new { BatchId = batchId };
        var response = await _http.PostAsJsonAsync("api/toggl/summarize", request);
        return await ReadFromApiResponseAsync<TogglSummaryResult>(response);
    }

    public async Task<ServiceResult?> EditTogglSummaryAsync(string batchId, string entryId, string newSummary)
    {
        var request = new { BatchId = batchId, EntryId = entryId, NewSummary = newSummary };
        var response = await _http.PutAsJsonAsync("api/toggl/summary/edit", request);
        return await ReadVoidApiResponseAsync(response);
    }

    public async Task<ServiceResult<TogglApproveResult>?> ApproveTogglBatchAsync(string batchId, List<string>? entryIds = null)
    {
        var request = new { BatchId = batchId, EntryIds = entryIds ?? new List<string>() };
        var response = await _http.PostAsJsonAsync("api/toggl/approve", request);
        return await ReadFromApiResponseAsync<TogglApproveResult>(response);
    }

    #endregion

    #region Private Methods

    private async Task<ServiceResult<T>?> GetFromApiAsync<T>(string requestUri)
    {
        using var response = await _http.GetAsync(requestUri);
        return await ReadFromApiResponseAsync<T>(response);
    }

    private async Task<ServiceResult<T>?> ReadFromApiResponseAsync<T>(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            return ServiceResult<T>.Fail("Session expired or not authorized. Sign in again.");

        if (response.StatusCode == HttpStatusCode.Forbidden)
            return ServiceResult<T>.Fail("Access denied.");

        if (!response.IsSuccessStatusCode)
        {
            var text = await response.Content.ReadAsStringAsync();
            if (TryParseServiceResultErrorMessage(text, out var apiMsg) && !string.IsNullOrWhiteSpace(apiMsg))
                return ServiceResult<T>.Fail(apiMsg);
            return ServiceResult<T>.Fail(
                $"The server could not complete this request (HTTP {(int)response.StatusCode}). Please try again.");
        }

        try
        {
            return await response.Content.ReadFromJsonAsync<ServiceResult<T>>();
        }
        catch
        {
            return ServiceResult<T>.Fail("Unexpected response from server.");
        }
    }

    private async Task<ServiceResult?> ReadVoidApiResponseAsync(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            return ServiceResult.Fail("Session expired or not authorized. Sign in again.");

        if (response.StatusCode == HttpStatusCode.Forbidden)
            return ServiceResult.Fail("Access denied.");

        if (!response.IsSuccessStatusCode)
        {
            var text = await response.Content.ReadAsStringAsync();
            if (TryParseServiceResultErrorMessage(text, out var apiMsg) && !string.IsNullOrWhiteSpace(apiMsg))
                return ServiceResult.Fail(apiMsg);
            return ServiceResult.Fail(
                $"The server could not complete this request (HTTP {(int)response.StatusCode}). Please try again.");
        }

        try
        {
            return await response.Content.ReadFromJsonAsync<ServiceResult>();
        }
        catch
        {
            return ServiceResult.Fail("Unexpected response from server.");
        }
    }

    /// <summary>
    /// When the API returns a non-2xx JSON body shaped like ServiceResult, surface ErrorMessage only (no raw JSON).
    /// </summary>
    private static bool TryParseServiceResultErrorMessage(string body, out string? message)
    {
        message = null;
        if (string.IsNullOrWhiteSpace(body))
            return false;
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("ErrorMessage", out var el))
                return false;
            if (el.ValueKind != JsonValueKind.String)
                return false;
            message = el.GetString();
            return !string.IsNullOrWhiteSpace(message);
        }
        catch
        {
            return false;
        }
    }

    #endregion
}
