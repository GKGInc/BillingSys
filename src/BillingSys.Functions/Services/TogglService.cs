using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BillingSys.Functions.Repositories;
using BillingSys.Shared.DTOs;
using BillingSys.Shared.Models;
using Microsoft.Extensions.Logging;

namespace BillingSys.Functions.Services;

public class TogglService
{
    private readonly ITogglImportRepository _togglRepo;
    private readonly ITimeEntryRepository _timeEntryRepo;
    private readonly IProjectRepository _projectRepo;
    private readonly ICustomerRepository _customerRepo;
    private readonly IEmployeeRepository _employeeRepo;
    private readonly IServiceItemRepository _serviceItemRepo;
    private readonly BillingService _billingService;
    private readonly ILogger<TogglService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public TogglService(
        ITogglImportRepository togglRepo,
        ITimeEntryRepository timeEntryRepo,
        IProjectRepository projectRepo,
        ICustomerRepository customerRepo,
        IEmployeeRepository employeeRepo,
        IServiceItemRepository serviceItemRepo,
        BillingService billingService,
        ILogger<TogglService> logger)
    {
        _togglRepo = togglRepo;
        _timeEntryRepo = timeEntryRepo;
        _projectRepo = projectRepo;
        _customerRepo = customerRepo;
        _employeeRepo = employeeRepo;
        _serviceItemRepo = serviceItemRepo;
        _billingService = billingService;
        _logger = logger;
    }

    #region Pull from Toggl

    public async Task<ServiceResult<TogglPullResult>> PullFromTogglAsync(DateTime startDate, DateTime endDate)
    {
        var togglApiToken = Environment.GetEnvironmentVariable("TogglApiToken");
        var togglWorkspaceId = Environment.GetEnvironmentVariable("TogglWorkspaceId");

        if (string.IsNullOrEmpty(togglApiToken) || string.IsNullOrEmpty(togglWorkspaceId))
            return ServiceResult<TogglPullResult>.Fail("Toggl API token or workspace ID not configured");

        try
        {
            var rawEntries = await FetchTogglEntriesAsync(togglApiToken, togglWorkspaceId, startDate, endDate);
            if (!rawEntries.Any())
                return ServiceResult<TogglPullResult>.Ok(new TogglPullResult { BatchId = Guid.NewGuid().ToString() });

            var togglProjects = await FetchTogglProjectsAsync(togglApiToken, togglWorkspaceId);
            var togglClients = await FetchTogglClientsAsync(togglApiToken, togglWorkspaceId);

            var wrigleyProjects = (await _projectRepo.GetAllAsync())?.Data ?? new();
            var wrigleyCustomers = (await _customerRepo.GetAllAsync())?.Data ?? new();
            var wrigleyEmployees = (await _employeeRepo.GetAllAsync())?.Data ?? new();

            var batchId = $"TGL-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
            var imports = new List<TogglImport>();

            foreach (var entry in rawEntries)
            {
                if (entry.Duration < 0) continue;

                var projectName = "";
                var clientName = "";
                if (entry.ProjectId.HasValue && togglProjects.TryGetValue(entry.ProjectId.Value, out var tp))
                {
                    projectName = tp.Name ?? "";
                    if (tp.ClientId.HasValue && togglClients.TryGetValue(tp.ClientId.Value, out var tc))
                        clientName = tc.Name ?? "";
                }

                var employee = wrigleyEmployees.FirstOrDefault(e =>
                    !string.IsNullOrEmpty(e.Email) &&
                    e.Email.Equals(entry.UserEmail, StringComparison.OrdinalIgnoreCase))
                    ?? wrigleyEmployees.FirstOrDefault(e =>
                        e.Name.Equals(entry.UserName, StringComparison.OrdinalIgnoreCase));

                // Match Toggl project name directly to Wrigley ProjectCode
                var mappedProject = wrigleyProjects.FirstOrDefault(p =>
                    p.ProjectCode.Equals(projectName, StringComparison.OrdinalIgnoreCase));

                var import = new TogglImport
                {
                    BatchId = batchId,
                    TogglEntryId = entry.Id,
                    OriginalDescription = entry.Description?.Trim(),
                    TogglProjectName = projectName,
                    TogglClientName = clientName,
                    EmployeeId = employee?.Id ?? "",
                    EmployeeName = employee?.Name ?? entry.UserName ?? "",
                    Date = entry.Start.Date,
                    Hours = Math.Round((decimal)entry.Duration / 3600m, 2),
                    Billable = entry.Billable,
                    TogglTags = entry.Tags != null ? string.Join(",", entry.Tags) : null,
                    MappedProjectCode = mappedProject?.ProjectCode,
                    MappedCustomerId = mappedProject?.CustomerId,
                    Status = TogglImportStatus.Raw
                };

                imports.Add(import);
            }

            var absorbed = AbsorbBlankEntries(imports);

            var saveResult = await _togglRepo.UpsertBatchAsync(imports);
            if (!saveResult.Success)
                return ServiceResult<TogglPullResult>.Fail(saveResult.ErrorMessage ?? "Failed to save imports");

            var groups = BuildGroups(imports, wrigleyCustomers);

            return ServiceResult<TogglPullResult>.Ok(new TogglPullResult
            {
                BatchId = batchId,
                TotalEntriesPulled = rawEntries.Count,
                BlankEntriesAbsorbed = absorbed,
                EntriesReadyForSummary = imports.Count(i => i.Status == TogglImportStatus.Raw),
                Groups = groups
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pulling from Toggl");
            return ServiceResult<TogglPullResult>.Fail(ex.Message);
        }
    }

    #endregion

    #region Blank Absorption

    private int AbsorbBlankEntries(List<TogglImport> imports)
    {
        int absorbedCount = 0;

        var groups = imports
            .GroupBy(i => new { i.TogglProjectName, i.EmployeeId })
            .ToList();

        foreach (var group in groups)
        {
            var sorted = group.OrderBy(i => i.Date).ThenBy(i => i.TogglEntryId).ToList();

            TogglImport? lastDescribed = null;
            var orphanBlanks = new List<TogglImport>();

            foreach (var entry in sorted)
            {
                var hasDescription = !string.IsNullOrWhiteSpace(entry.OriginalDescription);

                if (hasDescription)
                {
                    foreach (var orphan in orphanBlanks)
                    {
                        entry.Hours += orphan.Hours;
                        entry.AbsorbedHours += orphan.Hours;
                        orphan.AbsorbedIntoId = entry.Id;
                        orphan.Status = TogglImportStatus.Absorbed;
                        absorbedCount++;
                    }
                    orphanBlanks.Clear();

                    lastDescribed = entry;
                }
                else
                {
                    if (lastDescribed != null)
                    {
                        lastDescribed.Hours += entry.Hours;
                        lastDescribed.AbsorbedHours += entry.Hours;
                        entry.AbsorbedIntoId = lastDescribed.Id;
                        entry.Status = TogglImportStatus.Absorbed;
                        absorbedCount++;
                    }
                    else
                    {
                        orphanBlanks.Add(entry);
                    }
                }
            }

            foreach (var orphan in orphanBlanks)
            {
                orphan.OriginalDescription = "(no description)";
            }
        }

        return absorbedCount;
    }

    #endregion

    #region Summarize with Claude

    public async Task<ServiceResult<TogglSummaryResult>> SummarizeAsync(string batchId)
    {
        var claudeApiKey = Environment.GetEnvironmentVariable("ClaudeApiKey");
        if (string.IsNullOrEmpty(claudeApiKey))
            return ServiceResult<TogglSummaryResult>.Fail("Claude API key not configured");

        try
        {
            var batchResult = await _togglRepo.GetByBatchAsync(batchId);
            if (!batchResult.Success)
                return ServiceResult<TogglSummaryResult>.Fail(batchResult.ErrorMessage ?? "Batch not found");

            var entries = batchResult.Data!
                .Where(e => e.Status == TogglImportStatus.Raw)
                .ToList();

            if (!entries.Any())
                return ServiceResult<TogglSummaryResult>.Fail("No entries to summarize in this batch");

            var projectGroups = entries
                .GroupBy(e => new { e.TogglProjectName, e.TogglClientName, e.MappedProjectCode })
                .ToList();

            var summaryResult = new TogglSummaryResult { BatchId = batchId, Groups = new() };

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("x-api-key", claudeApiKey);
            httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

            foreach (var group in projectGroups)
            {
                var entryLines = group.Select(e =>
                {
                    var absorbed = e.AbsorbedHours > 0 ? $" (includes {e.AbsorbedHours}h from unlabeled entries)" : "";
                    return $"  {e.EmployeeName} | {e.Date:yyyy-MM-dd} | {e.Hours}h{absorbed} | {e.OriginalDescription}";
                });

                var prompt = $@"You are creating invoice line item descriptions from developer time tracking entries.

Project: {group.Key.TogglProjectName ?? "General"}
Client: {group.Key.TogglClientName ?? "Unknown"}

Here are the detailed time entries:
{string.Join("\n", entryLines)}

Instructions:
- Group entries that describe similar or related work into a single invoice line
- Write concise, professional descriptions appropriate for a client invoice
- Do not include hours, rates, or amounts (those are separate columns)
- Do not include internal ticket numbers, debug details, or developer jargon
- Use active past-tense language (e.g. ""Configured EDI integration"" not ""EDI configuration work"")
- Preserve total hours accurately when grouping
- A client should understand what they are paying for from the description alone

Respond with ONLY a JSON array. Each element:
{{""entry_indices"": [0-based indices of entries grouped into this line], ""description"": ""Invoice line description"", ""total_hrs"": number}}

No other text, no markdown fences, just the JSON array.";

                var claudeRequest = new
                {
                    model = "claude-sonnet-4-20250514",
                    max_tokens = 2000,
                    messages = new[] { new { role = "user", content = prompt } }
                };

                var json = JsonSerializer.Serialize(claudeRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync("https://api.anthropic.com/v1/messages", content);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                var claudeResponse = JsonSerializer.Deserialize<ClaudeResponse>(responseJson, JsonOptions);

                var responseText = string.Join("",
                    claudeResponse?.Content?.Where(c => c.Type == "text").Select(c => c.Text) ?? Array.Empty<string>());

                responseText = responseText.Trim();
                if (responseText.StartsWith("```"))
                    responseText = responseText.Split('\n', 2).Length > 1 ? responseText.Split('\n', 2)[1] : responseText[3..];
                if (responseText.EndsWith("```"))
                    responseText = responseText[..^3];
                responseText = responseText.Trim();

                var summaryLines = JsonSerializer.Deserialize<List<ClaudeInvoiceLine>>(responseText, JsonOptions);
                if (summaryLines == null) continue;

                var groupEntries = group.ToList();
                var summaryGroup = new TogglSummaryGroup
                {
                    GroupKey = group.Key.TogglProjectName ?? "General",
                    ProjectName = group.Key.TogglProjectName,
                    CustomerName = group.Key.TogglClientName,
                    Lines = new()
                };

                foreach (var line in summaryLines)
                {
                    var billingGroupKey = Guid.NewGuid().ToString()[..8];
                    var matchedEntries = new List<TogglImport>();

                    foreach (var idx in line.EntryIndices ?? new())
                    {
                        if (idx >= 0 && idx < groupEntries.Count)
                            matchedEntries.Add(groupEntries[idx]);
                    }

                    foreach (var entry in matchedEntries)
                    {
                        entry.SummarizedDescription = line.Description;
                        entry.BillingGroupKey = billingGroupKey;
                        entry.Status = TogglImportStatus.Summarized;
                        entry.StampUpdated();
                    }

                    summaryGroup.Lines.Add(new TogglSummaryLine
                    {
                        BillingGroupKey = billingGroupKey,
                        SummarizedDescription = line.Description ?? "",
                        TotalHours = line.TotalHrs,
                        EntryIds = matchedEntries.Select(e => e.Id).ToList(),
                        OriginalDescriptions = matchedEntries
                            .Select(e => e.OriginalDescription ?? "").ToList()
                    });
                }

                summaryResult.Groups.Add(summaryGroup);

                foreach (var entry in groupEntries.Where(e => e.Status == TogglImportStatus.Summarized))
                {
                    await _togglRepo.UpsertAsync(entry);
                }
            }

            return ServiceResult<TogglSummaryResult>.Ok(summaryResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error summarizing batch {BatchId}", batchId);
            return ServiceResult<TogglSummaryResult>.Fail(ex.Message);
        }
    }

    #endregion

    #region Invoice Preview

    public async Task<ServiceResult<TogglInvoicePreview>> GenerateInvoicePreviewAsync(string batchId, DateTime invoiceDate)
    {
        try
        {
            var batchResult = await _togglRepo.GetByBatchAsync(batchId);
            if (!batchResult.Success)
                return ServiceResult<TogglInvoicePreview>.Fail(batchResult.ErrorMessage ?? "Batch not found");

            var entries = batchResult.Data!
                .Where(e => e.Status == TogglImportStatus.Summarized)
                .ToList();

            if (!entries.Any())
                return ServiceResult<TogglInvoicePreview>.Fail("No summarized entries. Run summarization first.");

            var allProjects = (await _projectRepo.GetAllAsync())?.Data ?? new();
            var projectLookup = allProjects.ToDictionary(p => p.ProjectCode, StringComparer.OrdinalIgnoreCase);

            var allCustomers = (await _customerRepo.GetAllAsync(false))?.Data ?? new();
            var customerLookup = allCustomers.ToDictionary(c => c.CustomerId);

            var allServiceItems = (await _serviceItemRepo.GetAllAsync())?.Data ?? new();
            var itemLookup = allServiceItems.ToDictionary(i => i.ItemCode, StringComparer.OrdinalIgnoreCase);

            var customerGroups = new Dictionary<string, TogglCustomerInvoicePreview>();

            var billingGroups = entries
                .GroupBy(e => e.BillingGroupKey ?? e.Id)
                .ToList();

            foreach (var group in billingGroups)
            {
                var representative = group.First();
                var projectCode = representative.MappedProjectCode ?? representative.TogglProjectName ?? "";

                Project? project = null;
                if (!string.IsNullOrEmpty(projectCode))
                    projectLookup.TryGetValue(projectCode, out project);

                if (project == null)
                {
                    _logger.LogWarning("No matching project for '{ProjectCode}', skipping", projectCode);
                    continue;
                }

                var customerId = project.CustomerId;
                var customerName = customerLookup.TryGetValue(customerId, out var cust)
                    ? cust.Company : customerId;

                var rate = project.Price;
                if (itemLookup.TryGetValue(project.ServiceItemCode, out var serviceItem))
                    rate = serviceItem.Price;

                if (!customerGroups.TryGetValue(customerId, out var customerPreview))
                {
                    customerPreview = new TogglCustomerInvoicePreview
                    {
                        CustomerId = customerId,
                        CustomerName = customerName,
                        Selected = true
                    };
                    customerGroups[customerId] = customerPreview;
                }

                var totalHours = group.Sum(e => e.Hours);
                var description = representative.SummarizedDescription ?? "(no description)";

                customerPreview.Lines.Add(new TogglInvoiceLine
                {
                    BillingGroupKey = group.Key,
                    ProjectCode = project.ProjectCode,
                    ItemCode = project.ServiceItemCode,
                    Description = description,
                    FormattedDescription = description,
                    Hours = totalHours,
                    Rate = rate,
                    ServiceDate = group.Min(e => e.Date),
                    OriginalDescriptions = group
                        .Select(e => e.OriginalDescription ?? "")
                        .Where(d => !string.IsNullOrEmpty(d))
                        .ToList(),
                    EntryIds = group.Select(e => e.Id).ToList()
                });

                customerPreview.TotalHours += totalHours;
                customerPreview.TotalAmount += Math.Round(totalHours * rate, 2);
            }

            return ServiceResult<TogglInvoicePreview>.Ok(new TogglInvoicePreview
            {
                BatchId = batchId,
                InvoiceDate = invoiceDate,
                Customers = customerGroups.Values.ToList(),
                GrandTotal = customerGroups.Values.Sum(c => c.TotalAmount)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating invoice preview for batch {BatchId}", batchId);
            return ServiceResult<TogglInvoicePreview>.Fail(ex.Message);
        }
    }

    #endregion

    #region Post Invoices

    public async Task<ServiceResult<TogglPostInvoicesResult>> PostInvoicesAsync(
        string batchId, DateTime invoiceDate, List<string> selectedCustomerIds)
    {
        try
        {
            var previewResult = await GenerateInvoicePreviewAsync(batchId, invoiceDate);
            if (!previewResult.Success)
                return ServiceResult<TogglPostInvoicesResult>.Fail(previewResult.ErrorMessage ?? "Failed to build preview");

            var customersToProcess = previewResult.Data!.Customers
                .Where(c => selectedCustomerIds.Contains(c.CustomerId))
                .ToList();

            var result = new TogglPostInvoicesResult();

            foreach (var customerPreview in customersToProcess)
            {
                var billingPreview = new CustomerBillingPreview
                {
                    CustomerId = customerPreview.CustomerId,
                    CustomerName = customerPreview.CustomerName,
                    Selected = true,
                    TotalHours = customerPreview.TotalHours,
                    TotalAmount = customerPreview.TotalAmount
                };

                foreach (var line in customerPreview.Lines)
                {
                    billingPreview.Lines.Add(new BillingLinePreview
                    {
                        ItemCode = line.ItemCode,
                        Description = (line.FormattedDescription ?? line.Description).ToUpperInvariant(),
                        Hours = line.Hours,
                        Price = line.Rate,
                        ServiceDate = line.ServiceDate
                    });
                }

                var invoiceResult = await _billingService.CreateInvoiceFromPreviewAsync(billingPreview, invoiceDate);
                if (invoiceResult.Success && invoiceResult.Data != null)
                {
                    result.Invoices.Add(new TogglPostedInvoice
                    {
                        InvoiceNumber = invoiceResult.Data.InvoiceNumber,
                        CustomerName = customerPreview.CustomerName,
                        Amount = invoiceResult.Data.InvoiceAmount,
                        Status = invoiceResult.Data.Status.ToString()
                    });
                    result.InvoicesCreated++;

                    var batchEntries = await _togglRepo.GetByBatchAsync(batchId);
                    if (batchEntries.Success)
                    {
                        var entryIds = customerPreview.Lines.SelectMany(l => l.EntryIds).ToHashSet();
                        foreach (var entry in batchEntries.Data!.Where(e => entryIds.Contains(e.Id)))
                        {
                            entry.Status = TogglImportStatus.Imported;
                            entry.TimeEntryId = invoiceResult.Data.InvoiceNumber;
                            entry.StampUpdated();
                            await _togglRepo.UpsertAsync(entry);
                        }
                    }
                }
            }

            return ServiceResult<TogglPostInvoicesResult>.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error posting invoices for batch {BatchId}", batchId);
            return ServiceResult<TogglPostInvoicesResult>.Fail(ex.Message);
        }
    }

    #endregion

    #region Edit Summary

    public async Task<ServiceResult> EditSummaryAsync(string batchId, string entryId, string newSummary)
    {
        try
        {
            var batchResult = await _togglRepo.GetByBatchAsync(batchId);
            if (!batchResult.Success)
                return ServiceResult.Fail(batchResult.ErrorMessage ?? "Batch not found");

            var entry = batchResult.Data!.FirstOrDefault(e => e.Id == entryId);
            if (entry == null)
                return ServiceResult.Fail("Entry not found");

            entry.SummarizedDescription = newSummary;
            entry.StampUpdated();
            await _togglRepo.UpsertAsync(entry);

            return ServiceResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error editing summary for entry {EntryId}", entryId);
            return ServiceResult.Fail(ex.Message);
        }
    }

    #endregion

    #region Load Batch

    public async Task<ServiceResult<TogglPullResult>> LoadBatchAsync(string batchId)
    {
        try
        {
            var batchResult = await _togglRepo.GetByBatchAsync(batchId);
            if (!batchResult.Success)
                return ServiceResult<TogglPullResult>.Fail(batchResult.ErrorMessage ?? "Batch not found");

            var imports = batchResult.Data!;
            var customers = (await _customerRepo.GetAllAsync())?.Data ?? new();

            return ServiceResult<TogglPullResult>.Ok(new TogglPullResult
            {
                BatchId = batchId,
                TotalEntriesPulled = imports.Count,
                BlankEntriesAbsorbed = imports.Count(i => i.Status == TogglImportStatus.Absorbed),
                EntriesReadyForSummary = imports.Count(i => i.Status == TogglImportStatus.Raw || i.Status == TogglImportStatus.Summarized),
                Groups = BuildGroups(imports, customers)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading batch {BatchId}", batchId);
            return ServiceResult<TogglPullResult>.Fail(ex.Message);
        }
    }

    #endregion

    #region Private Helpers

    private List<TogglImportGroup> BuildGroups(List<TogglImport> imports, List<Customer> customers)
    {
        return imports
            .Where(i => i.Status != TogglImportStatus.Absorbed)
            .GroupBy(i => new { i.TogglProjectName, i.TogglClientName, i.MappedProjectCode, i.MappedCustomerId })
            .Select(g =>
            {
                var customerName = "";
                if (!string.IsNullOrEmpty(g.Key.MappedCustomerId))
                    customerName = customers.FirstOrDefault(c => c.CustomerId == g.Key.MappedCustomerId)?.Company ?? "";

                return new TogglImportGroup
                {
                    GroupKey = g.Key.TogglProjectName ?? "Unmapped",
                    TogglProjectName = g.Key.TogglProjectName,
                    TogglClientName = g.Key.TogglClientName,
                    MappedProjectCode = g.Key.MappedProjectCode,
                    MappedCustomerId = g.Key.MappedCustomerId,
                    MappedCustomerName = customerName,
                    TotalHours = g.Sum(e => e.Hours),
                    EntryCount = g.Count(),
                    Entries = g.Select(e => new TogglImportLine
                    {
                        Id = e.Id,
                        TogglEntryId = e.TogglEntryId,
                        EmployeeName = e.EmployeeName,
                        Date = e.Date,
                        Hours = e.Hours,
                        AbsorbedHours = e.AbsorbedHours,
                        OriginalDescription = e.OriginalDescription,
                        SummarizedDescription = e.SummarizedDescription,
                        Status = e.Status.ToString()
                    }).ToList()
                };
            })
            .ToList();
    }

    private async Task<List<TogglTimeEntry>> FetchTogglEntriesAsync(
        string apiToken, string workspaceId, DateTime start, DateTime end)
    {
        using var http = new HttpClient();
        var authBytes = Encoding.ASCII.GetBytes($"{apiToken}:api_token");
        http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));

        var url = $"https://api.track.toggl.com/api/v9/me/time_entries" +
                  $"?start_date={start:yyyy-MM-dd}T00:00:00Z&end_date={end:yyyy-MM-dd}T23:59:59Z";

        var response = await http.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<TogglTimeEntry>>(json, JsonOptions) ?? new();
    }

    private async Task<Dictionary<long, TogglProject>> FetchTogglProjectsAsync(string apiToken, string workspaceId)
    {
        using var http = new HttpClient();
        var authBytes = Encoding.ASCII.GetBytes($"{apiToken}:api_token");
        http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));

        var url = $"https://api.track.toggl.com/api/v9/workspaces/{workspaceId}/projects";
        var response = await http.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var projects = JsonSerializer.Deserialize<List<TogglProject>>(json, JsonOptions) ?? new();
        return projects.ToDictionary(p => p.Id);
    }

    private async Task<Dictionary<long, TogglClient>> FetchTogglClientsAsync(string apiToken, string workspaceId)
    {
        using var http = new HttpClient();
        var authBytes = Encoding.ASCII.GetBytes($"{apiToken}:api_token");
        http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));

        var url = $"https://api.track.toggl.com/api/v9/workspaces/{workspaceId}/clients";
        var response = await http.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var clients = JsonSerializer.Deserialize<List<TogglClient>>(json, JsonOptions) ?? new();
        return clients.ToDictionary(c => c.Id);
    }

    #endregion
}

#region Toggl API Response Models

internal class TogglTimeEntry
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("project_id")]
    public long? ProjectId { get; set; }

    [JsonPropertyName("start")]
    public DateTime Start { get; set; }

    [JsonPropertyName("duration")]
    public long Duration { get; set; }

    [JsonPropertyName("billable")]
    public bool Billable { get; set; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }

    [JsonPropertyName("user_id")]
    public long UserId { get; set; }

    [JsonPropertyName("user_name")]
    public string? UserName { get; set; }

    [JsonPropertyName("user_email")]
    public string? UserEmail { get; set; }
}

internal class TogglProject
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("client_id")]
    public long? ClientId { get; set; }
}

internal class TogglClient
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

internal class ClaudeResponse
{
    [JsonPropertyName("content")]
    public List<ClaudeContentBlock>? Content { get; set; }
}

internal class ClaudeContentBlock
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

internal class ClaudeInvoiceLine
{
    [JsonPropertyName("entry_indices")]
    public List<int>? EntryIndices { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("total_hrs")]
    public decimal TotalHrs { get; set; }
}

#endregion
