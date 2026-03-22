# Billing System Web Application

A modern web-based billing system built with Blazor WebAssembly and Azure Functions, replacing the legacy Visual FoxPro system.

## Architecture

```
┌──────────────────────────┐     ┌─────────────────────┐     ┌─────────────────────┐
│ Azure Static Web Apps    │────▶│  Azure Functions    │────▶│  Azure Storage      │
│ (Blazor WASM)            │     │  (REST API)         │     │  (Table Storage)    │
└──────────────────────────┘     └─────────────────────┘     └─────────────────────┘
                                      │
                                      ▼
                            ┌─────────────────────┐
                            │  QuickBooks Online  │
                            │  (Invoice Sync)     │
                            └─────────────────────┘
```

## Features

- **Authentication**: Direct **Google OAuth 2.0** / OpenID Connect (ID token; restricted to tech85.com domain via API)
- **Time Entry**: Calendar-based time entry with project selection
- **Weekly Billing**: Consolidate employee hours and generate invoices
- **Project Management**: Track quoted hours, billed hours, and remaining hours
- **EDI Billing**: Monthly EDI trading partner billing (connects to existing Azure SQL)
- **QuickBooks Integration**: Sync invoices to QuickBooks Online via API
- **Reports**: Weekly hours summary, employee utilization, project status

## Technology Stack

- **Frontend**: Blazor WebAssembly (.NET 8)
- **Backend**: Azure Functions (C#, isolated worker model)
- **Data Storage**: Azure Table Storage
- **Hosting**: Azure Static Web Apps (frontend), Azure Functions (API)
- **Accounting**: QuickBooks Online API (OAuth2)

## Project Structure

```
BillingSys/
├── src/
│   ├── BillingSys.Shared/       # Shared models and DTOs
│   ├── BillingSys.Client/       # Blazor WASM frontend
│   └── BillingSys.Functions/    # Azure Functions API
├── .github/
│   └── workflows/
│       └── deploy.yml           # CI/CD pipeline
└── README.md
```

## Getting Started

### Prerequisites

- .NET 8 SDK
- Azure Storage Emulator (Azurite) or Azure Storage account
- Visual Studio 2022 or VS Code

### Local Development

1. Clone the repository (or open `C:\Projects\Tech85_BillingSys\src\BillingSys.slnx` in Visual Studio 2022):
   ```bash
   git clone https://github.com/your-org/BillingSys.git
   cd BillingSys
   ```

2. Start the Azure Storage Emulator:
   ```bash
   azurite --silent --location ./azurite-data
   ```

3. Start the Azure Functions:
   ```bash
   cd src/BillingSys.Functions
   func start
   ```

4. Start the Blazor client:
   ```bash
   cd src/BillingSys.Client
   dotnet run
   ```

5. Open browser to `https://localhost:5001`

### Configuration

#### Google OAuth 2.0 (direct — authentication)

1. In [Google Cloud Console](https://console.cloud.google.com/) → **APIs & Services** → **Credentials** → **Create credentials** → **OAuth client ID** → **Web application**.
2. **Authorized JavaScript origins**: e.g. `https://localhost:5001`, `https://<your-static-web-app>.azurestaticapps.net` (origins only).
3. **Authorized redirect URIs**: e.g. `https://localhost:5001/authentication/login-callback` and `https://<your-static-web-app>.azurestaticapps.net/authentication/login-callback`.
4. Copy the **Client ID** into Blazor `wwwroot/appsettings.json` as **`Google:ClientId`** and into Azure Functions as **`Google__ClientId`** (same value).

**Note:** `AllowedEmailDomain` on the Function App restricts sign-in to that email domain (e.g. `tech85.com`). See **[docs/ENTRA_AND_GOOGLE_URIS.md](docs/ENTRA_AND_GOOGLE_URIS.md)** for the full redirect URI checklist.

#### Azure Functions (`local.settings.json`)
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "SqlConnectionString": "your-sql-connection-string",
    "Google__ClientId": "your-google-oauth-web-client-id",
    "AllowedEmailDomain": "tech85.com",
    "QBO_REALM_ID": "your-qbo-realm-id",
    "QBO_CLIENT_ID": "your-qbo-client-id",
    "QBO_CLIENT_SECRET": "your-qbo-client-secret"
  },
  "Host": {
    "CORS": "*",
    "CORSCredentials": true
  }
}
```

#### Blazor Client (`wwwroot/appsettings.json`)
```json
{
  "ApiBaseAddress": "https://your-functions-app.azurewebsites.net/",
  "Google": {
    "ClientId": "your-google-oauth-web-client-id"
  }
}
```

## Deployment

Step-by-step (GitHub remote, Azure Bicep, secrets, Static Web Apps, Google URIs): **[docs/DEPLOYMENT.md](docs/DEPLOYMENT.md)**  
Production redirect URI checklist: **[docs/ENTRA_AND_GOOGLE_URIS.md](docs/ENTRA_AND_GOOGLE_URIS.md)**  
Manual E2E verification: **[docs/E2E_TEST_CHECKLIST.md](docs/E2E_TEST_CHECKLIST.md)**

Local Functions settings template (copy to `local.settings.json`, not committed): **`src/BillingSys.Functions/local.settings.json.example`**

### Azure Static Web Apps (Frontend)

The Blazor WASM app is deployed to **Azure Static Web Apps** on push to `main` (see `.github/workflows/deploy.yml`). Configure the **`AZURE_STATIC_WEB_APPS_API_TOKEN`** repository secret.

### Azure Functions (Backend)

1. Create an Azure Functions app in the Azure Portal

2. Add the following Application Settings:
   - `AzureWebJobsStorage`: Azure Storage connection string
   - `SqlConnectionString`: SQL Server connection string (for EDI)
   - `Google__ClientId`: Same **Google OAuth Web client ID** as in Blazor `Google:ClientId`
   - `AllowedEmailDomain`: `tech85.com`
   - `QBO_REALM_ID`, `QBO_CLIENT_ID`, `QBO_CLIENT_SECRET`: QuickBooks credentials

3. Configure CORS:
   - In Azure Portal → Functions App → CORS
   - Add your **Azure Static Web Apps** URL (e.g. `https://<app>.azurestaticapps.net`)
   - Enable "Access-Control-Allow-Credentials" if your API requires it

4. Set up the GitHub Actions secret:
   - `AZURE_CREDENTIALS`: Service principal JSON for Azure login

5. Push to `main` to trigger deployment

## Data Migration

Use the migration endpoints to import data from the legacy system:

```bash
# Import employees
curl -X POST https://your-functions-app/api/migration/employees \
  -H "Content-Type: application/json" \
  -d '[{"id": "GG", "name": "Gary Gregory", "isActive": true}]'

# Import customers
curl -X POST https://your-functions-app/api/migration/customers \
  -H "Content-Type: application/json" \
  -d '[{"customerId": "ABC123", "company": "ABC Corp", "isActive": true}]'

# Import projects
curl -X POST https://your-functions-app/api/migration/projects \
  -H "Content-Type: application/json" \
  -d '[{"projectCode": "ABC001", "customerId": "ABC123", "description": "Web App", "quotedHours": 100, "price": 150}]'

# Import time entries from CSV
curl -X POST https://your-functions-app/api/migration/timeentries/csv \
  -H "Content-Type: text/csv" \
  --data-binary @emp2025hrs.csv
```

## API Endpoints

### Time Entries
- `GET /api/timeentries?year=2026&week=12&employeeId=GG`
- `POST /api/timeentries`
- `PUT /api/timeentries/{yearWeek}/{id}`
- `DELETE /api/timeentries/{yearWeek}/{id}`

### Projects
- `GET /api/projects?status=Active`
- `GET /api/projects/summaries`
- `POST /api/projects`
- `PUT /api/projects/{customerId}/{projectCode}`

### Billing
- `GET /api/billing/weekly/preview?year=2026&week=12`
- `POST /api/billing/weekly/process`
- `POST /api/billing/project/process`

### EDI
- `GET /api/edi/billing/preview?year=2026&month=3`
- `POST /api/edi/billing/process`

### QuickBooks
- `POST /api/qbo/sync/invoice`
- `POST /api/qbo/sync/invoices`

## License

Private - All rights reserved
