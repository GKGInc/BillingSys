using System.Net;
using System.Text.Json;
using BillingSys.Functions.Infrastructure;
using BillingSys.Functions.Repositories;
using BillingSys.Functions.Services;
using BillingSys.Functions.Validators;
using BillingSys.Shared.DTOs;
using BillingSys.Shared.Enums;
using BillingSys.Shared.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace BillingSys.Functions.Functions;

public class ProjectFunctions
{
    private readonly IProjectRepository _projects;
    private readonly ICustomerRepository _customers;
    private readonly IEmployeeRepository _employees;
    private readonly AuthorizationService _authService;
    private readonly ILogger<ProjectFunctions> _logger;
    private static readonly JsonSerializerOptions JsonOptions = FunctionsJsonSerializerOptions.Default;

    public ProjectFunctions(
        IProjectRepository projects,
        ICustomerRepository customers,
        IEmployeeRepository employees,
        AuthorizationService authService,
        ILogger<ProjectFunctions> logger)
    {
        _projects = projects;
        _customers = customers;
        _employees = employees;
        _authService = authService;
        _logger = logger;
    }

    [Function("GetProjects")]
    public async Task<HttpResponseData> GetProjects(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects")] HttpRequestData req)
    {
        var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        var statusStr = query["status"];
        ProjectStatus? status = null;
        if (!string.IsNullOrEmpty(statusStr) && Enum.TryParse<ProjectStatus>(statusStr, true, out var s))
        {
            status = s;
        }

        var result = await _projects.GetAllAsync(status);

        var response = req.CreateResponse(result.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest);
        await response.WriteAsJsonAsync(result);
        return response;
    }

    [Function("GetProjectsByCustomer")]
    public async Task<HttpResponseData> GetProjectsByCustomer(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/by-customer/{customerId}")] HttpRequestData req,
        string customerId)
    {
        var result = await _projects.GetByCustomerAsync(customerId);

        var response = req.CreateResponse(result.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest);
        await response.WriteAsJsonAsync(result);
        return response;
    }

    [Function("GetProject")]
    public async Task<HttpResponseData> GetProject(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{customerId}/{projectCode}")] HttpRequestData req,
        string customerId, string projectCode)
    {
        var result = await _projects.GetAsync(customerId, projectCode);

        var response = req.CreateResponse(result.Success ? HttpStatusCode.OK : HttpStatusCode.NotFound);
        await response.WriteAsJsonAsync(result);
        return response;
    }

    [Function("CreateProject")]
    public async Task<HttpResponseData> CreateProject(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects")] HttpRequestData req)
    {
        // Any authenticated user with an employee record may create projects — supports initial seeding without Manager/Admin.
        var authResult = await _authService.AuthorizeAsync(req);
        if (!authResult.IsAuthorized) return await authResult.ToResponseAsync(req);

        try
        {
            var body = await req.ReadAsStringAsync();
            var request = JsonSerializer.Deserialize<CreateProjectRequest>(body!, JsonOptions);
            if (request == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(ServiceResult<Project>.Fail("Invalid request body"));
                return badResponse;
            }

            var validator = new CreateProjectValidator();
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(ServiceResult<Project>.Fail(
                    string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage))));
                return badResponse;
            }

            var project = new Project
            {
                ProjectCode = request.ProjectCode,
                CustomerId = request.CustomerId,
                Description = request.Description,
                ServiceItemCode = request.ServiceItemCode,
                CustomerPO = request.CustomerPO,
                ProgrammerId = request.ProgrammerId,
                Price = request.Price,
                QuotedHours = request.QuotedHours,
                PreBill = request.PreBill,
                AddDetailToInvoice = request.AddDetailToInvoice,
                Status = ProjectStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _projects.UpsertAsync(project);

            var response = req.CreateResponse(result.Success ? HttpStatusCode.Created : HttpStatusCode.BadRequest);
            await response.WriteAsJsonAsync(result);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating project");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(ServiceResult<Project>.Fail(ex.Message));
            return errorResponse;
        }
    }

    [Function("UpdateProject")]
    public async Task<HttpResponseData> UpdateProject(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "projects/{customerId}/{projectCode}")] HttpRequestData req,
        string customerId, string projectCode)
    {
        var authResult = await _authService.AuthorizeAsync(req, UserRole.Manager, UserRole.Admin);
        if (!authResult.IsAuthorized) return await authResult.ToResponseAsync(req);

        try
        {
            var existingResult = await _projects.GetAsync(customerId, projectCode);
            if (!existingResult.Success)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteAsJsonAsync(existingResult);
                return notFoundResponse;
            }

            var body = await req.ReadAsStringAsync();
            var request = JsonSerializer.Deserialize<UpdateProjectRequest>(body!, JsonOptions);
            if (request == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(ServiceResult<Project>.Fail("Invalid request body"));
                return badResponse;
            }

            var validator = new UpdateProjectValidator();
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(ServiceResult<Project>.Fail(
                    string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage))));
                return badResponse;
            }

            var project = existingResult.Data!;
            project.Description = request.Description;
            project.ServiceItemCode = request.ServiceItemCode;
            project.CustomerPO = request.CustomerPO;
            project.ProgrammerId = request.ProgrammerId;
            project.Price = request.Price;
            project.QuotedHours = request.QuotedHours;
            project.AdditionalHours = request.AdditionalHours;
            project.PreBill = request.PreBill;
            project.AddDetailToInvoice = request.AddDetailToInvoice;
            if (Enum.TryParse<ProjectStatus>(request.Status, true, out var status))
            {
                project.Status = status;
            }
            project.UpdatedAt = DateTime.UtcNow;

            var result = await _projects.UpsertAsync(project);

            var response = req.CreateResponse(result.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest);
            await response.WriteAsJsonAsync(result);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating project {ProjectCode}", projectCode);
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(ServiceResult<Project>.Fail(ex.Message));
            return errorResponse;
        }
    }

    [Function("GetProjectSummaries")]
    public async Task<HttpResponseData> GetProjectSummaries(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/summaries")] HttpRequestData req)
    {
        try
        {
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var statusStr = query["status"];
            ProjectStatus? status = null;
            if (!string.IsNullOrEmpty(statusStr) && Enum.TryParse<ProjectStatus>(statusStr, true, out var s))
            {
                status = s;
            }

            var projectsResult = await _projects.GetAllAsync(status);
            if (!projectsResult.Success)
            {
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteAsJsonAsync(ServiceResult<List<ProjectSummary>>.Fail(projectsResult.ErrorMessage ?? "Failed"));
                return errorResponse;
            }

            var customersResult = await _customers.GetAllAsync(false);
            var employeesResult = await _employees.GetAllAsync(false);

            var customers = customersResult.Success ? customersResult.Data!.ToDictionary(c => c.CustomerId) : new Dictionary<string, Customer>();
            var employees = employeesResult.Success ? employeesResult.Data!.ToDictionary(e => e.Id) : new Dictionary<string, Employee>();

            var summaries = projectsResult.Data!.Select(p => new ProjectSummary
            {
                ProjectCode = p.ProjectCode,
                CustomerId = p.CustomerId,
                CustomerName = customers.TryGetValue(p.CustomerId, out var cust) ? cust.Company : null,
                Description = p.Description,
                ProgrammerId = p.ProgrammerId,
                ProgrammerName = !string.IsNullOrEmpty(p.ProgrammerId) && employees.TryGetValue(p.ProgrammerId, out var emp) ? emp.Name : null,
                Price = p.Price,
                QuotedHours = p.QuotedHours,
                AdditionalHours = p.AdditionalHours,
                BilledHours = p.BilledHours,
                RemainingHours = p.RemainingHours,
                Status = p.Status.ToString()
            }).ToList();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(ServiceResult<List<ProjectSummary>>.Ok(summaries));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting project summaries");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(ServiceResult<List<ProjectSummary>>.Fail(ex.Message));
            return errorResponse;
        }
    }
}
