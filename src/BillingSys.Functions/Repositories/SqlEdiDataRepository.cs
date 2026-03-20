using BillingSys.Shared.DTOs;
using BillingSys.Shared.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace BillingSys.Functions.Repositories;

public class SqlEdiDataRepository : IEdiDataRepository
{
    private readonly string _connectionString;
    private readonly ILogger<SqlEdiDataRepository> _logger;

    public SqlEdiDataRepository(string connectionString, ILogger<SqlEdiDataRepository> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    #region Public Methods

    public async Task<ServiceResult<List<EdiTradingPartnerSummary>>> GetTradingPartnersByMonthAsync(int year, int month)
    {
        if (string.IsNullOrEmpty(_connectionString))
        {
            return ServiceResult<List<EdiTradingPartnerSummary>>.Fail("SQL connection string not configured");
        }

        try
        {
            var startDate = $"{year}{month:D2}01";
            var endDay = DateTime.DaysInMonth(year, month);
            var endDate = $"{year}{month:D2}{endDay:D2}";

            var sql = @"
                SELECT DISTINCT
                    e.customer_number as CustomerNumber,
                    e.customer_name as CustomerName,
                    e.trading_partner as TradingPartnerCode,
                    e.tp_bill_type as BillType
                FROM gkgwebdb.dbo.edi_transactions e
                WHERE e.document_type NOT IN ('864', '816', '997')
                    AND e.transaction_dt BETWEEN @StartDate AND @EndDate
                ORDER BY e.customer_name, e.trading_partner";

            var results = new List<EdiTradingPartnerSummary>();

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@StartDate", startDate);
            command.Parameters.AddWithValue("@EndDate", endDate);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var tpCode = reader.GetString(2);
                var billType = reader.IsDBNull(3) ? "" : reader.GetString(3);

                if (string.IsNullOrEmpty(billType))
                {
                    billType = tpCode == "_" ? "PDF" : "X12";
                }

                results.Add(new EdiTradingPartnerSummary
                {
                    CustomerNumber = reader.GetString(0),
                    CustomerName = reader.GetString(1),
                    TradingPartnerCode = tpCode,
                    BillType = billType
                });
            }

            results = ApplyTradingPartnerMappings(results);

            return ServiceResult<List<EdiTradingPartnerSummary>>.Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting trading partners for {Year}/{Month}", year, month);
            return ServiceResult<List<EdiTradingPartnerSummary>>.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult<List<EdiCustomerBillingPreview>>> GetMonthlyBillingPreviewAsync(
        int year, int month, List<EdiCustomer> ediCustomers)
    {
        try
        {
            var tradingPartnersResult = await GetTradingPartnersByMonthAsync(year, month);
            if (!tradingPartnersResult.Success)
            {
                return ServiceResult<List<EdiCustomerBillingPreview>>.Fail(tradingPartnersResult.ErrorMessage ?? "Failed to get trading partners");
            }

            var tradingPartnerNames = await GetTradingPartnerNamesAsync();
            var kilocharData = await GetKilocharDataAsync(year, month);

            var previews = new List<EdiCustomerBillingPreview>();

            foreach (var ediCustomer in ediCustomers.Where(c => c.IsActive))
            {
                var customerTps = tradingPartnersResult.Data!
                    .Where(tp => tp.CustomerNumber == ediCustomer.SiteId)
                    .ToList();

                if (!customerTps.Any() && ediCustomer.MinimumFee == 0)
                {
                    continue;
                }

                var preview = new EdiCustomerBillingPreview
                {
                    SiteId = ediCustomer.SiteId,
                    CustomerId = ediCustomer.CustomerId,
                    CustomerName = ediCustomer.CustomerName,
                    Selected = true
                };

                if (ediCustomer.MinimumFee > 0)
                {
                    preview.TotalAmount = ediCustomer.MinimumFee;
                }
                else
                {
                    var ediTps = customerTps.Where(tp => tp.BillType == "X12" && tp.TradingPartnerCode != "_" &&
                        tp.TradingPartnerCode != "QRS" && tp.TradingPartnerCode != "INT").ToList();
                    var nonEdiTps = customerTps.Where(tp => tp.TradingPartnerCode == "_").ToList();
                    var pdfTps = customerTps.Where(tp => tp.BillType == "PDF").ToList();
                    var catalogTps = customerTps.Where(tp => tp.TradingPartnerCode == "QRS" || tp.TradingPartnerCode == "INT").ToList();

                    preview.EdiTradingPartnerCount = ediTps.Count;
                    preview.EdiTradingPartners = string.Join(", ", ediTps.Select(tp =>
                        tradingPartnerNames.TryGetValue(tp.TradingPartnerCode, out var name) ? name : tp.TradingPartnerCode));
                    preview.EdiTradingPartnerFee = ediCustomer.EdiTradingPartnerFee;

                    preview.NonEdiTradingPartnerCount = nonEdiTps.Count;
                    preview.NonEdiTradingPartners = string.Join(", ", nonEdiTps.Select(tp =>
                        tradingPartnerNames.TryGetValue(tp.TradingPartnerCode, out var name) ? name : tp.TradingPartnerCode));
                    preview.NonEdiTradingPartnerFee = ediCustomer.NonEdiTradingPartnerFee;

                    preview.PdfCount = pdfTps.Count;
                    preview.PdfTradingPartners = string.Join(", ", pdfTps.Select(tp =>
                        tradingPartnerNames.TryGetValue(tp.TradingPartnerCode, out var name) ? name : tp.TradingPartnerCode));
                    preview.PdfFee = ediCustomer.PdfFee;

                    preview.CatalogTradingPartnerCount = catalogTps.Count;
                    preview.CatalogTradingPartners = string.Join(", ", catalogTps.Select(tp =>
                        tradingPartnerNames.TryGetValue(tp.TradingPartnerCode, out var name) ? name : tp.TradingPartnerCode));
                    preview.CatalogFee = ediCustomer.CatalogTradingPartnerFee;

                    if (kilocharData.TryGetValue(ediCustomer.SiteId, out var kc) && ediCustomer.KilocharBillingType == "Y")
                    {
                        preview.KilocharQuantity = kc;
                        preview.KilocharRate = ediCustomer.KilocharRate;
                        var kcAmount = kc * ediCustomer.KilocharRate;
                        if (kcAmount < ediCustomer.KilocharMinimumDollars)
                        {
                            preview.KilocharQuantity = 1;
                            preview.KilocharRate = ediCustomer.KilocharMinimumDollars;
                            kcAmount = ediCustomer.KilocharMinimumDollars;
                        }
                        preview.KilocharAmount = Math.Round(kcAmount, 2);
                    }

                    preview.MailboxFee = ediCustomer.MailboxFee;

                    preview.TotalAmount =
                        (preview.EdiTradingPartnerCount * preview.EdiTradingPartnerFee) +
                        (preview.NonEdiTradingPartnerCount * preview.NonEdiTradingPartnerFee) +
                        (preview.PdfCount > 0 ? preview.PdfFee : 0) +
                        (preview.CatalogTradingPartnerCount * preview.CatalogFee) +
                        preview.KilocharAmount +
                        preview.MailboxFee;
                }

                previews.Add(preview);
            }

            return ServiceResult<List<EdiCustomerBillingPreview>>.Ok(previews);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating EDI billing preview for {Year}/{Month}", year, month);
            return ServiceResult<List<EdiCustomerBillingPreview>>.Fail(ex.Message);
        }
    }

    #endregion

    #region Private Methods

    private static List<EdiTradingPartnerSummary> ApplyTradingPartnerMappings(List<EdiTradingPartnerSummary> results)
    {
        var mappings = new Dictionary<string, string>
        {
            { "MSD", "MSC" },
            { "GND", "GNS" },
            { "NP1", "NPG" },
            { "NP2", "NPI" }
        };

        foreach (var result in results)
        {
            if (mappings.TryGetValue(result.TradingPartnerCode, out var mapped))
            {
                result.TradingPartnerCode = mapped;
            }

            result.BillType = result.TradingPartnerCode != "_" ? "X12" : "PDF";
        }

        return results.DistinctBy(r => new { r.CustomerNumber, r.TradingPartnerCode }).ToList();
    }

    private async Task<Dictionary<string, string>> GetTradingPartnerNamesAsync()
    {
        var names = new Dictionary<string, string>();

        if (string.IsNullOrEmpty(_connectionString))
        {
            return names;
        }

        try
        {
            var sql = "SELECT tp_code, tp_name FROM tradpart";

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var code = reader.GetString(0).Trim();
                var name = reader.IsDBNull(1) ? code : reader.GetString(1).Trim();
                names[code] = name;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load trading partner names");
        }

        return names;
    }

    private async Task<Dictionary<string, long>> GetKilocharDataAsync(int year, int month)
    {
        var data = new Dictionary<string, long>();

        if (string.IsNullOrEmpty(_connectionString))
        {
            return data;
        }

        try
        {
            var tableName = $"DXC_{year}{month:D2}";
            var sql = $"SELECT siteid, total_kc FROM {tableName}";

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var siteId = reader.GetString(0).Trim();
                var totalKc = reader.IsDBNull(1) ? 0 : reader.GetInt64(1);
                data[siteId] = totalKc;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load kilochar data for {Year}/{Month}", year, month);
        }

        return data;
    }

    #endregion
}
