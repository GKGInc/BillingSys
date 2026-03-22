using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using HttpRequestData = Microsoft.Azure.Functions.Worker.Http.HttpRequestData;

namespace BillingSys.Functions.Services;

public class AuthenticationService
{
    private readonly ILogger<AuthenticationService> _logger;
    private readonly string _tenantName;
    private readonly string _clientId;
    private readonly string _allowedDomain;
    private TokenValidationParameters? _validationParameters;
    private OpenIdConnectConfiguration? _configuration;

    public AuthenticationService(ILogger<AuthenticationService> logger)
    {
        _logger = logger;
        _tenantName = Environment.GetEnvironmentVariable("AzureAd__TenantName") ?? "tech85";
        _clientId = Environment.GetEnvironmentVariable("AzureAd__ClientId") ?? "";
        _allowedDomain = Environment.GetEnvironmentVariable("AllowedEmailDomain") ?? "tech85.com";
    }

    public async Task<ClaimsPrincipal?> ValidateTokenAsync(HttpRequestData request)
    {
        try
        {
            var authHeader = request.Headers.TryGetValues("Authorization", out var authValues) 
                ? authValues.FirstOrDefault() 
                : null;

            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                _logger.LogWarning("Missing or invalid Authorization header");
                return null;
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();
            return await ValidateTokenAsync(token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token from request");
            return null;
        }
    }

    public async Task<ClaimsPrincipal?> ValidateTokenAsync(string token)
    {
        try
        {
            if (_validationParameters == null)
            {
                await InitializeValidationParametersAsync();
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, _validationParameters, out var validatedToken);

            if (!ValidateEmailDomain(principal))
            {
                _logger.LogWarning("User email domain is not allowed");
                return null;
            }

            return principal;
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex,
                "Token validation failed (expected audiences: {ClientId}, api://{ClientId})",
                _clientId ?? "(unset)",
                _clientId ?? "");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return null;
        }
    }

    private bool ValidateEmailDomain(ClaimsPrincipal principal)
    {
        if (string.IsNullOrEmpty(_allowedDomain))
        {
            return true;
        }

        var email = principal.FindFirst(ClaimTypes.Email)?.Value 
            ?? principal.FindFirst("emails")?.Value
            ?? principal.FindFirst("email")?.Value;

        if (string.IsNullOrEmpty(email))
        {
            _logger.LogWarning("No email claim found in token");
            return false;
        }

        var domain = email.Split('@').LastOrDefault()?.ToLowerInvariant();
        var allowed = _allowedDomain.ToLowerInvariant();

        if (domain != allowed)
        {
            _logger.LogWarning("Email domain {Domain} is not allowed (expected {AllowedDomain})", domain, allowed);
            return false;
        }

        return true;
    }

    private async Task InitializeValidationParametersAsync()
    {
        var metadataEndpoint = $"https://{_tenantName}.ciamlogin.com/{_tenantName}.onmicrosoft.com/v2.0/.well-known/openid-configuration";
        
        var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            metadataEndpoint,
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever());

        _configuration = await configManager.GetConfigurationAsync();

        // Access tokens for api://{clientId}/access use aud = api://{clientId}; some tokens use the bare client GUID.
        string[]? audiences = null;
        if (!string.IsNullOrEmpty(_clientId))
        {
            audiences = new[] { _clientId, $"api://{_clientId}" };
        }
        else
        {
            _logger.LogWarning("AzureAd__ClientId is not set; set it in Function App Configuration to validate API JWTs.");
        }

        _validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _configuration.Issuer,
            ValidateAudience = audiences != null && audiences.Length > 0,
            ValidAudiences = audiences,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = _configuration.SigningKeys,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5)
        };
    }

    public static string? GetUserEmail(ClaimsPrincipal? principal)
    {
        if (principal == null) return null;
        
        return principal.FindFirst(ClaimTypes.Email)?.Value 
            ?? principal.FindFirst("emails")?.Value
            ?? principal.FindFirst("email")?.Value;
    }

    public static string? GetUserId(ClaimsPrincipal? principal)
    {
        if (principal == null) return null;
        
        return principal.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? principal.FindFirst("sub")?.Value
            ?? principal.FindFirst("oid")?.Value;
    }

    public static string? GetUserName(ClaimsPrincipal? principal)
    {
        if (principal == null) return null;
        
        return principal.FindFirst(ClaimTypes.Name)?.Value 
            ?? principal.FindFirst("name")?.Value;
    }
}
