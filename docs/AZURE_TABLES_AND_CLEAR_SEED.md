# Azure Table Storage — table names & clearing test data

The BillingSys Functions app stores most operational data in **Azure Storage Tables** (same storage account as `AzureWebJobsStorage` unless you split accounts later).

## Table names (exact)

Defined in `TableStorageContext` (`src/BillingSys.Functions/Repositories/TableStorageContext.cs`):

| Table name      | Contents |
|-----------------|----------|
| **Employees**   | Employee records |
| **Customers**   | Customers |
| **Projects**    | Projects |
| **TimeEntries** | Time entries |
| **Invoices**    | Invoices |
| **InvoiceLines**| Invoice line items |
| **ServiceItems**| Service items |
| **SystemConfig**| App/system configuration (initialization) |

## Option A — Clear via API (dev / opt-in)

**POST** `https://<your-function-app>.azurewebsites.net/api/admin/clear-seed`

- **Body (required):** `{ "confirm": true }`
- **Headers:** `Content-Type: application/json` and **`Authorization: Bearer <JWT>`** for a user whose employee record has the **Admin** role.
- **Behavior:** Deletes **all entities** in the tables listed below (does **not** delete the table itself). **SystemConfig is not cleared** so initialization flags remain.

**Tables cleared:** `Employees`, `Customers`, `Projects`, `TimeEntries`, `Invoices`, `InvoiceLines`, `ServiceItems`

### When the endpoint is allowed

Callers must be **authenticated as Admin** (see header above). The endpoint then returns **403 Forbidden** unless **one** of these is also true:

1. **`DOTNET_ENVIRONMENT=Development`** or **`ASPNETCORE_ENVIRONMENT=Development`** (typical when running Functions locally), or  
2. **`BILLINGSYS_ALLOW_CLEAR_SEED=true`** is set in **Function App → Configuration → Application settings** (use only on a **dev/slot** environment — never enable on production unless you accept full data loss in those tables).

**Example (curl):**

```bash
curl -X POST "https://billingsys-func-tech85.azurewebsites.net/api/admin/clear-seed" ^
  -H "Content-Type: application/json" ^
  -H "Authorization: Bearer <access_token>" ^
  -d "{\"confirm\":true}"
```

**Example (local Functions):** ensure `local.settings.json` has `"DOTNET_ENVIRONMENT": "Development"` (or use the opt-in app setting).

## Option B — Clear in Azure Portal (manual)

1. Open **Azure Portal** → your **Storage account** (the one used for app data / `AzureWebJobsStorage`).
2. **Storage browser** (or **Tables** under **Data storage**).
3. Open each table: **Employees**, **Customers**, **Projects**, **TimeEntries**, **Invoices**, **InvoiceLines**, **ServiceItems**.
4. Select entities and **Delete** (or use **Edit query** to list partitions, then delete).  
   - For large tables, the API (Option A) or a script is easier than clicking thousands of rows.

**SystemConfig:** only clear if you know what you’re doing; it may affect initialization.

## After clearing

- Run **seed** again from `seed-data.html` on GitHub Pages, or  
- Re-import / enter **real** production data through the app or migration APIs.
