using BillingSys.Client;
using BillingSys.Client.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseAddress = builder.Configuration["ApiBaseAddress"] ?? builder.HostEnvironment.BaseAddress;

// Was: AddMsalAuthentication + Entra External ID — replaced with direct Google OIDC (id_token).
builder.Services.AddOidcAuthentication(options =>
{
    options.ProviderOptions.Authority = "https://accounts.google.com";
    options.ProviderOptions.ClientId = builder.Configuration["Google:ClientId"] ?? "";
    options.ProviderOptions.ResponseType = "id_token";
    options.ProviderOptions.DefaultScopes.Add("openid");
    options.ProviderOptions.DefaultScopes.Add("email");
    options.ProviderOptions.DefaultScopes.Add("profile");
});

builder.Services.AddScoped(sp =>
{
    var handler = new ApiBearerTokenHandler(sp.GetRequiredService<IAccessTokenProvider>())
    {
        InnerHandler = new HttpClientHandler()
    };
    return new HttpClient(handler) { BaseAddress = new Uri(apiBaseAddress) };
});

builder.Services.AddScoped<ApiService>();

await builder.Build().RunAsync();
