namespace BillingSys.Shared.Models;

public class TimeEntry : AuditableEntity
{
    #region Identity

    public string Id { get; set; } = Guid.NewGuid().ToString();

    #endregion

    #region Employee

    public string EmployeeId { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;

    #endregion

    #region Time Details

    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public decimal Hours { get; set; }

    #endregion

    #region Project

    public bool Billable { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public int Miles { get; set; }
    public string? Comments { get; set; }

    #endregion

    #region Status

    public TimeEntryStatus Status { get; set; } = TimeEntryStatus.Pending;
    public string? InvoiceNumber { get; set; }

    #endregion

    #region Computed Properties

    public int WeekNumber => GetIso8601WeekOfYear(Date);
    public int Year => Date.Year;

    #endregion

    #region Private Methods

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

    #endregion
}

public enum TimeEntryStatus
{
    Pending,
    Approved,
    Billed,
    Rejected
}
