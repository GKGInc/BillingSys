# Billing System Web Application

A modern web-based billing system built with Blazor WebAssembly and Azure Functions, replacing the legacy Visual FoxPro system.

## Architecture

```
┌─────────────────────┐     ┌─────────────────────┐     ┌─────────────────────┐
│   GitHub Pages      │────▶│  Azure Functions    │────▶│  Azure Storage      │
│   (Blazor WASM)     │     │  (REST API)         │     │  (Table Storage)    │
└─────────────────────┘     └─────────────────────┘     └─────────────────────┘
                                      │
                                      ▼
                            ┌─────────────────────┐
                            │  QuickBooks Online  │
                            │  (Invoice Sync)     │
                            └─────────────────────┘
```

## Features

- **Authentication**: Microsoft Entra External ID with Google OAuth (restricted to tech85.com domain)
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
- **Hosting**: GitHub Pages (frontend), Azure Functions (API)
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

#### Microsoft Entra External ID Setup (Authentication)

1. **Create External Tenant**:
   - Go to [Microsoft Entra admin center](https://entra.microsoft.com/)
   - Navigate to Identity → Overview → Manage tenants
   - Select Create → External → Continue
   - Tenant name: `tech85` (creates `tech85.onmicrosoft.com`)
   - Select your Azure subscription and resource group
   - Note: Tenant creation can take up to 30 minutes

2. **Register Application**:
   - In the external tenant → App registrations → New registration
   - Name: `BillingSys`
   - Supported account types: "Accounts in this organizational directory only"
   - Redirect URI (Single-page application):
     - `https://your-github-pages-url/authentication/login-callback`
     - `https://localhost:5001/authentication/login-callback` (for local dev)
   - Copy the Application (client) ID

3. **Configure Google Identity Provider**:
   - Go to [Google Cloud Console](https://console.cloud.google.com/)
   - Create OAuth 2.0 credentials (Web application)
   - Add authorized redirect URIs (replace `tech85` with your tenant name):
     - `https://tech85.ciamlogin.com/tech85.onmicrosoft.com/federation/oauth2`
     - `https://tech85.ciamlogin.com/tech85/federation/oauth2`
     - `https://tech85.ciamlogin.com/tech85.onmicrosoft.com/federation/oidc/accounts.google.com`
     - `https://login.microsoftonline.com/te/tech85.onmicrosoft.com/oauth2/authresp`
   - Copy Client ID and Client Secret
   - In Entra admin center → External Identities → All identity providers → Add Google
   - Enter the Google Client ID and Client Secret

4. **Configure Authentication Methods**:
   - In external tenant → Authentication methods
   - Enable Google as an authentication method
   - Configure user attributes to collect email

5. **Configure API Permissions** (optional):
   - Register an API application for the Functions backend
   - Expose an API scope: `api://billingsys/access`
   - Grant the Client app permission to use this scope

**Note**: The `AllowedEmailDomain` setting in Azure Functions validates that only users with @tech85.com email addresses can access the application.

#### Azure Functions (`local.settings.json`)
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "SqlConnectionString": "your-sql-connection-string",
    "AzureAd__TenantName": "tech85",
    "AzureAd__ClientId": "your-client-id",
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
  "AzureAd": {
    "Authority": "https://tech85.ciamlogin.com/",
    "ClientId": "your-client-id",
    "ValidateAuthority": true,
    "ApiScope": "api://billingsys/access"
  }
}
```

## Deployment

Step-by-step (GitHub remote, Azure Bicep, secrets, Pages, Entra URIs): **[docs/DEPLOYMENT.md](docs/DEPLOYMENT.md)**  
Production redirect URI checklist: **[docs/ENTRA_AND_GOOGLE_URIS.md](docs/ENTRA_AND_GOOGLE_URIS.md)**  
Manual E2E verification: **[docs/E2E_TEST_CHECKLIST.md](docs/E2E_TEST_CHECKLIST.md)**

Local Functions settings template (copy to `local.settings.json`, not committed): **`src/BillingSys.Functions/local.settings.json.example`**

### GitHub Pages (Frontend)

The Blazor WASM app is automatically deployed to GitHub Pages on push to `main` branch.

### Azure Functions (Backend)

1. Create an Azure Functions app in the Azure Portal

2. Add the following Application Settings:
   - `AzureWebJobsStorage`: Azure Storage connection string
   - `SqlConnectionString`: SQL Server connection string (for EDI)
   - `AzureAd__TenantName`: `tech85`
   - `AzureAd__ClientId`: Your Microsoft Entra External ID Client ID
   - `AllowedEmailDomain`: `tech85.com`
   - `QBO_REALM_ID`, `QBO_CLIENT_ID`, `QBO_CLIENT_SECRET`: QuickBooks credentials

3. Configure CORS:
   - In Azure Portal → Functions App → CORS
   - Add your GitHub Pages URL (e.g., `https://your-org.github.io`)
   - Enable "Access-Control-Allow-Credentials"

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
