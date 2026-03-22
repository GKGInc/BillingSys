# Production redirect URIs (Google OAuth 2.0 — direct)

The Blazor SPA uses **Google** as the OpenID Connect authority (`https://accounts.google.com`) with **`id_token`** response type. Configure the **same OAuth 2.0 Web client** in Google Cloud Console that supplies **`Google:ClientId`** in `wwwroot/appsettings.json` and **`Google__ClientId`** on the Function App.

Replace placeholders:

- `<ORIGIN>` = site origin only (scheme + host, no path), e.g. `https://your-app.azurestaticapps.net` or `https://localhost:5001`
- `<SWA>` = your Azure Static Web Apps hostname, e.g. `https://your-app.azurestaticapps.net`

## Google Cloud Console — OAuth 2.0 Client IDs (Web application)

### Authorized JavaScript origins

Add every origin where the SPA is loaded:

| Origin | Purpose |
|--------|---------|
| `http://localhost:5001` | Local Blazor WASM (adjust port if yours differs) |
| `https://localhost:5001` | Local HTTPS if used |
| `<SWA>` (no path) | Production Static Web Apps |

### Authorized redirect URIs

Blazor’s OIDC login callback (fragment response) uses:

| URI | Purpose |
|-----|---------|
| `http://localhost:5001/authentication/login-callback` | Local dev |
| `<SWA>/authentication/login-callback` | Production (no trailing slash on `<SWA>` before the path) |

Example production redirect:

`https://your-app.azurestaticapps.net/authentication/login-callback`

**Logout:** If you configure post-logout redirects in Google for your client, you may add `<SWA>/authentication/logout-callback` to match [Authentication.razor](../src/BillingSys.Client/Pages/Authentication.razor) routes.

## Function App (`Google__ClientId`)

Use the **same** Web client **Client ID** string as in Blazor `Google:ClientId`. The API validates Google **ID tokens** with audience = that client ID.

## Smoke test

1. Open `<SWA>/` in a private window.
2. **Sign In** → choose Google → an allowed `@tech85.com` account (see `AllowedEmailDomain` on the Function App).
3. Open **Time** (or any authorized page) and confirm API calls return **200** (Network tab), not **401**.
