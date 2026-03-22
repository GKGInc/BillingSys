using BillingSys.Client;
using BillingSys.Client.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseAddress = builder.Configuration["ApiBaseAddress"] ?? builder.HostEnvironment.BaseAddress;

// Was: AddOidcAuthentication — replaced with Google Identity Services (JS) + GoogleAuthService.
builder.Services.AddAuthorizationCore();

builder.Services.AddScoped<GoogleAuthService>();
builder.Services.AddScoped<GoogleAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<GoogleAuthenticationStateProvider>());
builder.Services.AddScoped<ApiBearerTokenHandler>();

builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<ApiBearerTokenHandler>();
    handler.InnerHandler = new HttpClientHandler();
    return new HttpClient(handler) { BaseAddress = new Uri(apiBaseAddress) };
});

builder.Services.AddScoped<ApiService>();

await builder.Build().RunAsync();
