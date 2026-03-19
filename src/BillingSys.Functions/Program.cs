using BillingSys.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

var storageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage") 
    ?? "UseDevelopmentStorage=true";
var sqlConnectionString = Environment.GetEnvironmentVariable("SqlConnectionString") 
    ?? string.Empty;

builder.Services.AddSingleton<TableStorageService>(sp => 
    new TableStorageService(storageConnectionString, sp.GetRequiredService<ILogger<TableStorageService>>()));

builder.Services.AddSingleton<BillingService>(sp =>
    new BillingService(
        sp.GetRequiredService<TableStorageService>(), 
        sp.GetRequiredService<ILogger<BillingService>>()));

builder.Services.AddSingleton<EdiService>(sp =>
    new EdiService(sqlConnectionString, sp.GetRequiredService<ILogger<EdiService>>()));

builder.Services.AddSingleton<AuthenticationService>(sp =>
    new AuthenticationService(sp.GetRequiredService<ILogger<AuthenticationService>>()));

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
