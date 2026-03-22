# GitHub Pages and authentication

## `/authentication/login-callback` shows 404 in the network tab

On a static host like **GitHub Pages**, only real files exist at fixed URLs. Blazor WebAssembly with MSAL uses client-side routes such as `/authentication/login-callback`. After Entra redirects back to your app, the browser requests that path from the server.

**If the server has no file at that path**, it may return **404**. That is **expected** when:

- The workflow copies **`index.html`** to **`404.html`** (see `.github/workflows/deploy.yml` — `cp index.html 404.html` or equivalent), so GitHub Pages serves the SPA shell for unknown paths.
- The browser still loads **`index.html`**, Blazor boots, and MSAL completes the redirect flow in the client.

So a **404** on the initial document request for `/authentication/login-callback` can be **normal**; the important part is that the **page loads** and sign-in completes.

If sign-in fails instead, check:

- **Redirect URIs** in Entra match your GitHub Pages URL exactly (including base path if the app is under `/<repo>/`).
- **`wwwroot/appsettings.json`** (or build-time config) has the correct **`ApiBaseAddress`** and **`AzureAd`** settings.
- **`AzureAd:ApiScope`** matches the API scope exposed by your app registration (see [DEPLOYMENT.md](./DEPLOYMENT.md)).

## API returns 401 after sign-in

The API validates JWT **audience** (`aud`). Access tokens for `api://{clientId}/access` typically have `aud` = `api://{clientId}`. The Functions backend accepts both that value and the bare **client ID** GUID so tokens validate correctly.

Ensure the client requests the **API scope** (included in **`DefaultAccessTokenScopes`** and the **`AuthorizationMessageHandler`** scopes in `Program.cs`) so the token is intended for your API.
