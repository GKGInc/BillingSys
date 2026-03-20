using Azure.Data.Tables;

namespace BillingSys.Functions.Repositories;

public class TableStorageContext
{
    private readonly TableServiceClient _serviceClient;

    public const string EmployeesTable = "Employees";
    public const string CustomersTable = "Customers";
    public const string ProjectsTable = "Projects";
    public const string TimeEntriesTable = "TimeEntries";
    public const string InvoicesTable = "Invoices";
    public const string InvoiceLinesTable = "InvoiceLines";
    public const string ServiceItemsTable = "ServiceItems";
    public const string SystemConfigTable = "SystemConfig";

    public TableStorageContext(string connectionString)
    {
        _serviceClient = new TableServiceClient(connectionString);
    }

    public TableClient GetTable(string tableName) => _serviceClient.GetTableClient(tableName);

    public async Task InitializeTablesAsync()
    {
        var tables = new[]
        {
            EmployeesTable, CustomersTable, ProjectsTable, TimeEntriesTable,
            InvoicesTable, InvoiceLinesTable, ServiceItemsTable, SystemConfigTable
        };

        foreach (var table in tables)
        {
            await _serviceClient.CreateTableIfNotExistsAsync(table);
        }
    }
}
