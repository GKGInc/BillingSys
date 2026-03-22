using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace BillingSys.Client.Services;

/// <summary>
/// Bridges Google Identity Services (JS) to Blazor: in-memory ID token, sign-in prompt, JWT helpers.
/// </summary>
public class GoogleAuthService : IDisposable
{
    #region Fields

    private readonly IJSRuntime _js;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleAuthService> _logger;
    private readonly JwtSecurityTokenHandler _jwtHandler = new();
    private DotNetObjectReference<GoogleAuthService>? _dotNetRef;
    private TaskCompletionSource<bool>? _signInCompletion;
    private bool _disposed;
    private bool _initialized;

    #endregion

    #region Public Methods

    public GoogleAuthService(IJSRuntime js, IConfiguration configuration, ILogger<GoogleAuthService> logger)
    {
        _js = js;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>Raised when the GIS token changes or expires (notify AuthenticationStateProvider).</summary>
    public event Action? AuthenticationStateChanged;

    /// <summary>Notifies listeners (e.g. <see cref="GoogleAuthenticationStateProvider"/>) to re-evaluate auth state without showing the GIS prompt.</summary>
    public void NotifyAuthenticationStateChanged()
    {
        AuthenticationStateChanged?.Invoke();
    }

    public async Task InitializeAsync()
    {
        if (_initialized) return;

        var clientId = _configuration["Google:ClientId"];
        if (string.IsNullOrWhiteSpace(clientId))
        {
            _logger.LogWarning("Google:ClientId is not configured.");
            return;
        }

        _dotNetRef = DotNetObjectReference.Create(this);
        await _js.InvokeVoidAsync("billingSysAuth.initializeGoogleAuth", clientId, _dotNetRef);
        _initialized = true;
    }

    public async Task<string?> GetTokenAsync()
    {
        return await _js.InvokeAsync<string?>("billingSysAuth.getStoredToken");
    }

    public async Task SignInAsync()
    {
        _signInCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        await _js.InvokeVoidAsync("billingSysAuth.promptSignIn");

        await Task.WhenAny(
            _signInCompletion.Task,
            Task.Delay(TimeSpan.FromMinutes(5)));
    }

    public async Task SignOutAsync()
    {
        await _js.InvokeVoidAsync("billingSysAuth.clearToken");
        _signInCompletion?.TrySetResult(false);
        AuthenticationStateChanged?.Invoke();
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var token = await GetTokenAsync();
        if (string.IsNullOrEmpty(token)) return false;
        return !IsTokenExpired(token);
    }

    public async Task<ClaimsPrincipal> GetUserPrincipalAsync()
    {
        var token = await GetTokenAsync();
        if (string.IsNullOrEmpty(token) || IsTokenExpired(token))
            return new ClaimsPrincipal(new ClaimsIdentity());

        return CreatePrincipalFromToken(token);
    }

    [JSInvokable]
    public void NotifyTokenChanged()
    {
        _signInCompletion?.TrySetResult(true);
        AuthenticationStateChanged?.Invoke();
    }

    [JSInvokable]
    public void NotifyTokenExpired()
    {
        _signInCompletion?.TrySetResult(false);
        AuthenticationStateChanged?.Invoke();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _dotNetRef?.Dispose();
        _dotNetRef = null;
        GC.SuppressFinalize(this);
    }

    #endregion

    #region Private Methods

    private bool IsTokenExpired(string jwt)
    {
        try
        {
            var t = _jwtHandler.ReadJwtToken(jwt);
            return t.ValidTo <= DateTime.UtcNow.AddMinutes(-1);
        }
        catch
        {
            return true;
        }
    }

    private ClaimsPrincipal CreatePrincipalFromToken(string jwt)
    {
        var token = _jwtHandler.ReadJwtToken(jwt);
        var claims = new List<Claim>();

        foreach (var c in token.Claims)
        {
            claims.Add(c);
        }

        var identity = new ClaimsIdentity(claims, authenticationType: "google", nameType: "name", roleType: ClaimTypes.Role);
        return new ClaimsPrincipal(identity);
    }

    #endregion
}
