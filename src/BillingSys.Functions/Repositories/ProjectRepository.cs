using Azure;
using BillingSys.Functions.Services;
using BillingSys.Shared.Models;
using Microsoft.Extensions.Logging;

namespace BillingSys.Functions.Repositories;

public class ProjectRepository : IProjectRepository
{
    private readonly TableStorageContext _context;
    private readonly ILogger<ProjectRepository> _logger;

    public ProjectRepository(TableStorageContext context, ILogger<ProjectRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Public Methods

    public async Task<ServiceResult<Project>> GetAsync(string customerId, string projectCode)
    {
        try
        {
            var table = _context.GetTable(TableStorageContext.ProjectsTable);
            var response = await table.GetEntityAsync<ProjectEntity>(customerId, projectCode);
            return ServiceResult<Project>.Ok(response.Value.ToModel());
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return ServiceResult<Project>.Fail($"Project {projectCode} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting project {ProjectCode}", projectCode);
            return ServiceResult<Project>.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult<List<Project>>> GetByCustomerAsync(string customerId)
    {
        try
        {
            var table = _context.GetTable(TableStorageContext.ProjectsTable);
            var filter = $"PartitionKey eq '{customerId}'";
            var projects = new List<Project>();
            await foreach (var entity in table.QueryAsync<ProjectEntity>(filter))
            {
                projects.Add(entity.ToModel());
            }
            return ServiceResult<List<Project>>.Ok(projects);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting projects for customer {CustomerId}", customerId);
            return ServiceResult<List<Project>>.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult<List<Project>>> GetAllAsync(ProjectStatus? status = null)
    {
        try
        {
            var table = _context.GetTable(TableStorageContext.ProjectsTable);
            var projects = new List<Project>();
            await foreach (var entity in table.QueryAsync<ProjectEntity>())
            {
                var project = entity.ToModel();
                if (status == null || project.Status == status)
                {
                    projects.Add(project);
                }
            }
            return ServiceResult<List<Project>>.Ok(projects);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all projects");
            return ServiceResult<List<Project>>.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult<Project>> UpsertAsync(Project project)
    {
        try
        {
            var table = _context.GetTable(TableStorageContext.ProjectsTable);
            var entity = ProjectEntity.FromModel(project);
            await table.UpsertEntityAsync(entity);
            return ServiceResult<Project>.Ok(project);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting project {ProjectCode}", project.ProjectCode);
            return ServiceResult<Project>.Fail(ex.Message);
        }
    }

    #endregion
}
