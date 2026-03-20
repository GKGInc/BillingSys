using Azure;
using BillingSys.Functions.Services;
using BillingSys.Shared.Models;
using Microsoft.Extensions.Logging;

namespace BillingSys.Functions.Repositories;

public class InvoiceRepository : IInvoiceRepository
{
    private readonly TableStorageContext _context;
    private readonly ILogger<InvoiceRepository> _logger;

    public InvoiceRepository(TableStorageContext context, ILogger<InvoiceRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Public Methods

    public async Task<ServiceResult<Invoice>> GetAsync(string yearMonth, string invoiceNumber)
    {
        try
        {
            var table = _context.GetTable(TableStorageContext.InvoicesTable);
            var response = await table.GetEntityAsync<InvoiceEntity>(yearMonth, invoiceNumber);
            return ServiceResult<Invoice>.Ok(response.Value.ToModel());
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return ServiceResult<Invoice>.Fail($"Invoice {invoiceNumber} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting invoice {InvoiceNumber}", invoiceNumber);
            return ServiceResult<Invoice>.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult<List<Invoice>>> GetByMonthAsync(int year, int month)
    {
        try
        {
            var table = _context.GetTable(TableStorageContext.InvoicesTable);
            var partitionKey = $"{year}-{month:D2}";
            var filter = $"PartitionKey eq '{partitionKey}'";
            var invoices = new List<Invoice>();
            await foreach (var entity in table.QueryAsync<InvoiceEntity>(filter))
            {
                invoices.Add(entity.ToModel());
            }
            return ServiceResult<List<Invoice>>.Ok(invoices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting invoices for {Year}-{Month}", year, month);
            return ServiceResult<List<Invoice>>.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult<string>> GetNextInvoiceNumberAsync(DateTime invoiceDate)
    {
        try
        {
            var table = _context.GetTable(TableStorageContext.InvoicesTable);
            var partitionKey = $"{invoiceDate.Year}-{invoiceDate.Month:D2}";
            var baseNumber = long.Parse($"{invoiceDate:yyyyMMdd}00");
            var maxNumber = baseNumber;

            await foreach (var entity in table.QueryAsync<InvoiceEntity>($"PartitionKey eq '{partitionKey}'"))
            {
                if (long.TryParse(entity.RowKey, out var num) && num > maxNumber)
                {
                    maxNumber = num;
                }
            }

            return ServiceResult<string>.Ok((maxNumber + 1).ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting next invoice number");
            return ServiceResult<string>.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult<Invoice>> UpsertAsync(Invoice invoice)
    {
        try
        {
            var table = _context.GetTable(TableStorageContext.InvoicesTable);
            var entity = InvoiceEntity.FromModel(invoice);
            await table.UpsertEntityAsync(entity);
            return ServiceResult<Invoice>.Ok(invoice);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting invoice {InvoiceNumber}", invoice.InvoiceNumber);
            return ServiceResult<Invoice>.Fail(ex.Message);
        }
    }

    #endregion
}
