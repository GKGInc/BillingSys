using System.Text.Json;

namespace BillingSys.Functions.Infrastructure;

/// <summary>
/// Shared JSON settings for manual <see cref="JsonSerializer.Deserialize"/> in HTTP triggers (must match
/// <c>Configure&lt;JsonOptions&gt;</c> in Program.cs for ASP.NET Core integration).
/// </summary>
public static class FunctionsJsonSerializerOptions
{
    public static JsonSerializerOptions Create()
    {
        var o = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };
        o.Converters.Add(new UtcDateTimeConverter());
        o.Converters.Add(new UtcNullableDateTimeConverter());
        return o;
    }

    public static readonly JsonSerializerOptions Default = Create();
}
