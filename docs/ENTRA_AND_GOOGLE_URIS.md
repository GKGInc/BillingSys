# Production redirect URIs (Entra External ID + Google)

Use this checklist after GitHub Pages is live. Replace placeholders:

- `<PAGES>` = GitHub Pages site root, e.g. `https://tech85.github.io/BillingSys` (no trailing slash for some portals)
- `<REPO_PATH>` = path segment only if using project pages, e.g. `/BillingSys`

## Microsoft Entra External ID (CIAM) — App registration (SPA)

**Authentication → Platform configurations → Single-page application → Redirect URIs**, add:

| URI | Purpose |
|-----|--------|
| `https://localhost:5001/authentication/login-callback` | Local dev |
| `<PAGES>/authentication/login-callback` | Production GitHub Pages |

Example production URI:

`https://your-org.github.io/your-repo/authentication/login-callback`

**Logout URL** (if your app uses front-channel logout), add:

`<PAGES>/authentication/logout-callback`

(Only if you configure logout redirect in Entra; otherwise optional.)

## Google Cloud Console — OAuth 2.0 Web client

**Authorized JavaScript origins** (if required by your Google setup):

- `https://your-org.github.io` (project pages: origin is user/org domain, not repo path)

**Authorized redirect URIs** — keep the Entra federation endpoints your admin center documented when Google was added as an IdP. Typical patterns (tenant `tech85`):

- `https://tech85.ciamlogin.com/tech85.onmicrosoft.com/federation/oauth2`
- `https://tech85.ciamlogin.com/tech85/federation/oauth2`
- `https://tech85.ciamlogin.com/tech85.onmicrosoft.com/federation/oidc/accounts.google.com`
- `https://login.microsoftonline.com/te/tech85.onmicrosoft.com/oauth2/authresp`

Do **not** remove these when adding GitHub Pages; the SPA redirect is in **Entra**, not Google’s redirect list for user login (unless your Google project is set up differently).

## API scope for Functions

Ensure the SPA app registration is granted delegated permission to call your protected API scope (e.g. `api://<api-app-id>/access`) and that `wwwroot/appsettings.json` **`AzureAd:ApiScope`** matches the exposed scope in Entra.

## Smoke test

1. Open `<PAGES>/` in a private window.
2. **Sign In** → Google → `@tech85.com` account.
3. Open **Time** (or any authorized page) and confirm API calls succeed (browser **Network** tab → API returns 200, not 401).
