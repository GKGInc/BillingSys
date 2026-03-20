using BillingSys.Shared.Models;

namespace BillingSys.Functions.Repositories;

public interface IInvoiceRepository
{
    Task<ServiceResult<Invoice>> GetAsync(string yearMonth, string invoiceNumber);
    Task<ServiceResult<List<Invoice>>> GetByMonthAsync(int year, int month);
    Task<ServiceResult<string>> GetNextInvoiceNumberAsync(DateTime invoiceDate);
    Task<ServiceResult<Invoice>> UpsertAsync(Invoice invoice);
}
