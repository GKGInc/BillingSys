using BillingSys.Client;
using BillingSys.Client.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseAddress = builder.Configuration["ApiBaseAddress"] ?? builder.HostEnvironment.BaseAddress;

builder.Services.AddMsalAuthentication(options =>
{
    builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
    options.ProviderOptions.DefaultAccessTokenScopes.Add("openid");
    options.ProviderOptions.DefaultAccessTokenScopes.Add("profile");
    options.ProviderOptions.DefaultAccessTokenScopes.Add("email");
    options.ProviderOptions.LoginMode = "redirect";
});

builder.Services.AddScoped(sp =>
{
    var authorizationMessageHandler = sp.GetRequiredService<AuthorizationMessageHandler>()
        .ConfigureHandler(
            authorizedUrls: new[] { apiBaseAddress },
            scopes: new[] { builder.Configuration["AzureAd:ApiScope"] ?? "api://billingsys/access" });
    authorizationMessageHandler.InnerHandler = new HttpClientHandler();
    return new HttpClient(authorizationMessageHandler) { BaseAddress = new Uri(apiBaseAddress) };
});

builder.Services.AddScoped<ApiService>();

await builder.Build().RunAsync();
