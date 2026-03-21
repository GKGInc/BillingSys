using System.Net.Http.Json;
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
        return await _http.GetFromJsonAsync<ServiceResult<List<TimeEntry>>>(url);
    }

    public async Task<ServiceResult<List<TimeEntry>>?> GetTimeEntriesByDateRangeAsync(DateTime startDate, DateTime endDate, string? employeeId = null)
    {
        var url = $"api/timeentries/range?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}";
        if (!string.IsNullOrEmpty(employeeId))
            url += $"&employeeId={employeeId}";
        return await _http.GetFromJsonAsync<ServiceResult<List<TimeEntry>>>(url);
    }

    public async Task<ServiceResult<TimeEntry>?> CreateTimeEntryAsync(CreateTimeEntryRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/timeentries", request);
        return await response.Content.ReadFromJsonAsync<ServiceResult<TimeEntry>>();
    }

    public async Task<ServiceResult<TimeEntry>?> UpdateTimeEntryAsync(string yearWeek, string id, UpdateTimeEntryRequest request)
    {
        var response = await _http.PutAsJsonAsync($"api/timeentries/{yearWeek}/{id}", request);
        return await response.Content.ReadFromJsonAsync<ServiceResult<TimeEntry>>();
    }

    public async Task<ServiceResult?> DeleteTimeEntryAsync(string yearWeek, string id)
    {
        var response = await _http.DeleteAsync($"api/timeentries/{yearWeek}/{id}");
        return await response.Content.ReadFromJsonAsync<ServiceResult>();
    }

    public async Task<ServiceResult<List<WeeklyHoursSummary>>?> GetWeeklyHoursSummaryAsync(int year, int week)
    {
        return await _http.GetFromJsonAsync<ServiceResult<List<WeeklyHoursSummary>>>($"api/timeentries/reports/weekly-summary?year={year}&week={week}");
    }

    #endregion

    #region Employees

    public async Task<ServiceResult<List<Employee>>?> GetEmployeesAsync(bool includeInactive = false)
    {
        return await _http.GetFromJsonAsync<ServiceResult<List<Employee>>>($"api/employees?includeInactive={includeInactive}");
    }

    public async Task<ServiceResult<Employee>?> UpsertEmployeeAsync(Employee employee)
    {
        var response = await _http.PostAsJsonAsync("api/employees", employee);
        return await response.Content.ReadFromJsonAsync<ServiceResult<Employee>>();
    }

    #endregion

    #region Customers

    public async Task<ServiceResult<List<Customer>>?> GetCustomersAsync(bool includeInactive = false)
    {
        return await _http.GetFromJsonAsync<ServiceResult<List<Customer>>>($"api/customers?includeInactive={includeInactive}");
    }

    public async Task<ServiceResult<Customer>?> UpsertCustomerAsync(Customer customer)
    {
        var response = await _http.PostAsJsonAsync("api/customers", customer);
        return await response.Content.ReadFromJsonAsync<ServiceResult<Customer>>();
    }

    #endregion

    #region Projects

    public async Task<ServiceResult<List<Project>>?> GetProjectsAsync(string? status = null)
    {
        var url = "api/projects";
        if (!string.IsNullOrEmpty(status))
            url += $"?status={status}";
        return await _http.GetFromJsonAsync<ServiceResult<List<Project>>>(url);
    }

    public async Task<ServiceResult<List<Project>>?> GetProjectsByCustomerAsync(string customerId)
    {
        return await _http.GetFromJsonAsync<ServiceResult<List<Project>>>($"api/projects/by-customer/{customerId}");
    }

    public async Task<ServiceResult<List<ProjectSummary>>?> GetProjectSummariesAsync(string? status = null)
    {
        var url = "api/projects/summaries";
        if (!string.IsNullOrEmpty(status))
            url += $"?status={status}";
        return await _http.GetFromJsonAsync<ServiceResult<List<ProjectSummary>>>(url);
    }

    public async Task<ServiceResult<Project>?> CreateProjectAsync(CreateProjectRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/projects", request);
        return await response.Content.ReadFromJsonAsync<ServiceResult<Project>>();
    }

    public async Task<ServiceResult<Project>?> UpdateProjectAsync(string customerId, string projectCode, UpdateProjectRequest request)
    {
        var response = await _http.PutAsJsonAsync($"api/projects/{customerId}/{projectCode}", request);
        return await response.Content.ReadFromJsonAsync<ServiceResult<Project>>();
    }

    #endregion

    #region Service Items

    public async Task<ServiceResult<List<ServiceItem>>?> GetServiceItemsAsync(bool includeInactive = false)
    {
        return await _http.GetFromJsonAsync<ServiceResult<List<ServiceItem>>>($"api/serviceitems?includeInactive={includeInactive}");
    }

    #endregion

    #region Billing

    public async Task<ServiceResult<WeeklyBillingPreview>?> GetWeeklyBillingPreviewAsync(int year, int week)
    {
        return await _http.GetFromJsonAsync<ServiceResult<WeeklyBillingPreview>>($"api/billing/weekly/preview?year={year}&week={week}");
    }

    public async Task<ServiceResult<List<Invoice>>?> ProcessWeeklyBillingAsync(int year, int week, DateTime invoiceDate, List<string> customerIds)
    {
        var request = new { Year = year, WeekNumber = week, InvoiceDate = invoiceDate, SelectedCustomerIds = customerIds };
        var response = await _http.PostAsJsonAsync("api/billing/weekly/process", request);
        return await response.Content.ReadFromJsonAsync<ServiceResult<List<Invoice>>>();
    }

    public async Task<ServiceResult<Invoice>?> ProcessProjectBillingAsync(ProjectBillingRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/billing/project/process", request);
        return await response.Content.ReadFromJsonAsync<ServiceResult<Invoice>>();
    }

    #endregion

    #region Invoices

    public async Task<ServiceResult<List<Invoice>>?> GetInvoicesAsync(int year, int month)
    {
        return await _http.GetFromJsonAsync<ServiceResult<List<Invoice>>>($"api/invoices?year={year}&month={month}");
    }

    #endregion

    #region EDI

    public async Task<ServiceResult<List<EdiCustomerBillingPreview>>?> GetEdiBillingPreviewAsync(int year, int month)
    {
        return await _http.GetFromJsonAsync<ServiceResult<List<EdiCustomerBillingPreview>>>($"api/edi/billing/preview?year={year}&month={month}");
    }

    public async Task<ServiceResult<List<Invoice>>?> ProcessEdiBillingAsync(int year, int month, DateTime invoiceDate, List<string> siteIds)
    {
        var request = new { Year = year, Month = month, InvoiceDate = invoiceDate, SelectedSiteIds = siteIds };
        var response = await _http.PostAsJsonAsync("api/edi/billing/process", request);
        return await response.Content.ReadFromJsonAsync<ServiceResult<List<Invoice>>>();
    }

    #endregion
}
