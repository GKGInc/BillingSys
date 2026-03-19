namespace BillingSys.Shared.DTOs;

public class CreateTimeEntryRequest
{
    public string EmployeeId { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public decimal Hours { get; set; }
    public bool Billable { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public int Miles { get; set; }
    public string? Comments { get; set; }
}

public class UpdateTimeEntryRequest
{
    public string Id { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public decimal Hours { get; set; }
    public bool Billable { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public int Miles { get; set; }
    public string? Comments { get; set; }
}

public class TimeEntryFilterRequest
{
    public string? EmployeeId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? WeekNumber { get; set; }
    public int? Year { get; set; }
    public string? ProjectCode { get; set; }
    public bool? Billable { get; set; }
    public string? Status { get; set; }
}

public class WeeklyHoursSummary
{
    public string EmployeeId { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public int WeekNumber { get; set; }
    public int Year { get; set; }
    public decimal TotalHours { get; set; }
    public decimal BillableHours { get; set; }
    public decimal VacationHours { get; set; }
    public decimal HolidayHours { get; set; }
    public decimal SickHours { get; set; }
    public decimal PersonalHours { get; set; }
    public decimal WorkHours => TotalHours - VacationHours - HolidayHours - SickHours - PersonalHours;
    public decimal BillablePercentage => WorkHours > 0 ? Math.Round((BillableHours / WorkHours) * 100, 1) : 0;
}
