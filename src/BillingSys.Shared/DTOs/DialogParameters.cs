using System.ComponentModel.DataAnnotations;

namespace BillingSys.Shared.DTOs;

/// <summary>
/// Base class for dialog parameters that provides common validation support
/// </summary>
public abstract class DialogParametersBase
{
    /// <summary>
    /// Validates the parameters and returns any error messages
    /// </summary>
    public abstract IEnumerable<string> Validate();

    /// <summary>
    /// Returns true if the parameters are valid
    /// </summary>
    public bool IsValid => !Validate().Any();
}

/// <summary>
/// Parameters for confirming a potentially destructive action
/// </summary>
public class ConfirmationDialogParameters : DialogParametersBase
{
    #region Properties

    public string Title { get; set; } = "Confirm Action";
    public string Message { get; set; } = "Are you sure you want to proceed?";
    public string ConfirmButtonText { get; set; } = "Confirm";
    public string CancelButtonText { get; set; } = "Cancel";
    public bool IsDangerous { get; set; }

    /// <summary>
    /// Optional text the user must type to confirm (for extra dangerous operations)
    /// </summary>
    public string? RequiredConfirmationText { get; set; }

    /// <summary>
    /// The text the user has entered
    /// </summary>
    public string? EnteredConfirmationText { get; set; }

    #endregion

    #region Validation

    public override IEnumerable<string> Validate()
    {
        if (!string.IsNullOrEmpty(RequiredConfirmationText) && 
            !string.Equals(RequiredConfirmationText, EnteredConfirmationText, StringComparison.OrdinalIgnoreCase))
        {
            yield return $"Please type '{RequiredConfirmationText}' to confirm";
        }
    }

    #endregion

    #region Factory Methods

    public static ConfirmationDialogParameters ForDelete(string itemName)
    {
        return new ConfirmationDialogParameters
        {
            Title = "Delete Confirmation",
            Message = $"Are you sure you want to delete '{itemName}'? This action cannot be undone.",
            ConfirmButtonText = "Delete",
            IsDangerous = true
        };
    }

    public static ConfirmationDialogParameters ForCancel(string operationName)
    {
        return new ConfirmationDialogParameters
        {
            Title = "Cancel Confirmation",
            Message = $"Are you sure you want to cancel {operationName}? Any unsaved changes will be lost.",
            ConfirmButtonText = "Yes, Cancel",
            CancelButtonText = "No, Continue"
        };
    }

    #endregion
}

/// <summary>
/// Parameters for selecting a date range
/// </summary>
public class DateRangeDialogParameters : DialogParametersBase
{
    #region Properties

    public DateTime StartDate { get; set; } = DateTime.Today.AddMonths(-1);
    public DateTime EndDate { get; set; } = DateTime.Today;
    public string? Title { get; set; } = "Select Date Range";

    /// <summary>
    /// Minimum allowed start date
    /// </summary>
    public DateTime? MinDate { get; set; }

    /// <summary>
    /// Maximum allowed end date
    /// </summary>
    public DateTime? MaxDate { get; set; }

    #endregion

    #region Validation

    public override IEnumerable<string> Validate()
    {
        if (EndDate < StartDate)
        {
            yield return "End date must be after start date";
        }

        if (MinDate.HasValue && StartDate < MinDate.Value)
        {
            yield return $"Start date cannot be before {MinDate.Value:d}";
        }

        if (MaxDate.HasValue && EndDate > MaxDate.Value)
        {
            yield return $"End date cannot be after {MaxDate.Value:d}";
        }
    }

    #endregion

    #region Factory Methods

    public static DateRangeDialogParameters ForCurrentMonth()
    {
        var today = DateTime.Today;
        return new DateRangeDialogParameters
        {
            StartDate = new DateTime(today.Year, today.Month, 1),
            EndDate = today
        };
    }

    public static DateRangeDialogParameters ForLastMonth()
    {
        var today = DateTime.Today;
        var firstOfMonth = new DateTime(today.Year, today.Month, 1);
        var lastMonth = firstOfMonth.AddMonths(-1);
        return new DateRangeDialogParameters
        {
            StartDate = lastMonth,
            EndDate = firstOfMonth.AddDays(-1)
        };
    }

    #endregion
}

/// <summary>
/// Parameters for selecting billing week
/// </summary>
public class WeekSelectionDialogParameters : DialogParametersBase
{
    #region Properties

    [Required]
    public int Year { get; set; } = DateTime.Today.Year;

    [Required]
    [Range(1, 53, ErrorMessage = "Week must be between 1 and 53")]
    public int WeekNumber { get; set; }

    public string? Title { get; set; } = "Select Week";

    #endregion

    #region Computed Properties

    public string WeekDisplay => $"Week {WeekNumber}, {Year}";

    #endregion

    #region Validation

    public override IEnumerable<string> Validate()
    {
        if (WeekNumber < 1 || WeekNumber > 53)
        {
            yield return "Week number must be between 1 and 53";
        }

        if (Year < 2020 || Year > 2050)
        {
            yield return "Year must be between 2020 and 2050";
        }
    }

    #endregion

    #region Factory Methods

    public static WeekSelectionDialogParameters ForCurrentWeek()
    {
        var today = DateTime.Today;
        return new WeekSelectionDialogParameters
        {
            Year = today.Year,
            WeekNumber = Helpers.DateTimeHelpers.GetIso8601WeekOfYear(today)
        };
    }

    #endregion
}

/// <summary>
/// Parameters for processing billing with customer selection
/// </summary>
public class BillingProcessDialogParameters : DialogParametersBase
{
    #region Properties

    [Required]
    public DateTime InvoiceDate { get; set; } = DateTime.Today;

    public List<string> SelectedCustomerIds { get; set; } = new();

    public decimal TotalAmount { get; set; }

    public int CustomerCount => SelectedCustomerIds.Count;

    #endregion

    #region Validation

    public override IEnumerable<string> Validate()
    {
        if (InvoiceDate == default)
        {
            yield return "Invoice date is required";
        }

        if (!SelectedCustomerIds.Any())
        {
            yield return "At least one customer must be selected";
        }
    }

    #endregion
}

/// <summary>
/// Parameters for project billing
/// </summary>
public class ProjectBillingDialogParameters : DialogParametersBase
{
    #region Properties

    [Required]
    public string CustomerId { get; set; } = string.Empty;

    [Required]
    public DateTime InvoiceDate { get; set; } = DateTime.Today;

    public List<ProjectBillingLine> Projects { get; set; } = new();

    #endregion

    #region Computed Properties

    public decimal TotalAmount => Projects.Sum(p => p.Amount);

    #endregion

    #region Validation

    public override IEnumerable<string> Validate()
    {
        if (string.IsNullOrEmpty(CustomerId))
        {
            yield return "Customer is required";
        }

        if (InvoiceDate == default)
        {
            yield return "Invoice date is required";
        }

        if (!Projects.Any(p => p.HoursToBill > 0))
        {
            yield return "At least one project must have hours to bill";
        }
    }

    #endregion

    #region Nested Types

    public class ProjectBillingLine
    {
        public string ProjectCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal AvailableHours { get; set; }
        public decimal HoursToBill { get; set; }
        public decimal Rate { get; set; }
        public decimal Amount => Math.Round(HoursToBill * Rate, 2);
    }

    #endregion
}

/// <summary>
/// Parameters for adding/editing time entry
/// </summary>
public class TimeEntryDialogParameters : DialogParametersBase
{
    #region Properties

    public string? Id { get; set; }

    public bool IsEditing => !string.IsNullOrEmpty(Id);

    [Required]
    public string EmployeeId { get; set; } = string.Empty;

    [Required]
    public DateTime Date { get; set; } = DateTime.Today;

    public TimeSpan StartTime { get; set; } = new TimeSpan(9, 0, 0);

    public TimeSpan EndTime { get; set; } = new TimeSpan(17, 0, 0);

    [Range(0.25, 24, ErrorMessage = "Hours must be between 0.25 and 24")]
    public decimal Hours { get; set; } = 8;

    [Required]
    public string ProjectCode { get; set; } = string.Empty;

    public bool Billable { get; set; } = true;

    public string? Comments { get; set; }

    #endregion

    #region Validation

    public override IEnumerable<string> Validate()
    {
        if (string.IsNullOrEmpty(EmployeeId))
        {
            yield return "Employee is required";
        }

        if (string.IsNullOrEmpty(ProjectCode))
        {
            yield return "Project is required";
        }

        if (Hours <= 0)
        {
            yield return "Hours must be greater than 0";
        }

        if (EndTime <= StartTime)
        {
            yield return "End time must be after start time";
        }
    }

    #endregion

    #region Methods

    /// <summary>
    /// Calculates hours based on start and end time
    /// </summary>
    public void CalculateHours()
    {
        if (EndTime > StartTime)
        {
            var duration = EndTime - StartTime;
            Hours = Math.Round((decimal)duration.TotalHours, 2);
        }
    }

    #endregion
}
