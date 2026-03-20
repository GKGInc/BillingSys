using BillingSys.Shared.Models;

namespace BillingSys.Functions.Repositories;

public interface ITimeEntryRepository
{
    Task<ServiceResult<TimeEntry>> GetAsync(string yearWeek, string id);
    Task<ServiceResult<List<TimeEntry>>> GetByWeekAsync(int year, int weekNumber, string? employeeId = null);
    Task<ServiceResult<List<TimeEntry>>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, string? employeeId = null);
    Task<ServiceResult<TimeEntry>> UpsertAsync(TimeEntry entry);
    Task<ServiceResult> DeleteAsync(string yearWeek, string id);
}
