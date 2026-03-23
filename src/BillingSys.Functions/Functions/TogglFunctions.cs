using System.Net;
using System.Text.Json;
using BillingSys.Functions.Infrastructure;
using BillingSys.Functions.Services;
using BillingSys.Shared.DTOs;
using BillingSys.Shared.Enums;
using BillingSys.Shared.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace BillingSys.Functions.Functions;

public class TogglFunctions
{
    private readonly TogglService _togglService;
    private readonly AuthorizationService _authService;
    private readonly ILogger<TogglFunctions> _logger;
    private static readonly JsonSerializerOptions JsonOptions = FunctionsJsonSerializerOptions.Default;

    public TogglFunctions(
        TogglService togglService,
        AuthorizationService authService,
        ILogger<TogglFunctions> logger)
    {
        _togglService = togglService;
        _authService = authService;
        _logger = logger;
    }

    #region Pull

    [Function("TogglPull")]
    public async Task<HttpResponseData> Pull(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "toggl/pull")] HttpRequestData req)
    {
        var authResult = await _authService.AuthorizeAsync(req, UserRole.Manager, UserRole.Admin);
        if (!authResult.IsAuthorized) return await authResult.ToResponseAsync(req);

        try
        {
            var body = await req.ReadAsStringAsync();
            var request = JsonSerializer.Deserialize<TogglPullRequest>(body!, JsonOptions);
            if (request == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(ServiceResult<TogglPullResult>.Fail("Invalid request body"));
                return badResponse;
            }

            var result = await _togglService.PullFromTogglAsync(request.StartDate, request.EndDate);

            var response = req.CreateResponse(result.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest);
            await response.WriteAsJsonAsync(result);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in TogglPull");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(ServiceResult<TogglPullResult>.Fail(ex.Message));
            return errorResponse;
        }
    }

    #endregion

    #region Load Batch

    [Function("TogglLoadBatch")]
    public async Task<HttpResponseData> LoadBatch(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "toggl/batch/{batchId}")] HttpRequestData req,
        string batchId)
    {
        var authResult = await _authService.AuthorizeAsync(req, UserRole.Manager, UserRole.Admin);
        if (!authResult.IsAuthorized) return await authResult.ToResponseAsync(req);

        var result = await _togglService.LoadBatchAsync(batchId);

        var response = req.CreateResponse(result.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest);
        await response.WriteAsJsonAsync(result);
        return response;
    }

    #endregion

    #region Summarize

    [Function("TogglSummarize")]
    public async Task<HttpResponseData> Summarize(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "toggl/summarize")] HttpRequestData req)
    {
        var authResult = await _authService.AuthorizeAsync(req, UserRole.Manager, UserRole.Admin);
        if (!authResult.IsAuthorized) return await authResult.ToResponseAsync(req);

        try
        {
            var body = await req.ReadAsStringAsync();
            var request = JsonSerializer.Deserialize<TogglSummarizeRequest>(body!, JsonOptions);
            if (request == null || string.IsNullOrEmpty(request.BatchId))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(ServiceResult<TogglSummaryResult>.Fail("Invalid request body"));
                return badResponse;
            }

            var result = await _togglService.SummarizeAsync(request.BatchId);

            var response = req.CreateResponse(result.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest);
            await response.WriteAsJsonAsync(result);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in TogglSummarize");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(ServiceResult<TogglSummaryResult>.Fail(ex.Message));
            return errorResponse;
        }
    }

    #endregion

    #region Edit Summary

    [Function("TogglEditSummary")]
    public async Task<HttpResponseData> EditSummary(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "toggl/summary/edit")] HttpRequestData req)
    {
        var authResult = await _authService.AuthorizeAsync(req, UserRole.Manager, UserRole.Admin);
        if (!authResult.IsAuthorized) return await authResult.ToResponseAsync(req);

        try
        {
            var body = await req.ReadAsStringAsync();
            var request = JsonSerializer.Deserialize<TogglEditRequest>(body!, JsonOptions);
            if (request == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(ServiceResult.Fail("Invalid request body"));
                return badResponse;
            }

            var result = await _togglService.EditSummaryAsync(request.BatchId, request.EntryId, request.NewSummary);

            var response = req.CreateResponse(result.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest);
            await response.WriteAsJsonAsync(result);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in TogglEditSummary");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(ServiceResult.Fail(ex.Message));
            return errorResponse;
        }
    }

    #endregion

    #region Invoice Preview

    [Function("TogglInvoicePreview")]
    public async Task<HttpResponseData> InvoicePreview(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "toggl/invoice/preview")] HttpRequestData req)
    {
        var authResult = await _authService.AuthorizeAsync(req, UserRole.Manager, UserRole.Admin);
        if (!authResult.IsAuthorized) return await authResult.ToResponseAsync(req);

        try
        {
            var body = await req.ReadAsStringAsync();
            var request = JsonSerializer.Deserialize<TogglInvoicePreviewRequest>(body!, JsonOptions);
            if (request == null || string.IsNullOrEmpty(request.BatchId))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(ServiceResult<TogglInvoicePreview>.Fail("Invalid request"));
                return badResponse;
            }

            var result = await _togglService.GenerateInvoicePreviewAsync(request.BatchId, request.InvoiceDate);
            var response = req.CreateResponse(result.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest);
            await response.WriteAsJsonAsync(result);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in TogglInvoicePreview");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(ServiceResult<TogglInvoicePreview>.Fail(ex.Message));
            return errorResponse;
        }
    }

    #endregion

    #region Post Invoices

    [Function("TogglPostInvoices")]
    public async Task<HttpResponseData> PostInvoices(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "toggl/invoice/post")] HttpRequestData req)
    {
        var authResult = await _authService.AuthorizeAsync(req, UserRole.Manager, UserRole.Admin);
        if (!authResult.IsAuthorized) return await authResult.ToResponseAsync(req);

        try
        {
            var body = await req.ReadAsStringAsync();
            var request = JsonSerializer.Deserialize<TogglPostInvoicesRequest>(body!, JsonOptions);
            if (request == null || string.IsNullOrEmpty(request.BatchId))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(ServiceResult<TogglPostInvoicesResult>.Fail("Invalid request"));
                return badResponse;
            }

            var result = await _togglService.PostInvoicesAsync(
                request.BatchId, request.InvoiceDate, request.SelectedCustomerIds);
            var response = req.CreateResponse(result.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest);
            await response.WriteAsJsonAsync(result);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in TogglPostInvoices");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(ServiceResult<TogglPostInvoicesResult>.Fail(ex.Message));
            return errorResponse;
        }
    }

    #endregion
}

public class TogglEditRequest
{
    public string BatchId { get; set; } = string.Empty;
    public string EntryId { get; set; } = string.Empty;
    public string NewSummary { get; set; } = string.Empty;
}
