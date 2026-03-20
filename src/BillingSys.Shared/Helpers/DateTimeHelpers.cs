using System.Globalization;

namespace BillingSys.Shared.Helpers;

/// <summary>
/// Helper methods for date and time operations
/// </summary>
public static class DateTimeHelpers
{
    #region Week Number Calculations

    /// <summary>
    /// Gets the ISO 8601 week number for a given date
    /// </summary>
    public static int GetIso8601WeekOfYear(DateTime date)
    {
        var day = CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(date);
        if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
        {
            date = date.AddDays(3);
        }
        return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
            date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
    }

    /// <summary>
    /// Gets the first day (Monday) of a given ISO week
    /// </summary>
    public static DateTime GetFirstDayOfWeek(int year, int weekNumber)
    {
        var jan1 = new DateTime(year, 1, 1);
        var daysOffset = DayOfWeek.Thursday - jan1.DayOfWeek;
        var firstThursday = jan1.AddDays(daysOffset);
        var firstWeek = GetIso8601WeekOfYear(firstThursday);

        var weekNum = weekNumber;
        if (firstWeek == 1)
        {
            weekNum -= 1;
        }

        var result = firstThursday.AddDays(weekNum * 7);
        return result.AddDays(-3);
    }

    /// <summary>
    /// Gets the last day (Sunday) of a given ISO week
    /// </summary>
    public static DateTime GetLastDayOfWeek(int year, int weekNumber)
    {
        return GetFirstDayOfWeek(year, weekNumber).AddDays(6);
    }

    /// <summary>
    /// Gets a display string for a week (e.g., "Week 12, 2026")
    /// </summary>
    public static string GetWeekDisplayString(int year, int weekNumber)
    {
        return $"Week {weekNumber}, {year}";
    }

    /// <summary>
    /// Gets a date range display string for a week (e.g., "Mar 16 - Mar 22, 2026")
    /// </summary>
    public static string GetWeekDateRangeString(int year, int weekNumber)
    {
        var firstDay = GetFirstDayOfWeek(year, weekNumber);
        var lastDay = GetLastDayOfWeek(year, weekNumber);
        
        if (firstDay.Month == lastDay.Month)
        {
            return $"{firstDay:MMM d} - {lastDay:d}, {lastDay.Year}";
        }
        return $"{firstDay:MMM d} - {lastDay:MMM d}, {lastDay.Year}";
    }

    #endregion

    #region Billing Period Helpers

    /// <summary>
    /// Gets the current billing period (year and month)
    /// </summary>
    public static (int Year, int Month) GetCurrentBillingPeriod()
    {
        var now = DateTime.UtcNow;
        return (now.Year, now.Month);
    }

    /// <summary>
    /// Gets the partition key for invoices based on date
    /// </summary>
    public static string GetInvoicePartitionKey(DateTime invoiceDate)
    {
        return $"{invoiceDate.Year}-{invoiceDate.Month:D2}";
    }

    /// <summary>
    /// Gets the partition key for time entries based on date
    /// </summary>
    public static string GetTimeEntryPartitionKey(DateTime date)
    {
        var year = date.Year;
        var week = GetIso8601WeekOfYear(date);
        return $"{year}-{week:D2}";
    }

    #endregion
}
