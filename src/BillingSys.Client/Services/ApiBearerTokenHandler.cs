using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace BillingSys.Client.Services;

/// <summary>
/// Attaches the OIDC token (Google ID token in implicit/id_token flow) as Bearer for API calls.
/// </summary>
public class ApiBearerTokenHandler : DelegatingHandler
{
    #region Fields

    private readonly IAccessTokenProvider _accessTokenProvider;

    #endregion

    #region Public Methods

    public ApiBearerTokenHandler(IAccessTokenProvider accessTokenProvider)
    {
        _accessTokenProvider = accessTokenProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var result = await _accessTokenProvider.RequestAccessToken();
        if (result.TryGetToken(out var token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);
        }

        return await base.SendAsync(request, cancellationToken);
    }

    #endregion
}
