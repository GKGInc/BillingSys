using Azure;
using BillingSys.Functions.Services;
using BillingSys.Shared.Models;
using Microsoft.Extensions.Logging;

namespace BillingSys.Functions.Repositories;

public class TimeEntryRepository : ITimeEntryRepository
{
    private readonly TableStorageContext _context;
    private readonly ILogger<TimeEntryRepository> _logger;

    public TimeEntryRepository(TableStorageContext context, ILogger<TimeEntryRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Public Methods

    public async Task<ServiceResult<TimeEntry>> GetAsync(string yearWeek, string id)
    {
        try
        {
            var table = _context.GetTable(TableStorageContext.TimeEntriesTable);
            var response = await table.GetEntityAsync<TimeEntryEntity>(yearWeek, id);
            return ServiceResult<TimeEntry>.Ok(response.Value.ToModel());
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return ServiceResult<TimeEntry>.Fail($"Time entry {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting time entry {Id}", id);
            return ServiceResult<TimeEntry>.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult<List<TimeEntry>>> GetByWeekAsync(int year, int weekNumber, string? employeeId = null)
    {
        try
        {
            var table = _context.GetTable(TableStorageContext.TimeEntriesTable);
            var partitionKey = $"{year}-{weekNumber:D2}";
            var filter = $"PartitionKey eq '{partitionKey}'";
            if (!string.IsNullOrEmpty(employeeId))
            {
                filter += $" and EmployeeId eq '{employeeId}'";
            }
            var entries = new List<TimeEntry>();
            await foreach (var entity in table.QueryAsync<TimeEntryEntity>(filter))
            {
                entries.Add(entity.ToModel());
            }
            return ServiceResult<List<TimeEntry>>.Ok(entries.OrderBy(e => e.Date).ThenBy(e => e.StartTime).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting time entries for week {Year}-{Week}", year, weekNumber);
            return ServiceResult<List<TimeEntry>>.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult<List<TimeEntry>>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, string? employeeId = null)
    {
        try
        {
            var table = _context.GetTable(TableStorageContext.TimeEntriesTable);
            var entries = new List<TimeEntry>();

            await foreach (var entity in table.QueryAsync<TimeEntryEntity>())
            {
                if (entity.Date >= startDate && entity.Date <= endDate)
                {
                    if (string.IsNullOrEmpty(employeeId) || entity.EmployeeId == employeeId)
                    {
                        entries.Add(entity.ToModel());
                    }
                }
            }
            return ServiceResult<List<TimeEntry>>.Ok(entries.OrderBy(e => e.Date).ThenBy(e => e.StartTime).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting time entries for date range");
            return ServiceResult<List<TimeEntry>>.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult<TimeEntry>> UpsertAsync(TimeEntry entry)
    {
        try
        {
            var table = _context.GetTable(TableStorageContext.TimeEntriesTable);
            var entity = TimeEntryEntity.FromModel(entry);
            await table.UpsertEntityAsync(entity);
            return ServiceResult<TimeEntry>.Ok(entry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting time entry {Id}", entry.Id);
            return ServiceResult<TimeEntry>.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult> DeleteAsync(string yearWeek, string id)
    {
        try
        {
            var table = _context.GetTable(TableStorageContext.TimeEntriesTable);
            await table.DeleteEntityAsync(yearWeek, id);
            return ServiceResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting time entry {Id}", id);
            return ServiceResult.Fail(ex.Message);
        }
    }

    #endregion
}
