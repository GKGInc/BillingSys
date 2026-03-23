using Azure;
using Azure.Data.Tables;
using BillingSys.Functions.Services;
using BillingSys.Shared.Models;
using Microsoft.Extensions.Logging;

namespace BillingSys.Functions.Repositories;

#region Table Entity

public class TogglImportEntity : ITableEntity
{
    // PartitionKey = BatchId, RowKey = Id
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public long TogglEntryId { get; set; }
    public string? OriginalDescription { get; set; }
    public string? TogglProjectName { get; set; }
    public string? TogglClientName { get; set; }
    public string EmployeeId { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public double Hours { get; set; }
    public bool Billable { get; set; }
    public string? TogglTags { get; set; }
    public string? SummarizedDescription { get; set; }
    public string? BillingGroupKey { get; set; }
    public string? MappedProjectCode { get; set; }
    public string? MappedCustomerId { get; set; }
    public string? AbsorbedIntoId { get; set; }
    public double AbsorbedHours { get; set; }
    public string Status { get; set; } = "Raw";
    public string? TimeEntryId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public TogglImport ToModel() => new()
    {
        Id = RowKey,
        BatchId = PartitionKey,
        TogglEntryId = TogglEntryId,
        OriginalDescription = OriginalDescription,
        TogglProjectName = TogglProjectName,
        TogglClientName = TogglClientName,
        EmployeeId = EmployeeId,
        EmployeeName = EmployeeName,
        Date = DateTimeUtc.EnsureUtcDate(Date),
        Hours = (decimal)Hours,
        Billable = Billable,
        TogglTags = TogglTags,
        SummarizedDescription = SummarizedDescription,
        BillingGroupKey = BillingGroupKey,
        MappedProjectCode = MappedProjectCode,
        MappedCustomerId = MappedCustomerId,
        AbsorbedIntoId = AbsorbedIntoId,
        AbsorbedHours = (decimal)AbsorbedHours,
        Status = Enum.TryParse<TogglImportStatus>(Status, out var s) ? s : TogglImportStatus.Raw,
        TimeEntryId = TimeEntryId,
        CreatedAt = DateTimeUtc.EnsureUtc(CreatedAt),
        UpdatedAt = UpdatedAt.HasValue ? DateTimeUtc.EnsureUtc(UpdatedAt.Value) : null
    };

    public static TogglImportEntity FromModel(TogglImport model) => new()
    {
        PartitionKey = model.BatchId,
        RowKey = model.Id,
        TogglEntryId = model.TogglEntryId,
        OriginalDescription = model.OriginalDescription,
        TogglProjectName = model.TogglProjectName,
        TogglClientName = model.TogglClientName,
        EmployeeId = model.EmployeeId,
        EmployeeName = model.EmployeeName,
        Date = DateTimeUtc.EnsureUtcDate(model.Date),
        Hours = (double)model.Hours,
        Billable = model.Billable,
        TogglTags = model.TogglTags,
        SummarizedDescription = model.SummarizedDescription,
        BillingGroupKey = model.BillingGroupKey,
        MappedProjectCode = model.MappedProjectCode,
        MappedCustomerId = model.MappedCustomerId,
        AbsorbedIntoId = model.AbsorbedIntoId,
        AbsorbedHours = (double)model.AbsorbedHours,
        Status = model.Status.ToString(),
        TimeEntryId = model.TimeEntryId,
        CreatedAt = DateTimeUtc.EnsureUtc(model.CreatedAt),
        UpdatedAt = model.UpdatedAt.HasValue ? DateTimeUtc.EnsureUtc(model.UpdatedAt.Value) : null
    };
}

#endregion

#region Repository Interface

public interface ITogglImportRepository
{
    Task<ServiceResult<List<TogglImport>>> GetByBatchAsync(string batchId);
    Task<ServiceResult<TogglImport>> UpsertAsync(TogglImport import);
    Task<ServiceResult> UpsertBatchAsync(List<TogglImport> imports);
    Task<ServiceResult<bool>> TogglEntryExistsAsync(long togglEntryId);
}

#endregion

#region Repository Implementation

public class TogglImportRepository : ITogglImportRepository
{
    private readonly TableStorageContext _context;
    private readonly ILogger<TogglImportRepository> _logger;

    public const string TableName = "TogglImports";

    public TogglImportRepository(TableStorageContext context, ILogger<TogglImportRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ServiceResult<List<TogglImport>>> GetByBatchAsync(string batchId)
    {
        try
        {
            var table = _context.GetTable(TableName);
            var filter = $"PartitionKey eq '{batchId}'";
            var imports = new List<TogglImport>();
            await foreach (var entity in table.QueryAsync<TogglImportEntity>(filter))
            {
                imports.Add(entity.ToModel());
            }
            return ServiceResult<List<TogglImport>>.Ok(
                imports.OrderBy(e => e.Date).ThenBy(e => e.EmployeeName).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting toggl imports for batch {BatchId}", batchId);
            return ServiceResult<List<TogglImport>>.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult<TogglImport>> UpsertAsync(TogglImport import)
    {
        try
        {
            var table = _context.GetTable(TableName);
            var entity = TogglImportEntity.FromModel(import);
            await table.UpsertEntityAsync(entity);
            return ServiceResult<TogglImport>.Ok(import);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting toggl import {Id}", import.Id);
            return ServiceResult<TogglImport>.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult> UpsertBatchAsync(List<TogglImport> imports)
    {
        try
        {
            var table = _context.GetTable(TableName);
            // Azure Table Storage supports batch operations on same partition
            var grouped = imports.GroupBy(i => i.BatchId);
            foreach (var group in grouped)
            {
                var batch = new List<TableTransactionAction>();
                foreach (var import in group)
                {
                    var entity = TogglImportEntity.FromModel(import);
                    batch.Add(new TableTransactionAction(TableTransactionActionType.UpsertReplace, entity));

                    // Table Storage batches max at 100
                    if (batch.Count >= 100)
                    {
                        await table.SubmitTransactionAsync(batch);
                        batch.Clear();
                    }
                }
                if (batch.Any())
                {
                    await table.SubmitTransactionAsync(batch);
                }
            }
            return ServiceResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error batch upserting toggl imports");
            return ServiceResult.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult<bool>> TogglEntryExistsAsync(long togglEntryId)
    {
        try
        {
            var table = _context.GetTable(TableName);
            // Scan for this Toggl entry ID across all batches
            var filter = $"TogglEntryId eq {togglEntryId}L and Status ne 'Skipped'";
            await foreach (var _ in table.QueryAsync<TogglImportEntity>(filter))
            {
                return ServiceResult<bool>.Ok(true);
            }
            return ServiceResult<bool>.Ok(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking toggl entry existence {TogglEntryId}", togglEntryId);
            return ServiceResult<bool>.Fail(ex.Message);
        }
    }
}

#endregion
