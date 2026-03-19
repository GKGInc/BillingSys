using BillingSys.Shared.DTOs;
using BillingSys.Shared.Models;
using Microsoft.Extensions.Logging;

namespace BillingSys.Functions.Services;

public class BillingService
{
    private readonly TableStorageService _storage;
    private readonly ILogger<BillingService> _logger;

    public BillingService(TableStorageService storage, ILogger<BillingService> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    #region Public Methods

    public async Task<ServiceResult<WeeklyBillingPreview>> GetWeeklyBillingPreviewAsync(int year, int weekNumber)
    {
        try
        {
            var entriesResult = await _storage.GetTimeEntriesAsync(year, weekNumber);
            if (!entriesResult.Success)
            {
                return ServiceResult<WeeklyBillingPreview>.Fail(entriesResult.ErrorMessage ?? "Failed to get time entries");
            }

            var billableEntries = entriesResult.Data!
                .Where(e => e.Billable && e.Status == TimeEntryStatus.Pending || e.Status == TimeEntryStatus.Approved)
                .ToList();

            var projectsResult = await _storage.GetAllProjectsAsync();
            var customersResult = await _storage.GetAllCustomersAsync(false);
            var itemsResult = await _storage.GetAllServiceItemsAsync();

            var projects = projectsResult.Success ? projectsResult.Data!.ToDictionary(p => p.ProjectCode) : new Dictionary<string, Project>();
            var customers = customersResult.Success ? customersResult.Data!.ToDictionary(c => c.CustomerId) : new Dictionary<string, Customer>();
            var items = itemsResult.Success ? itemsResult.Data!.ToDictionary(i => i.ItemCode) : new Dictionary<string, ServiceItem>();

            var customerGroups = new Dictionary<string, CustomerBillingPreview>();

            foreach (var entry in billableEntries)
            {
                if (!projects.TryGetValue(entry.ProjectCode, out var project))
                {
                    _logger.LogWarning("Project {ProjectCode} not found for time entry {EntryId}", entry.ProjectCode, entry.Id);
                    continue;
                }

                if (ShouldExcludeFromBilling(project))
                {
                    continue;
                }

                var customerId = project.CustomerId;
                if (!customerGroups.TryGetValue(customerId, out var customerPreview))
                {
                    var customerName = customers.TryGetValue(customerId, out var cust) ? cust.Company : customerId;
                    customerPreview = new CustomerBillingPreview
                    {
                        CustomerId = customerId,
                        CustomerName = customerName,
                        Selected = true
                    };
                    customerGroups[customerId] = customerPreview;
                }

                var itemCode = project.ServiceItemCode;
                var price = items.TryGetValue(itemCode, out var item) ? item.Price : project.Price;

                var line = new BillingLinePreview
                {
                    ItemCode = itemCode,
                    Description = project.Description.ToUpperInvariant(),
                    Memo = FormatMemo(entry),
                    Hours = entry.Hours,
                    Price = entry.Billable ? price : 0,
                    ServiceDate = entry.Date,
                    EmployeeId = entry.EmployeeId
                };

                customerPreview.Lines.Add(line);
                customerPreview.TotalHours += entry.Hours;
                customerPreview.TotalAmount += line.Amount;
            }

            var preview = new WeeklyBillingPreview
            {
                WeekNumber = weekNumber,
                Year = year,
                Customers = customerGroups.Values.ToList(),
                TotalAmount = customerGroups.Values.Sum(c => c.TotalAmount),
                TotalHours = (int)customerGroups.Values.Sum(c => c.TotalHours)
            };

            return ServiceResult<WeeklyBillingPreview>.Ok(preview);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating weekly billing preview for {Year} week {Week}", year, weekNumber);
            return ServiceResult<WeeklyBillingPreview>.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult<List<Invoice>>> ProcessWeeklyBillingAsync(WeeklyBillingRequest request, List<string> selectedCustomerIds)
    {
        try
        {
            var previewResult = await GetWeeklyBillingPreviewAsync(request.Year, request.WeekNumber);
            if (!previewResult.Success)
            {
                return ServiceResult<List<Invoice>>.Fail(previewResult.ErrorMessage ?? "Failed to get preview");
            }

            var invoices = new List<Invoice>();
            var customersToProcess = previewResult.Data!.Customers
                .Where(c => selectedCustomerIds.Contains(c.CustomerId))
                .ToList();

            foreach (var customerPreview in customersToProcess)
            {
                var invoiceResult = await CreateInvoiceFromPreviewAsync(customerPreview, request.InvoiceDate);
                if (invoiceResult.Success && invoiceResult.Data != null)
                {
                    invoices.Add(invoiceResult.Data);
                }
                else
                {
                    _logger.LogError("Failed to create invoice for customer {CustomerId}: {Error}", 
                        customerPreview.CustomerId, invoiceResult.ErrorMessage);
                }
            }

            return ServiceResult<List<Invoice>>.Ok(invoices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing weekly billing");
            return ServiceResult<List<Invoice>>.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult<Invoice>> CreateInvoiceFromPreviewAsync(CustomerBillingPreview preview, DateTime invoiceDate)
    {
        try
        {
            var invoiceNumberResult = await _storage.GetNextInvoiceNumberAsync(invoiceDate);
            if (!invoiceNumberResult.Success)
            {
                return ServiceResult<Invoice>.Fail(invoiceNumberResult.ErrorMessage ?? "Failed to get invoice number");
            }

            var consolidatedLines = ConsolidateLines(preview.Lines);

            var invoice = new Invoice
            {
                InvoiceNumber = invoiceNumberResult.Data!,
                InvoiceDate = invoiceDate,
                CustomerId = preview.CustomerId,
                CustomerName = preview.CustomerName,
                PurchaseOrderNumber = "Verbal",
                InvoiceAmount = consolidatedLines.Sum(l => l.Amount),
                OrderNumber = "None",
                Status = InvoiceStatus.Posted,
                CreatedAt = DateTime.UtcNow
            };

            var lineNumber = 0;
            foreach (var line in consolidatedLines)
            {
                lineNumber++;
                invoice.Lines.Add(new InvoiceLine
                {
                    InvoiceNumber = invoice.InvoiceNumber,
                    LineNumber = lineNumber,
                    ItemCode = line.ItemCode,
                    Description = line.Description,
                    UnitOfMeasure = "HR",
                    Quantity = line.Hours,
                    Price = line.Price,
                    Memo = line.Memo,
                    ServiceDate = line.ServiceDate,
                    EmployeeId = line.EmployeeId
                });
            }

            var result = await _storage.UpsertInvoiceAsync(invoice);
            if (!result.Success)
            {
                return ServiceResult<Invoice>.Fail(result.ErrorMessage ?? "Failed to save invoice");
            }

            return ServiceResult<Invoice>.Ok(invoice);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating invoice for customer {CustomerId}", preview.CustomerId);
            return ServiceResult<Invoice>.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult<Invoice>> CreateProjectInvoiceAsync(ProjectBillingRequest request)
    {
        try
        {
            var customersResult = await _storage.GetAllCustomersAsync(false);
            var customer = customersResult.Data?.FirstOrDefault(c => c.CustomerId == request.CustomerId);
            
            var invoiceNumberResult = await _storage.GetNextInvoiceNumberAsync(request.InvoiceDate);
            if (!invoiceNumberResult.Success)
            {
                return ServiceResult<Invoice>.Fail(invoiceNumberResult.ErrorMessage ?? "Failed to get invoice number");
            }

            var invoice = new Invoice
            {
                InvoiceNumber = invoiceNumberResult.Data!,
                InvoiceDate = request.InvoiceDate,
                CustomerId = request.CustomerId,
                CustomerName = customer?.Company,
                PurchaseOrderNumber = "Verbal",
                Status = InvoiceStatus.Posted,
                CreatedAt = DateTime.UtcNow
            };

            decimal totalAmount = 0;
            var lineNumber = 0;

            foreach (var projectLine in request.Projects.Where(p => p.HoursToBill > 0))
            {
                var projectResult = await _storage.GetProjectAsync(request.CustomerId, projectLine.ProjectCode);
                if (!projectResult.Success || projectResult.Data == null)
                {
                    _logger.LogWarning("Project {ProjectCode} not found", projectLine.ProjectCode);
                    continue;
                }

                var project = projectResult.Data;
                lineNumber++;

                var extendedPrice = Math.Round(projectLine.HoursToBill * project.Price, 2);
                totalAmount += extendedPrice;

                invoice.Lines.Add(new InvoiceLine
                {
                    InvoiceNumber = invoice.InvoiceNumber,
                    LineNumber = lineNumber,
                    ItemCode = project.ServiceItemCode,
                    Description = $"{project.ProjectCode}-{project.Description}".ToUpperInvariant(),
                    UnitOfMeasure = "EACH",
                    Quantity = projectLine.HoursToBill,
                    Price = project.Price
                });

                if (!string.IsNullOrEmpty(project.CustomerPO))
                {
                    lineNumber++;
                    invoice.Lines.Add(new InvoiceLine
                    {
                        InvoiceNumber = invoice.InvoiceNumber,
                        LineNumber = lineNumber,
                        ItemCode = project.ServiceItemCode,
                        Description = $"PO: {project.CustomerPO}",
                        UnitOfMeasure = "EACH",
                        Quantity = 0,
                        Price = 0
                    });
                }

                project.BilledHours += projectLine.HoursToBill;
                project.UpdatedAt = DateTime.UtcNow;
                await _storage.UpsertProjectAsync(project);
            }

            invoice.InvoiceAmount = totalAmount;
            invoice.OrderNumber = request.Projects.Count > 1 ? "MULT" : request.Projects.FirstOrDefault()?.ProjectCode ?? "None";

            var result = await _storage.UpsertInvoiceAsync(invoice);
            if (!result.Success)
            {
                return ServiceResult<Invoice>.Fail(result.ErrorMessage ?? "Failed to save invoice");
            }

            return ServiceResult<Invoice>.Ok(invoice);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating project invoice");
            return ServiceResult<Invoice>.Fail(ex.Message);
        }
    }

    #endregion

    #region Private Methods

    private static bool ShouldExcludeFromBilling(Project project)
    {
        if (project.CustomerId.Contains("GKG", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (project.ServiceItemCode.Equals("TRAVEL", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (project.ProjectCode.Length >= 6 && 
            int.TryParse(project.ProjectCode.Substring(3), out var soNumber) && 
            soNumber > 0)
        {
            return true;
        }

        return false;
    }

    private static string FormatMemo(TimeEntry entry)
    {
        var parts = new List<string>();
        
        if (!entry.Billable)
        {
            parts.Add("(NOT BILLED)");
        }
        
        parts.Add(entry.Date.ToString("MM/dd/yyyy"));
        parts.Add($"({entry.EmployeeId})");
        
        if (!string.IsNullOrEmpty(entry.Comments))
        {
            parts.Add(entry.Comments);
        }

        return string.Join(" ", parts);
    }

    private static List<BillingLinePreview> ConsolidateLines(List<BillingLinePreview> lines)
    {
        var consolidated = new List<BillingLinePreview>();

        var groups = lines
            .OrderBy(l => l.ItemCode)
            .ThenBy(l => l.Price == 0 ? 0 : 1)
            .ThenBy(l => l.ServiceDate)
            .ThenBy(l => l.EmployeeId)
            .ThenBy(l => l.Description)
            .GroupBy(l => new 
            { 
                l.ItemCode, 
                IsBillable = l.Price > 0, 
                l.ServiceDate, 
                l.EmployeeId, 
                l.Description 
            });

        foreach (var group in groups)
        {
            var items = group.ToList();
            var first = items.First();
            
            var combinedMemo = string.Join("\n", items.Select(i => i.Memo).Where(m => !string.IsNullOrEmpty(m)));

            consolidated.Add(new BillingLinePreview
            {
                ItemCode = first.ItemCode,
                Description = first.Description,
                Memo = combinedMemo,
                Hours = items.Sum(i => i.Hours),
                Price = first.Price,
                ServiceDate = first.ServiceDate,
                EmployeeId = first.EmployeeId
            });
        }

        return consolidated;
    }

    #endregion
}
