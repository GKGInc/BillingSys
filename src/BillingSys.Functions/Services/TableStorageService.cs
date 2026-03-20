using Azure;
using Azure.Data.Tables;
using BillingSys.Shared.Enums;
using BillingSys.Shared.Models;
using Microsoft.Extensions.Logging;

namespace BillingSys.Functions.Services;

#region Table Entities

public class EmployeeEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "EMPLOYEE";
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;
    public double HourlyRate { get; set; }
    public string Role { get; set; } = "User";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Employee ToModel() => new()
    {
        Id = RowKey,
        Name = Name,
        Email = Email ?? string.Empty,
        IsActive = IsActive,
        HourlyRate = (decimal)HourlyRate,
        Role = Enum.TryParse<UserRole>(Role, out var role) ? role : UserRole.User,
        CreatedAt = CreatedAt,
        UpdatedAt = UpdatedAt
    };

    public static EmployeeEntity FromModel(Employee model) => new()
    {
        RowKey = model.Id,
        Name = model.Name,
        Email = model.Email,
        IsActive = model.IsActive,
        HourlyRate = (double)model.HourlyRate,
        Role = model.Role.ToString(),
        CreatedAt = model.CreatedAt,
        UpdatedAt = model.UpdatedAt
    };
}

public class CustomerEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "CUSTOMER";
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string Company { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string PaymentTerms { get; set; } = "Net 30";
    public int PaymentNetDays { get; set; } = 30;
    public string? PrintCode { get; set; }
    public string? EmailCode { get; set; }
    public string? QboCustomerId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastSaleDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Customer ToModel() => new()
    {
        CustomerId = RowKey,
        Company = Company,
        ContactName = ContactName,
        Email = Email,
        Phone = Phone,
        Address = Address,
        City = City,
        State = State,
        ZipCode = ZipCode,
        PaymentTerms = PaymentTerms,
        PaymentNetDays = PaymentNetDays,
        PrintCode = PrintCode,
        EmailCode = EmailCode,
        QboCustomerId = QboCustomerId,
        IsActive = IsActive,
        LastSaleDate = LastSaleDate,
        CreatedAt = CreatedAt,
        UpdatedAt = UpdatedAt
    };

    public static CustomerEntity FromModel(Customer model) => new()
    {
        RowKey = model.CustomerId,
        Company = model.Company,
        ContactName = model.ContactName,
        Email = model.Email,
        Phone = model.Phone,
        Address = model.Address,
        City = model.City,
        State = model.State,
        ZipCode = model.ZipCode,
        PaymentTerms = model.PaymentTerms,
        PaymentNetDays = model.PaymentNetDays,
        PrintCode = model.PrintCode,
        EmailCode = model.EmailCode,
        QboCustomerId = model.QboCustomerId,
        IsActive = model.IsActive,
        LastSaleDate = model.LastSaleDate,
        CreatedAt = model.CreatedAt,
        UpdatedAt = model.UpdatedAt
    };
}

public class ProjectEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string Description { get; set; } = string.Empty;
    public string ServiceItemCode { get; set; } = string.Empty;
    public string? CustomerPO { get; set; }
    public string? ProgrammerId { get; set; }
    public double Price { get; set; }
    public double QuotedHours { get; set; }
    public double AdditionalHours { get; set; }
    public double BilledHours { get; set; }
    public bool PreBill { get; set; }
    public bool AddDetailToInvoice { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Project ToModel() => new()
    {
        ProjectCode = RowKey,
        CustomerId = PartitionKey,
        Description = Description,
        ServiceItemCode = ServiceItemCode,
        CustomerPO = CustomerPO,
        ProgrammerId = ProgrammerId,
        Price = (decimal)Price,
        QuotedHours = (decimal)QuotedHours,
        AdditionalHours = (decimal)AdditionalHours,
        BilledHours = (decimal)BilledHours,
        PreBill = PreBill,
        AddDetailToInvoice = AddDetailToInvoice,
        Status = Enum.TryParse<ProjectStatus>(Status, out var status) ? status : ProjectStatus.Active,
        CreatedAt = CreatedAt,
        UpdatedAt = UpdatedAt
    };

    public static ProjectEntity FromModel(Project model) => new()
    {
        PartitionKey = model.CustomerId,
        RowKey = model.ProjectCode,
        Description = model.Description,
        ServiceItemCode = model.ServiceItemCode,
        CustomerPO = model.CustomerPO,
        ProgrammerId = model.ProgrammerId,
        Price = (double)model.Price,
        QuotedHours = (double)model.QuotedHours,
        AdditionalHours = (double)model.AdditionalHours,
        BilledHours = (double)model.BilledHours,
        PreBill = model.PreBill,
        AddDetailToInvoice = model.AddDetailToInvoice,
        Status = model.Status.ToString(),
        CreatedAt = model.CreatedAt,
        UpdatedAt = model.UpdatedAt
    };
}

public class TimeEntryEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string EmployeeId { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public double Hours { get; set; }
    public bool Billable { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public int Miles { get; set; }
    public string? Comments { get; set; }
    public string Status { get; set; } = "Pending";
    public string? InvoiceNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public TimeEntry ToModel() => new()
    {
        Id = RowKey,
        EmployeeId = EmployeeId,
        EmployeeName = EmployeeName,
        Date = Date,
        StartTime = TimeSpan.Parse(StartTime),
        EndTime = TimeSpan.Parse(EndTime),
        Hours = (decimal)Hours,
        Billable = Billable,
        ProjectCode = ProjectCode,
        Miles = Miles,
        Comments = Comments,
        Status = Enum.TryParse<TimeEntryStatus>(Status, out var status) ? status : TimeEntryStatus.Pending,
        InvoiceNumber = InvoiceNumber,
        CreatedAt = CreatedAt,
        UpdatedAt = UpdatedAt
    };

    public static TimeEntryEntity FromModel(TimeEntry model) => new()
    {
        PartitionKey = $"{model.Year}-{model.WeekNumber:D2}",
        RowKey = model.Id,
        EmployeeId = model.EmployeeId,
        EmployeeName = model.EmployeeName,
        Date = model.Date,
        StartTime = model.StartTime.ToString(@"hh\:mm"),
        EndTime = model.EndTime.ToString(@"hh\:mm"),
        Hours = (double)model.Hours,
        Billable = model.Billable,
        ProjectCode = model.ProjectCode,
        Miles = model.Miles,
        Comments = model.Comments,
        Status = model.Status.ToString(),
        InvoiceNumber = model.InvoiceNumber,
        CreatedAt = model.CreatedAt,
        UpdatedAt = model.UpdatedAt
    };
}

public class InvoiceEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public DateTime InvoiceDate { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? PurchaseOrderNumber { get; set; }
    public double InvoiceAmount { get; set; }
    public string? OrderNumber { get; set; }
    public string Status { get; set; } = "Draft";
    public string? QboInvoiceId { get; set; }
    public DateTime? QboSyncDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Invoice ToModel() => new()
    {
        InvoiceNumber = RowKey,
        InvoiceDate = InvoiceDate,
        CustomerId = CustomerId,
        CustomerName = CustomerName,
        PurchaseOrderNumber = PurchaseOrderNumber,
        InvoiceAmount = (decimal)InvoiceAmount,
        OrderNumber = OrderNumber,
        Status = Enum.TryParse<InvoiceStatus>(Status, out var status) ? status : InvoiceStatus.Draft,
        QboInvoiceId = QboInvoiceId,
        QboSyncDate = QboSyncDate,
        CreatedAt = CreatedAt,
        UpdatedAt = UpdatedAt
    };

    public static InvoiceEntity FromModel(Invoice model) => new()
    {
        PartitionKey = $"{model.InvoiceDate.Year}-{model.InvoiceDate.Month:D2}",
        RowKey = model.InvoiceNumber,
        InvoiceDate = model.InvoiceDate,
        CustomerId = model.CustomerId,
        CustomerName = model.CustomerName,
        PurchaseOrderNumber = model.PurchaseOrderNumber,
        InvoiceAmount = (double)model.InvoiceAmount,
        OrderNumber = model.OrderNumber,
        Status = model.Status.ToString(),
        QboInvoiceId = model.QboInvoiceId,
        QboSyncDate = model.QboSyncDate,
        CreatedAt = model.CreatedAt,
        UpdatedAt = model.UpdatedAt
    };
}

public class ServiceItemEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "ITEM";
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string Description { get; set; } = string.Empty;
    public double Price { get; set; }
    public string? IncomeAccount { get; set; }
    public string? QboItemId { get; set; }
    public string Category { get; set; } = "Service";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ServiceItem ToModel() => new()
    {
        ItemCode = RowKey,
        Description = Description,
        Price = (decimal)Price,
        IncomeAccount = IncomeAccount,
        QboItemId = QboItemId,
        Category = Category,
        IsActive = IsActive,
        CreatedAt = CreatedAt,
        UpdatedAt = UpdatedAt
    };

    public static ServiceItemEntity FromModel(ServiceItem model) => new()
    {
        RowKey = model.ItemCode,
        Description = model.Description,
        Price = (double)model.Price,
        IncomeAccount = model.IncomeAccount,
        QboItemId = model.QboItemId,
        Category = model.Category,
        IsActive = model.IsActive,
        CreatedAt = model.CreatedAt,
        UpdatedAt = model.UpdatedAt
    };
}

public class SystemConfigEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "CONFIG";
    public string RowKey { get; set; } = SystemConfig.SingletonId;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string CompanyName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public int DefaultPaymentNetDays { get; set; } = 30;
    public string DefaultPaymentTerms { get; set; } = "Net 30";
    public string InvoiceNumberPrefix { get; set; } = "";
    public int InvoiceNumberPadding { get; set; } = 6;
    public bool QuickBooksIntegrationEnabled { get; set; } = true;
    public string? QuickBooksRealmId { get; set; }
    public string? DefaultIncomeAccount { get; set; }
    public double DefaultEdiTradingPartnerFee { get; set; }
    public double DefaultNonEdiTradingPartnerFee { get; set; }
    public double DefaultPdfFee { get; set; }
    public double DefaultKilocharRate { get; set; }
    public bool EnableTimeEntryApproval { get; set; } = true;
    public bool EnableProjectBilling { get; set; } = true;
    public bool EnableEdiBilling { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    public SystemConfig ToModel() => new()
    {
        Id = RowKey,
        CompanyName = CompanyName,
        Address = Address,
        City = City,
        State = State,
        ZipCode = ZipCode,
        Phone = Phone,
        Email = Email,
        DefaultPaymentNetDays = DefaultPaymentNetDays,
        DefaultPaymentTerms = DefaultPaymentTerms,
        InvoiceNumberPrefix = InvoiceNumberPrefix,
        InvoiceNumberPadding = InvoiceNumberPadding,
        QuickBooksIntegrationEnabled = QuickBooksIntegrationEnabled,
        QuickBooksRealmId = QuickBooksRealmId,
        DefaultIncomeAccount = DefaultIncomeAccount,
        DefaultEdiTradingPartnerFee = (decimal)DefaultEdiTradingPartnerFee,
        DefaultNonEdiTradingPartnerFee = (decimal)DefaultNonEdiTradingPartnerFee,
        DefaultPdfFee = (decimal)DefaultPdfFee,
        DefaultKilocharRate = (decimal)DefaultKilocharRate,
        EnableTimeEntryApproval = EnableTimeEntryApproval,
        EnableProjectBilling = EnableProjectBilling,
        EnableEdiBilling = EnableEdiBilling,
        CreatedAt = CreatedAt,
        CreatedBy = CreatedBy,
        UpdatedAt = UpdatedAt,
        UpdatedBy = UpdatedBy
    };

    public static SystemConfigEntity FromModel(SystemConfig model) => new()
    {
        RowKey = model.Id,
        CompanyName = model.CompanyName,
        Address = model.Address,
        City = model.City,
        State = model.State,
        ZipCode = model.ZipCode,
        Phone = model.Phone,
        Email = model.Email,
        DefaultPaymentNetDays = model.DefaultPaymentNetDays,
        DefaultPaymentTerms = model.DefaultPaymentTerms,
        InvoiceNumberPrefix = model.InvoiceNumberPrefix,
        InvoiceNumberPadding = model.InvoiceNumberPadding,
        QuickBooksIntegrationEnabled = model.QuickBooksIntegrationEnabled,
        QuickBooksRealmId = model.QuickBooksRealmId,
        DefaultIncomeAccount = model.DefaultIncomeAccount,
        DefaultEdiTradingPartnerFee = (double)model.DefaultEdiTradingPartnerFee,
        DefaultNonEdiTradingPartnerFee = (double)model.DefaultNonEdiTradingPartnerFee,
        DefaultPdfFee = (double)model.DefaultPdfFee,
        DefaultKilocharRate = (double)model.DefaultKilocharRate,
        EnableTimeEntryApproval = model.EnableTimeEntryApproval,
        EnableProjectBilling = model.EnableProjectBilling,
        EnableEdiBilling = model.EnableEdiBilling,
        CreatedAt = model.CreatedAt,
        CreatedBy = model.CreatedBy,
        UpdatedAt = model.UpdatedAt,
        UpdatedBy = model.UpdatedBy
    };
}

#endregion

#region Table Storage Service

public class TableStorageService
{
    private readonly TableServiceClient _serviceClient;
    private readonly ILogger<TableStorageService> _logger;

    private const string EmployeesTable = "Employees";
    private const string CustomersTable = "Customers";
    private const string ProjectsTable = "Projects";
    private const string TimeEntriesTable = "TimeEntries";
    private const string InvoicesTable = "Invoices";
    private const string InvoiceLinesTable = "InvoiceLines";
    private const string ServiceItemsTable = "ServiceItems";
    private const string SystemConfigTable = "SystemConfig";

    public TableStorageService(string connectionString, ILogger<TableStorageService> logger)
    {
        _serviceClient = new TableServiceClient(connectionString);
        _logger = logger;
    }

    public async Task InitializeTablesAsync()
    {
        var tables = new[] { EmployeesTable, CustomersTable, ProjectsTable, TimeEntriesTable, InvoicesTable, InvoiceLinesTable, ServiceItemsTable, SystemConfigTable };
        foreach (var table in tables)
        {
            await _serviceClient.CreateTableIfNotExistsAsync(table);
        }
    }

    private TableClient GetTable(string tableName) => _serviceClient.GetTableClient(tableName);

    #region Employee Operations

    public async Task<ServiceResult<Employee>> GetEmployeeAsync(string id)
    {
        try
        {
            var table = GetTable(EmployeesTable);
            var response = await table.GetEntityAsync<EmployeeEntity>("EMPLOYEE", id);
            return ServiceResult<Employee>.Ok(response.Value.ToModel());
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return ServiceResult<Employee>.Fail($"Employee {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting employee {Id}", id);
            return ServiceResult<Employee>.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult<List<Employee>>> GetAllEmployeesAsync(bool activeOnly = true)
    {
        try
        {
            var table = GetTable(EmployeesTable);
            var filter = activeOnly ? "PartitionKey eq 'EMPLOYEE' and IsActive eq true" : "PartitionKey eq 'EMPLOYEE'";
            var employees = new List<Employee>();
            await foreach (var entity in table.QueryAsync<EmployeeEntity>(filter))
            {
                employees.Add(entity.ToModel());
            }
            return ServiceResult<List<Employee>>.Ok(employees);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting employees");
            return ServiceResult<List<Employee>>.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult<Employee>> UpsertEmployeeAsync(Employee employee)
    {
        try
        {
            var table = GetTable(EmployeesTable);
            var entity = EmployeeEntity.FromModel(employee);
            await table.UpsertEntityAsync(entity);
            return ServiceResult<Employee>.Ok(employee);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting employee {Id}", employee.Id);
            return ServiceResult<Employee>.Fail(ex.Message);
        }
    }

    #endregion

    #region Customer Operations

    public async Task<ServiceResult<Customer>> GetCustomerAsync(string id)
    {
        try
        {
            var table = GetTable(CustomersTable);
            var response = await table.GetEntityAsync<CustomerEntity>("CUSTOMER", id);
            return ServiceResult<Customer>.Ok(response.Value.ToModel());
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return ServiceResult<Customer>.Fail($"Customer {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer {Id}", id);
            return ServiceResult<Customer>.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult<List<Customer>>> GetAllCustomersAsync(bool activeOnly = true)
    {
        try
        {
            var table = GetTable(CustomersTable);
            var filter = activeOnly ? "PartitionKey eq 'CUSTOMER' and IsActive eq true" : "PartitionKey eq 'CUSTOMER'";
            var customers = new List<Customer>();
            await foreach (var entity in table.QueryAsync<CustomerEntity>(filter))
            {
                customers.Add(entity.ToModel());
            }
            return ServiceResult<List<Customer>>.Ok(customers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customers");
            return ServiceResult<List<Customer>>.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult<Customer>> UpsertCustomerAsync(Customer customer)
    {
        try
        {
            var table = GetTable(CustomersTable);
            var entity = CustomerEntity.FromModel(customer);
            await table.UpsertEntityAsync(entity);
            return ServiceResult<Customer>.Ok(customer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting customer {Id}", customer.CustomerId);
            return ServiceResult<Customer>.Fail(ex.Message);
        }
    }

    #endregion

    #region Project Operations

    public async Task<ServiceResult<Project>> GetProjectAsync(string customerId, string projectCode)
    {
        try
        {
            var table = GetTable(ProjectsTable);
            var response = await table.GetEntityAsync<ProjectEntity>(customerId, projectCode);
            return ServiceResult<Project>.Ok(response.Value.ToModel());
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return ServiceResult<Project>.Fail($"Project {projectCode} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting project {ProjectCode}", projectCode);
            return ServiceResult<Project>.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult<List<Project>>> GetProjectsByCustomerAsync(string customerId)
    {
        try
        {
            var table = GetTable(ProjectsTable);
            var filter = $"PartitionKey eq '{customerId}'";
            var projects = new List<Project>();
            await foreach (var entity in table.QueryAsync<ProjectEntity>(filter))
            {
                projects.Add(entity.ToModel());
            }
            return ServiceResult<List<Project>>.Ok(projects);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting projects for customer {CustomerId}", customerId);
            return ServiceResult<List<Project>>.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult<List<Project>>> GetAllProjectsAsync(ProjectStatus? status = null)
    {
        try
        {
            var table = GetTable(ProjectsTable);
            var projects = new List<Project>();
            await foreach (var entity in table.QueryAsync<ProjectEntity>())
            {
                var project = entity.ToModel();
                if (status == null || project.Status == status)
                {
                    projects.Add(project);
                }
            }
            return ServiceResult<List<Project>>.Ok(projects);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all projects");
            return ServiceResult<List<Project>>.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult<Project>> UpsertProjectAsync(Project project)
    {
        try
        {
            var table = GetTable(ProjectsTable);
            var entity = ProjectEntity.FromModel(project);
            await table.UpsertEntityAsync(entity);
            return ServiceResult<Project>.Ok(project);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting project {ProjectCode}", project.ProjectCode);
            return ServiceResult<Project>.Fail(ex.Message);
        }
    }

    #endregion

    #region Time Entry Operations

    public async Task<ServiceResult<TimeEntry>> GetTimeEntryAsync(string yearWeek, string id)
    {
        try
        {
            var table = GetTable(TimeEntriesTable);
            var response = await table.GetEntityAsync<TimeEntryEntity>(yearWeek, id);
            return ServiceResult<TimeEntry>.Ok(response.Value.ToModel());
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return ServiceResult<TimeEntry>.Fail($"Time entry {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting time entry {Id}", id);
            return ServiceResult<TimeEntry>.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult<List<TimeEntry>>> GetTimeEntriesAsync(int year, int weekNumber, string? employeeId = null)
    {
        try
        {
            var table = GetTable(TimeEntriesTable);
            var partitionKey = $"{year}-{weekNumber:D2}";
            var filter = $"PartitionKey eq '{partitionKey}'";
            if (!string.IsNullOrEmpty(employeeId))
            {
                filter += $" and EmployeeId eq '{employeeId}'";
            }
            var entries = new List<TimeEntry>();
            await foreach (var entity in table.QueryAsync<TimeEntryEntity>(filter))
            {
                entries.Add(entity.ToModel());
            }
            return ServiceResult<List<TimeEntry>>.Ok(entries.OrderBy(e => e.Date).ThenBy(e => e.StartTime).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting time entries for week {Year}-{Week}", year, weekNumber);
            return ServiceResult<List<TimeEntry>>.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult<List<TimeEntry>>> GetTimeEntriesByDateRangeAsync(DateTime startDate, DateTime endDate, string? employeeId = null)
    {
        try
        {
            var table = GetTable(TimeEntriesTable);
            var entries = new List<TimeEntry>();
            
            await foreach (var entity in table.QueryAsync<TimeEntryEntity>())
            {
                if (entity.Date >= startDate && entity.Date <= endDate)
                {
                    if (string.IsNullOrEmpty(employeeId) || entity.EmployeeId == employeeId)
                    {
                        entries.Add(entity.ToModel());
                    }
                }
            }
            return ServiceResult<List<TimeEntry>>.Ok(entries.OrderBy(e => e.Date).ThenBy(e => e.StartTime).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting time entries for date range");
            return ServiceResult<List<TimeEntry>>.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult<TimeEntry>> UpsertTimeEntryAsync(TimeEntry entry)
    {
        try
        {
            var table = GetTable(TimeEntriesTable);
            var entity = TimeEntryEntity.FromModel(entry);
            await table.UpsertEntityAsync(entity);
            return ServiceResult<TimeEntry>.Ok(entry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting time entry {Id}", entry.Id);
            return ServiceResult<TimeEntry>.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult> DeleteTimeEntryAsync(string yearWeek, string id)
    {
        try
        {
            var table = GetTable(TimeEntriesTable);
            await table.DeleteEntityAsync(yearWeek, id);
            return ServiceResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting time entry {Id}", id);
            return ServiceResult.Fail(ex.Message);
        }
    }

    #endregion

    #region Invoice Operations

    public async Task<ServiceResult<Invoice>> GetInvoiceAsync(string yearMonth, string invoiceNumber)
    {
        try
        {
            var table = GetTable(InvoicesTable);
            var response = await table.GetEntityAsync<InvoiceEntity>(yearMonth, invoiceNumber);
            return ServiceResult<Invoice>.Ok(response.Value.ToModel());
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return ServiceResult<Invoice>.Fail($"Invoice {invoiceNumber} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting invoice {InvoiceNumber}", invoiceNumber);
            return ServiceResult<Invoice>.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult<List<Invoice>>> GetInvoicesByMonthAsync(int year, int month)
    {
        try
        {
            var table = GetTable(InvoicesTable);
            var partitionKey = $"{year}-{month:D2}";
            var filter = $"PartitionKey eq '{partitionKey}'";
            var invoices = new List<Invoice>();
            await foreach (var entity in table.QueryAsync<InvoiceEntity>(filter))
            {
                invoices.Add(entity.ToModel());
            }
            return ServiceResult<List<Invoice>>.Ok(invoices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting invoices for {Year}-{Month}", year, month);
            return ServiceResult<List<Invoice>>.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult<string>> GetNextInvoiceNumberAsync(DateTime invoiceDate)
    {
        try
        {
            var table = GetTable(InvoicesTable);
            var partitionKey = $"{invoiceDate.Year}-{invoiceDate.Month:D2}";
            var baseNumber = long.Parse($"{invoiceDate:yyyyMMdd}00");
            var maxNumber = baseNumber;

            await foreach (var entity in table.QueryAsync<InvoiceEntity>($"PartitionKey eq '{partitionKey}'"))
            {
                if (long.TryParse(entity.RowKey, out var num) && num > maxNumber)
                {
                    maxNumber = num;
                }
            }

            return ServiceResult<string>.Ok((maxNumber + 1).ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting next invoice number");
            return ServiceResult<string>.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult<Invoice>> UpsertInvoiceAsync(Invoice invoice)
    {
        try
        {
            var table = GetTable(InvoicesTable);
            var entity = InvoiceEntity.FromModel(invoice);
            await table.UpsertEntityAsync(entity);
            return ServiceResult<Invoice>.Ok(invoice);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting invoice {InvoiceNumber}", invoice.InvoiceNumber);
            return ServiceResult<Invoice>.Fail(ex.Message);
        }
    }

    #endregion

    #region Service Item Operations

    public async Task<ServiceResult<ServiceItem>> GetServiceItemAsync(string itemCode)
    {
        try
        {
            var table = GetTable(ServiceItemsTable);
            var response = await table.GetEntityAsync<ServiceItemEntity>("ITEM", itemCode);
            return ServiceResult<ServiceItem>.Ok(response.Value.ToModel());
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return ServiceResult<ServiceItem>.Fail($"Service item {itemCode} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting service item {ItemCode}", itemCode);
            return ServiceResult<ServiceItem>.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult<List<ServiceItem>>> GetAllServiceItemsAsync(bool activeOnly = true)
    {
        try
        {
            var table = GetTable(ServiceItemsTable);
            var filter = activeOnly ? "PartitionKey eq 'ITEM' and IsActive eq true" : "PartitionKey eq 'ITEM'";
            var items = new List<ServiceItem>();
            await foreach (var entity in table.QueryAsync<ServiceItemEntity>(filter))
            {
                items.Add(entity.ToModel());
            }
            return ServiceResult<List<ServiceItem>>.Ok(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting service items");
            return ServiceResult<List<ServiceItem>>.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult<ServiceItem>> UpsertServiceItemAsync(ServiceItem item)
    {
        try
        {
            var table = GetTable(ServiceItemsTable);
            var entity = ServiceItemEntity.FromModel(item);
            await table.UpsertEntityAsync(entity);
            return ServiceResult<ServiceItem>.Ok(item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting service item {ItemCode}", item.ItemCode);
            return ServiceResult<ServiceItem>.Fail(ex.Message);
        }
    }

    #endregion

    #region System Config Operations

    public async Task<ServiceResult<SystemConfig>> GetSystemConfigAsync()
    {
        try
        {
            var table = GetTable(SystemConfigTable);
            var response = await table.GetEntityAsync<SystemConfigEntity>("CONFIG", SystemConfig.SingletonId);
            return ServiceResult<SystemConfig>.Ok(response.Value.ToModel());
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            var defaultConfig = SystemConfig.CreateDefault();
            return ServiceResult<SystemConfig>.Ok(defaultConfig);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system config");
            return ServiceResult<SystemConfig>.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult<SystemConfig>> UpsertSystemConfigAsync(SystemConfig config)
    {
        try
        {
            var table = GetTable(SystemConfigTable);
            var entity = SystemConfigEntity.FromModel(config);
            await table.UpsertEntityAsync(entity);
            return ServiceResult<SystemConfig>.Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting system config");
            return ServiceResult<SystemConfig>.Fail(ex.Message);
        }
    }

    #endregion
}

#endregion
