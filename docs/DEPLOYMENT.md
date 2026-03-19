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

### GitHub Pages

1. Repo **Settings → Pages**
2. **Build and deployment → Source**: **Deploy from a branch** (or use the branch `gh-pages` after the first workflow run)
3. If using **Deploy from a branch**: select branch **`gh-pages`**, folder **`/ (root)`**  
   (the workflow publishes to `gh-pages` via [peaceiris/actions-gh-pages](https://github.com/peaceiris/actions-gh-pages))

Your site URL will be:

`https://<YOUR_GITHUB_USER_OR_ORG>.github.io/<REPO_NAME>/`

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

**CORS**: Bicep sets `*` with credentials for quick start. For production, restrict **CORS** allowed origins to your GitHub Pages origin, e.g. `https://your-org.github.io`, and keep **Access-Control-Allow-Credentials** enabled if the client sends cookies (MSAL typically uses bearer tokens; align with your API CORS needs).

## 3. Blazor client production config

After Functions is deployed, set **`wwwroot/appsettings.json`** (or use a production-specific file + build pipeline) so:

- `ApiBaseAddress` is `https://<your-function-app>.azurewebsites.net/` (trailing slash recommended).

Commit and push; the workflow publishes Blazor with **BaseHref** `/<repo>/` for normal project Pages.

**Optional repository variable** `BLAZOR_BASE_HREF`: set to `/` if the repo is a **user or organization** GitHub Pages site (`username.github.io`), where the app is served from the domain root.

## 4. Entra External ID + Google redirect URIs

See [ENTRA_AND_GOOGLE_URIS.md](./ENTRA_AND_GOOGLE_URIS.md) and apply the production URLs after you know your GitHub Pages URL.

## 5. Verify CI/CD

1. Push to `main`.
2. **Actions** tab: `build` should succeed; `deploy-blazor` and `deploy-functions` run on push to `main`.
3. Open the GitHub Pages URL and sign in.

If **deploy-functions** fails with “app not found”, the name in `deploy.yml` does not match the Function App name in Azure.
