# BillingSys deployment guide

Follow these steps once per environment. Replace placeholders with your org/repo names.

## 1. GitHub repository

Local repo lives at `C:\Projects\Tech85_BillingSys`. Default branch must be **`main`** (GitHub Actions triggers on `main`).

```powershell
cd C:\Projects\Tech85_BillingSys
git branch -M main
```

Create an empty repository on GitHub (no README/license to avoid merge conflicts), then:

```powershell
git remote add origin https://github.com/<YOUR_ORG>/<YOUR_REPO>.git
git push -u origin main
```

### Azure Static Web Apps (Blazor frontend)

The workflow **`deploy-blazor`** uploads the published Blazor **`wwwroot`** to **Azure Static Web Apps** using [Azure/static-web-apps-deploy](https://github.com/Azure/static-web-apps-deploy). SPA routing is configured by **`wwwroot/staticwebapp.config.json`** (`navigationFallback` → `/index.html`).

1. Create or use an existing **Static Web App** in Azure (or connect the repo from the portal to generate a workflow — you can align secrets with this repo’s workflow).
2. In GitHub: **Settings → Secrets and variables → Actions → New repository secret**
   - Name: **`AZURE_STATIC_WEB_APPS_API_TOKEN`**
   - Value: the **deployment token** from Azure (**Static Web App → Overview → Manage deployment token**, or the token shown when linking GitHub).

Your app URL will be the Static Web App hostname, e.g. `https://<app-name>.azurestaticapps.net` (or your custom domain).

**Auth / client routes:** With **`navigationFallback`**, deep links such as `/authentication/login-callback` are served **`index.html`** so Blazor can handle the route. See **[GITHUB_PAGES_AUTH.md](./GITHUB_PAGES_AUTH.md)** for Entra redirect URI notes (apply your SWA URL instead of GitHub Pages where relevant).

### GitHub Actions secrets (Azure Functions deploy)

1. Create a service principal (replace subscription and resource group):

```powershell
az login
az ad sp create-for-rbac --name "billingsys-github-actions" `
  --role contributor `
  --scopes /subscriptions/<SUBSCRIPTION_ID>/resourceGroups/<RESOURCE_GROUP> `
  --sdk-auth
```

2. Copy the JSON output.
3. In GitHub: **Settings → Secrets and variables → Actions → New repository secret**
   - Name: `AZURE_CREDENTIALS`
   - Value: the full JSON from the command above

4. Edit `.github/workflows/deploy.yml` and set `AZURE_FUNCTIONAPP_NAME` under `env:` to match the **exact** name of your Function App in Azure (see below).

## 2. Azure resources (Storage + Functions)

Use the included Bicep file from a resource group you already created:

```powershell
az group create -n billingsys-rg -l eastus

az deployment group create `
  -g billingsys-rg `
  -f infra/main.bicep `
  -p functionAppName=<globally-unique-func-name> `
  -p storageAccountName=<globallyunique3to24>
```

Notes:

- `functionAppName` must be globally unique among Azure Function apps.
- `storageAccountName` must be **lowercase letters and numbers only**, 3–24 characters, globally unique.

After deployment, copy outputs if needed:

```powershell
az deployment group show -g billingsys-rg -n main --query properties.outputs
```

### Function App application settings (Portal or CLI)

Add or update these in **Function App → Configuration → Application settings**:

| Name | Example / notes |
|------|------------------|
| `AzureWebJobsStorage` | Use the **dedicated** storage connection string for Functions runtime (Bicep sets this; add a **second** storage account later if you want app data separate). |
| `SqlConnectionString` | Azure SQL connection string for EDI (optional until EDI is used). |
| `AzureAd__TenantName` | `tech85` |
| `AzureAd__ClientId` | SPA app registration client ID |
| `AllowedEmailDomain` | `tech85.com` |
| `QBO_REALM_ID`, `QBO_CLIENT_ID`, `QBO_CLIENT_SECRET` | When QBO is configured |
| `BILLINGSYS_ALLOW_CLEAR_SEED` | Set to `true` **only** on a dev/slot to enable `POST /api/admin/clear-seed` (dangerous — wipes business tables). Omit or `false` in production. |

**CORS**: Bicep sets `*` with credentials for quick start. For production, restrict **CORS** allowed origins to your **Azure Static Web Apps** URL (and custom domain if used), e.g. `https://<app>.azurestaticapps.net`, and keep **Access-Control-Allow-Credentials** aligned with your API needs (MSAL typically uses bearer tokens).

## 3. Blazor client production config

After Functions is deployed, set **`wwwroot/appsettings.json`** (or use a production-specific file + build pipeline) so:

- `ApiBaseAddress` is `https://<your-function-app>.azurewebsites.net/` (trailing slash recommended).

Commit and push; the workflow publishes Blazor **without** rewriting `index.html` — **`base href` stays `/`**, which matches **Azure Static Web Apps** (and similar hosts) when the app is served from the site root.

## 4. Entra External ID + Google redirect URIs

See [ENTRA_AND_GOOGLE_URIS.md](./ENTRA_AND_GOOGLE_URIS.md) and apply the production URLs after you know your **Azure Static Web Apps** URL (and any custom domain).

## 5. Verify CI/CD

1. Push to `main`.
2. **Actions** tab: `build` should succeed; `deploy-blazor` and `deploy-functions` run on push to `main`.
3. Open the **Azure Static Web Apps** URL and sign in.

If **deploy-functions** fails with “app not found”, the name in `deploy.yml` does not match the Function App name in Azure.

## 6. Clearing test / seed data (tables or API)

See **[AZURE_TABLES_AND_CLEAR_SEED.md](./AZURE_TABLES_AND_CLEAR_SEED.md)** for exact Azure Table names, Portal steps, and the optional dev-only **`POST /api/admin/clear-seed`** endpoint.
