namespace BillingSys.Functions.Services;

/// <summary>
/// Azure Table Storage (Azure.Data.Tables) requires DateTime values with a defined kind; Unspecified throws.
/// Use these helpers before persisting or when validating incoming JSON dates.
/// </summary>
public static class DateTimeUtc
{
    /// <summary>
    /// Normalizes a calendar date to UTC midnight for time-entry <see cref="BillingSys.Shared.Models.TimeEntry.Date" />.
    /// </summary>
    public static DateTime EnsureUtcDate(DateTime date)
    {
        return date.Kind switch
        {
            DateTimeKind.Utc => date,
            DateTimeKind.Local => DateTime.SpecifyKind(date.ToUniversalTime().Date, DateTimeKind.Utc),
            _ => DateTime.SpecifyKind(date.Date, DateTimeKind.Utc)
        };
    }

    /// <summary>
    /// Ensures any DateTime is UTC (e.g. audit fields) before Azure SDK calls.
    /// </summary>
    public static DateTime EnsureUtc(DateTime dt)
    {
        return dt.Kind switch
        {
            DateTimeKind.Utc => dt,
            DateTimeKind.Local => dt.ToUniversalTime(),
            _ => DateTime.SpecifyKind(dt, DateTimeKind.Utc)
        };
    }
}
