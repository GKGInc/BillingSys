namespace BillingSys.Shared.Models;

public class TimeEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string EmployeeId { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public decimal Hours { get; set; }
    public bool Billable { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public int Miles { get; set; }
    public string? Comments { get; set; }
    public TimeEntryStatus Status { get; set; } = TimeEntryStatus.Pending;
    public string? InvoiceNumber { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public int WeekNumber => GetIso8601WeekOfYear(Date);
    public int Year => Date.Year;

    private static int GetIso8601WeekOfYear(DateTime date)
    {
        var day = System.Globalization.CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(date);
        if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
        {
            date = date.AddDays(3);
        }
        return System.Globalization.CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
            date, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
    }
}

public enum TimeEntryStatus
{
    Pending,
    Approved,
    Billed,
    Rejected
}
