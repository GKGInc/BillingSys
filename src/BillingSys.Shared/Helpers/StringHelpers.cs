namespace BillingSys.Shared.Helpers;

/// <summary>
/// Helper methods for string operations
/// </summary>
public static class StringHelpers
{
    #region Formatting

    /// <summary>
    /// Formats a phone number for display
    /// </summary>
    public static string FormatPhoneNumber(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return string.Empty;

        var digits = new string(phone.Where(char.IsDigit).ToArray());
        
        return digits.Length switch
        {
            10 => $"({digits[..3]}) {digits[3..6]}-{digits[6..]}",
            11 when digits.StartsWith("1") => $"+1 ({digits[1..4]}) {digits[4..7]}-{digits[7..]}",
            _ => phone
        };
    }

    /// <summary>
    /// Formats currency for display
    /// </summary>
    public static string FormatCurrency(decimal amount, bool showCents = true)
    {
        return showCents ? amount.ToString("C2") : amount.ToString("C0");
    }

    /// <summary>
    /// Formats hours for display (e.g., "8.5 hrs")
    /// </summary>
    public static string FormatHours(decimal hours)
    {
        return hours == 1 ? "1 hr" : $"{hours:0.##} hrs";
    }

    #endregion

    #region Truncation

    /// <summary>
    /// Truncates a string to a maximum length with ellipsis
    /// </summary>
    public static string Truncate(string? value, int maxLength, string suffix = "...")
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (value.Length <= maxLength)
            return value;

        return value[..(maxLength - suffix.Length)] + suffix;
    }

    #endregion

    #region Normalization

    /// <summary>
    /// Normalizes a code or identifier (uppercase, trimmed)
    /// </summary>
    public static string NormalizeCode(string? code)
    {
        return code?.Trim().ToUpperInvariant() ?? string.Empty;
    }

    /// <summary>
    /// Creates a display name from a code (e.g., "JOHN_DOE" -> "John Doe")
    /// </summary>
    public static string CodeToDisplayName(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return string.Empty;

        var words = code.Replace('_', ' ').Replace('-', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", words.Select(w => 
            w.Length > 0 ? char.ToUpper(w[0]) + w[1..].ToLower() : w));
    }

    #endregion
}
