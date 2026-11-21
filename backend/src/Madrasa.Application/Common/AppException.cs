namespace Madrasa.Application.Common;

/// <summary>Business-rule failure with a message safe to show the user (Bangla). Mapped to 400/404/409 by the API.</summary>
public class AppException(string message, int status = 400, IDictionary<string, string>? fieldErrors = null) : Exception(message)
{
    public int Status { get; } = status;
    public IDictionary<string, string>? FieldErrors { get; } = fieldErrors;

    public static AppException NotFound(string what) => new($"{what} পাওয়া যায়নি", 404);
    public static AppException Invalid(IDictionary<string, string> errors) => new("লাল চিহ্নিত ঘরগুলো ঠিক করুন", 400, errors);
}

public interface IClock
{
    /// <summary>Today in the madrasa's time zone (Asia/Dhaka by default).</summary>
    DateOnly Today { get; }
    TimeOnly Now { get; }
}

public interface IFileStorage
{
    /// <summary>Saves bytes and returns the public path, e.g. /uploads/students/RM-2026-001.jpg</summary>
    Task<string> SaveAsync(string folder, string fileName, byte[] data, CancellationToken ct = default);
}

public record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);

public static class Months
{
    public static string Of(DateOnly d) => $"{d.Year:0000}-{d.Month:00}";
    public static DateOnly First(string ym) => new(int.Parse(ym[..4]), int.Parse(ym[5..7]), 1);
    public static bool IsValid(string? ym) => ym is { Length: 7 } && ym[4] == '-' && int.TryParse(ym[..4], out _) && int.TryParse(ym[5..], out var m) && m is >= 1 and <= 12;
    /// <summary>Inclusive list of months from start to end.</summary>
    public static IEnumerable<string> Range(DateOnly start, DateOnly end)
    {
        for (var d = new DateOnly(start.Year, start.Month, 1); d <= end; d = d.AddMonths(1)) yield return Of(d);
    }
}
