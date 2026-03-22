using System.Net.Http.Headers;

namespace BillingSys.Client.Services;

/// <summary>
/// Attaches the Google ID token from <see cref="GoogleAuthService"/> as Bearer for API calls.
/// </summary>
public class ApiBearerTokenHandler : DelegatingHandler
{
    #region Fields

    private readonly GoogleAuthService _googleAuth;

    #endregion

    #region Public Methods

    public ApiBearerTokenHandler(GoogleAuthService googleAuth)
    {
        _googleAuth = googleAuth;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _googleAuth.GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }

    #endregion
}
