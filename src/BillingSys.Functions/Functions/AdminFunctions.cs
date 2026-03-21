using System.Net;
using System.Text.Json;
using Azure;
using BillingSys.Functions.Infrastructure;
using Azure.Data.Tables;
using BillingSys.Functions.Repositories;
using BillingSys.Shared.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace BillingSys.Functions.Functions;

/// <summary>
/// Development / opt-in dangerous operations. Never enable BILLINGSYS_ALLOW_CLEAR_SEED in production unless you understand the risk.
/// </summary>
public class AdminFunctions
{
    private readonly TableStorageContext _context;
    private readonly ILogger<AdminFunctions> _logger;
    private static readonly JsonSerializerOptions JsonOptions = FunctionsJsonSerializerOptions.Default;

    public AdminFunctions(TableStorageContext context, ILogger<AdminFunctions> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Public Methods

    /// <summary>
    /// POST /api/admin/clear-seed — deletes all rows from seed/business tables (not SystemConfig).
    /// Allowed only when DOTNET_ENVIRONMENT=Development or ASPNETCORE_ENVIRONMENT=Development, or BILLINGSYS_ALLOW_CLEAR_SEED=true.
    /// Body: { "confirm": true } required.
    /// </summary>
    [Function("ClearSeed")]
    public async Task<HttpResponseData> ClearSeed(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "admin/clear-seed")] HttpRequestData req)
    {
        if (!IsClearSeedAllowed())
        {
            var forbidden = req.CreateResponse(HttpStatusCode.Forbidden);
            await forbidden.WriteAsJsonAsync(ServiceResult<ClearSeedReport>.Fail(
                "Clear-seed is disabled. Set DOTNET_ENVIRONMENT=Development locally, or set app setting BILLINGSYS_ALLOW_CLEAR_SEED=true on a non-production slot only."));
            return forbidden;
        }

        try
        {
            var body = await req.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(body))
            {
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteAsJsonAsync(ServiceResult<ClearSeedReport>.Fail("Request body is required: { \"confirm\": true }"));
                return bad;
            }

            var parsed = JsonSerializer.Deserialize<ClearSeedRequest>(body, JsonOptions);
            if (parsed is not { Confirm: true })
            {
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteAsJsonAsync(ServiceResult<ClearSeedReport>.Fail("You must send { \"confirm\": true } to delete all seed table rows."));
                return bad;
            }

            var report = await ClearSeedTablesAsync();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(ServiceResult<ClearSeedReport>.Ok(report));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Clear-seed failed");
            var err = req.CreateResponse(HttpStatusCode.InternalServerError);
            await err.WriteAsJsonAsync(ServiceResult<ClearSeedReport>.Fail(ex.Message));
            return err;
        }
    }

    #endregion

    #region Private Methods

    private static bool IsClearSeedAllowed()
    {
        var env = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        if (string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return string.Equals(
            Environment.GetEnvironmentVariable("BILLINGSYS_ALLOW_CLEAR_SEED"),
            "true",
            StringComparison.OrdinalIgnoreCase);
    }

    private async Task<ClearSeedReport> ClearSeedTablesAsync()
    {
        // SystemConfig is intentionally excluded (app initialization / flags).
        var tableNames = new[]
        {
            TableStorageContext.EmployeesTable,
            TableStorageContext.CustomersTable,
            TableStorageContext.ProjectsTable,
            TableStorageContext.TimeEntriesTable,
            TableStorageContext.InvoicesTable,
            TableStorageContext.InvoiceLinesTable,
            TableStorageContext.ServiceItemsTable
        };

        var report = new ClearSeedReport();

        foreach (var tableName in tableNames)
        {
            var deleted = await DeleteAllEntitiesInTableAsync(tableName);
            report.DeletedByTable[tableName] = deleted;
            _logger.LogWarning("[ClearSeed] Removed {Count} entities from table {Table}", deleted, tableName);
        }

        return report;
    }

    private async Task<int> DeleteAllEntitiesInTableAsync(string tableName)
    {
        var table = _context.GetTable(tableName);
        var count = 0;

        try
        {
            await foreach (var entity in table.QueryAsync<TableEntity>())
            {
                await table.DeleteEntityAsync(entity.PartitionKey, entity.RowKey, ETag.All, CancellationToken.None);
                count++;
            }
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogInformation("[ClearSeed] Table {Table} does not exist yet; skipping.", tableName);
        }

        return count;
    }

    #endregion
}

#region Classes

public class ClearSeedRequest
{
    public bool Confirm { get; set; }
}

public class ClearSeedReport
{
    public Dictionary<string, int> DeletedByTable { get; set; } = new();
    public int TotalDeleted => DeletedByTable.Values.Sum();
}

#endregion
