# Static hosting and authentication

## `/authentication/login-callback` and SPA routing

Blazor WebAssembly with **Google OIDC** uses client-side routes such as `/authentication/login-callback`. On **Azure Static Web Apps**, **`staticwebapp.config.json`** uses **`navigationFallback`** so unknown paths return **`index.html`** and the Blazor app can complete the login flow.

If you still see a **404** on the first request to `/authentication/login-callback`, confirm **`navigationFallback.rewrite`** is set to **`/index.html`** and that the path is not listed under **`exclude`**.

If sign-in fails, check:

- **Authorized redirect URIs** in **Google Cloud Console** match your app origin (e.g. `https://<app>.azurestaticapps.net/authentication/login-callback` and localhost for dev). See **[ENTRA_AND_GOOGLE_URIS.md](./ENTRA_AND_GOOGLE_URIS.md)**.
- **`wwwroot/appsettings.json`** has **`ApiBaseAddress`** and **`Google:ClientId`** set to your **Web application** OAuth client ID.

## API returns 401 after sign-in

The API validates **Google ID tokens** (`iss` = `https://accounts.google.com`, `aud` = your **Google OAuth client ID**). Set **`Google__ClientId`** on the Function App to the **same** value as **`Google:ClientId`** in the Blazor app.
