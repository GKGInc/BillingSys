using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace BillingSys.Client.Services;

/// <summary>
/// Supplies <see cref="AuthenticationState"/> from the Google GIS ID token (via <see cref="GoogleAuthService"/>).
/// </summary>
public class GoogleAuthenticationStateProvider : AuthenticationStateProvider, IDisposable
{
    #region Fields

    private readonly GoogleAuthService _googleAuth;
    private bool _disposed;

    #endregion

    #region Public Methods

    public GoogleAuthenticationStateProvider(GoogleAuthService googleAuth)
    {
        _googleAuth = googleAuth;
        _googleAuth.AuthenticationStateChanged += OnAuthenticationStateChanged;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var user = await _googleAuth.GetUserPrincipalAsync();
        return new AuthenticationState(user);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _googleAuth.AuthenticationStateChanged -= OnAuthenticationStateChanged;
        GC.SuppressFinalize(this);
    }

    #endregion

    #region Private Methods

    private void OnAuthenticationStateChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    #endregion
}
