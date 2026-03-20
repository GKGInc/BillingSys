using BillingSys.Shared.DTOs;
using BillingSys.Shared.Models;

namespace BillingSys.Functions.Repositories;

public interface IEdiDataRepository
{
    Task<ServiceResult<List<EdiTradingPartnerSummary>>> GetTradingPartnersByMonthAsync(int year, int month);
    Task<ServiceResult<List<EdiCustomerBillingPreview>>> GetMonthlyBillingPreviewAsync(int year, int month, List<EdiCustomer> ediCustomers);
}
