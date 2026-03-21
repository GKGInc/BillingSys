using BillingSys.Functions.Infrastructure;
using BillingSys.Functions.Repositories;
using BillingSys.Functions.Services;
using BillingSys.Shared.Interfaces;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Incoming JSON often omits "Z" on ISO datetimes, producing DateTimeKind.Unspecified — Azure SDK rejects that.
builder.Services.Configure<JsonOptions>(options =>
{
    var o = options.SerializerOptions;
    o.PropertyNameCaseInsensitive = true;
    o.Converters.Add(new UtcDateTimeConverter());
    o.Converters.Add(new UtcNullableDateTimeConverter());
});

var storageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage") 
    ?? "UseDevelopmentStorage=true";
var sqlConnectionString = Environment.GetEnvironmentVariable("SqlConnectionString") 
    ?? string.Empty;

#region Service Registration

// Redeploy trigger (2026-03-19): trivial change to force CI/CD. DI audit — all repository interfaces registered:
// IEmployeeRepository, ICustomerRepository, IProjectRepository, IServiceItemRepository → cached decorators over concrete repos;
// ITimeEntryRepository, IInvoiceRepository, ISystemConfigRepository → direct to concrete repos;
// IEdiDataRepository → SqlEdiDataRepository.

// Old: single TableStorageService handled all entities
// New: focused repository classes per entity with caching decorators for reference data

builder.Services.AddMemoryCache();

builder.Services.AddSingleton<TableStorageContext>(sp =>
    new TableStorageContext(storageConnectionString));

// Keep TableStorageService for backward compatibility (QuickBooksService still uses it)
builder.Services.AddSingleton<TableStorageService>(sp =>
    new TableStorageService(storageConnectionString, sp.GetRequiredService<ILogger<TableStorageService>>()));

// Repository implementations (concrete)
builder.Services.AddSingleton<EmployeeRepository>();
builder.Services.AddSingleton<CustomerRepository>();
builder.Services.AddSingleton<ProjectRepository>();
builder.Services.AddSingleton<ServiceItemRepository>();
builder.Services.AddSingleton<TimeEntryRepository>();
builder.Services.AddSingleton<InvoiceRepository>();
builder.Services.AddSingleton<SystemConfigRepository>();

// Repository interfaces with caching decorators for reference data
builder.Services.AddSingleton<IEmployeeRepository>(sp =>
    new CachedEmployeeRepository(
        sp.GetRequiredService<EmployeeRepository>(),
        sp.GetRequiredService<IMemoryCache>()));

builder.Services.AddSingleton<ICustomerRepository>(sp =>
    new CachedCustomerRepository(
        sp.GetRequiredService<CustomerRepository>(),
        sp.GetRequiredService<IMemoryCache>()));

builder.Services.AddSingleton<IProjectRepository>(sp =>
    new CachedProjectRepository(
        sp.GetRequiredService<ProjectRepository>(),
        sp.GetRequiredService<IMemoryCache>()));

builder.Services.AddSingleton<IServiceItemRepository>(sp =>
    new CachedServiceItemRepository(
        sp.GetRequiredService<ServiceItemRepository>(),
        sp.GetRequiredService<IMemoryCache>()));

// Transactional repositories (no caching)
builder.Services.AddSingleton<ITimeEntryRepository>(sp => sp.GetRequiredService<TimeEntryRepository>());
builder.Services.AddSingleton<IInvoiceRepository>(sp => sp.GetRequiredService<InvoiceRepository>());
builder.Services.AddSingleton<ISystemConfigRepository>(sp => sp.GetRequiredService<SystemConfigRepository>());

// EDI data repository (SQL-backed)
builder.Services.AddSingleton<IEdiDataRepository>(sp =>
    new SqlEdiDataRepository(sqlConnectionString, sp.GetRequiredService<ILogger<SqlEdiDataRepository>>()));

// System settings provider
builder.Services.AddSingleton<ISystemSettingsProvider, SystemSettingsProvider>();

// Business services
builder.Services.AddSingleton<BillingService>();

// Old: EdiService kept for backward compatibility but SqlEdiDataRepository is the preferred access path
builder.Services.AddSingleton<EdiService>(sp =>
    new EdiService(sqlConnectionString, sp.GetRequiredService<ILogger<EdiService>>()));

// Authentication & Authorization
builder.Services.AddSingleton<AuthenticationService>(sp =>
    new AuthenticationService(sp.GetRequiredService<ILogger<AuthenticationService>>()));

builder.Services.AddSingleton<AuthorizationService>();

#endregion

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
