using BillingSys.Shared.Models;
using Microsoft.Extensions.Caching.Memory;

namespace BillingSys.Functions.Repositories;

#region Cached Employee Repository

public class CachedEmployeeRepository : IEmployeeRepository
{
    private readonly IEmployeeRepository _inner;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    public CachedEmployeeRepository(IEmployeeRepository inner, IMemoryCache cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public Task<ServiceResult<Employee>> GetAsync(string id) => _inner.GetAsync(id);
    public Task<ServiceResult<Employee>> GetByEmailAsync(string email) => _inner.GetByEmailAsync(email);

    public async Task<ServiceResult<List<Employee>>> GetAllAsync(bool activeOnly = true)
    {
        var cacheKey = $"employees_{activeOnly}";
        if (_cache.TryGetValue(cacheKey, out ServiceResult<List<Employee>>? cached) && cached != null)
        {
            return cached;
        }

        var result = await _inner.GetAllAsync(activeOnly);
        if (result.Success)
        {
            _cache.Set(cacheKey, result, CacheDuration);
        }
        return result;
    }

    public async Task<ServiceResult<Employee>> UpsertAsync(Employee employee)
    {
        var result = await _inner.UpsertAsync(employee);
        if (result.Success) InvalidateCache();
        return result;
    }

    private void InvalidateCache()
    {
        _cache.Remove("employees_true");
        _cache.Remove("employees_false");
    }
}

#endregion

#region Cached Customer Repository

public class CachedCustomerRepository : ICustomerRepository
{
    private readonly ICustomerRepository _inner;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    public CachedCustomerRepository(ICustomerRepository inner, IMemoryCache cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public Task<ServiceResult<Customer>> GetAsync(string id) => _inner.GetAsync(id);

    public async Task<ServiceResult<List<Customer>>> GetAllAsync(bool activeOnly = true)
    {
        var cacheKey = $"customers_{activeOnly}";
        if (_cache.TryGetValue(cacheKey, out ServiceResult<List<Customer>>? cached) && cached != null)
        {
            return cached;
        }

        var result = await _inner.GetAllAsync(activeOnly);
        if (result.Success)
        {
            _cache.Set(cacheKey, result, CacheDuration);
        }
        return result;
    }

    public async Task<ServiceResult<Customer>> UpsertAsync(Customer customer)
    {
        var result = await _inner.UpsertAsync(customer);
        if (result.Success) InvalidateCache();
        return result;
    }

    private void InvalidateCache()
    {
        _cache.Remove("customers_true");
        _cache.Remove("customers_false");
    }
}

#endregion

#region Cached Project Repository

public class CachedProjectRepository : IProjectRepository
{
    private readonly IProjectRepository _inner;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public CachedProjectRepository(IProjectRepository inner, IMemoryCache cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public Task<ServiceResult<Project>> GetAsync(string customerId, string projectCode) => _inner.GetAsync(customerId, projectCode);
    public Task<ServiceResult<List<Project>>> GetByCustomerAsync(string customerId) => _inner.GetByCustomerAsync(customerId);

    public async Task<ServiceResult<List<Project>>> GetAllAsync(ProjectStatus? status = null)
    {
        var cacheKey = $"projects_{status}";
        if (_cache.TryGetValue(cacheKey, out ServiceResult<List<Project>>? cached) && cached != null)
        {
            return cached;
        }

        var result = await _inner.GetAllAsync(status);
        if (result.Success)
        {
            _cache.Set(cacheKey, result, CacheDuration);
        }
        return result;
    }

    public async Task<ServiceResult<Project>> UpsertAsync(Project project)
    {
        var result = await _inner.UpsertAsync(project);
        if (result.Success) InvalidateCache();
        return result;
    }

    private void InvalidateCache()
    {
        foreach (var status in Enum.GetValues<ProjectStatus>())
        {
            _cache.Remove($"projects_{status}");
        }
        _cache.Remove("projects_");
    }
}

#endregion

#region Cached Service Item Repository

public class CachedServiceItemRepository : IServiceItemRepository
{
    private readonly IServiceItemRepository _inner;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    public CachedServiceItemRepository(IServiceItemRepository inner, IMemoryCache cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public Task<ServiceResult<ServiceItem>> GetAsync(string itemCode) => _inner.GetAsync(itemCode);

    public async Task<ServiceResult<List<ServiceItem>>> GetAllAsync(bool activeOnly = true)
    {
        var cacheKey = $"serviceitems_{activeOnly}";
        if (_cache.TryGetValue(cacheKey, out ServiceResult<List<ServiceItem>>? cached) && cached != null)
        {
            return cached;
        }

        var result = await _inner.GetAllAsync(activeOnly);
        if (result.Success)
        {
            _cache.Set(cacheKey, result, CacheDuration);
        }
        return result;
    }

    public async Task<ServiceResult<ServiceItem>> UpsertAsync(ServiceItem item)
    {
        var result = await _inner.UpsertAsync(item);
        if (result.Success) InvalidateCache();
        return result;
    }

    private void InvalidateCache()
    {
        _cache.Remove("serviceitems_true");
        _cache.Remove("serviceitems_false");
    }
}

#endregion
