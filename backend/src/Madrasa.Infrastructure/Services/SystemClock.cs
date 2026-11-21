using Madrasa.Application.Common;
using Microsoft.Extensions.Configuration;

namespace Madrasa.Infrastructure.Services;

/// <summary>"Today" in the madrasa's time zone (config TimeZone, default Asia/Dhaka), not the server's.</summary>
public class SystemClock(IConfiguration cfg, TimeProvider time) : IClock
{
    private readonly TimeZoneInfo _tz = TimeZoneInfo.FindSystemTimeZoneById(cfg["TimeZone"] ?? "Asia/Dhaka");
    private DateTime Local => TimeZoneInfo.ConvertTime(time.GetUtcNow(), _tz).DateTime;
    public DateOnly Today => DateOnly.FromDateTime(Local);
    public DateTime ToLocal(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), _tz);
    public TimeOnly Now { get { var l = Local; return new TimeOnly(l.Hour, l.Minute, l.Second); } }
}

/// <summary>Stores uploads under wwwroot/uploads (config Uploads:Root). Swap for blob storage in production if needed.</summary>
public class LocalFileStorage(IConfiguration cfg) : IFileStorage
{
    public async Task<string> SaveAsync(string folder, string fileName, byte[] data, CancellationToken ct = default)
    {
        var root = cfg["Uploads:Root"] ?? Path.Combine(AppContext.BaseDirectory, "wwwroot", "uploads");
        var safeFolder = string.Concat(folder.Where(char.IsLetterOrDigit));
        var safeName = string.Concat(fileName.Where(c => char.IsLetterOrDigit(c) || c is '-' or '.' or '_'));
        var dir = Path.Combine(root, safeFolder);
        Directory.CreateDirectory(dir);
        await File.WriteAllBytesAsync(Path.Combine(dir, safeName), data, ct);
        return $"/uploads/{safeFolder}/{safeName}";
    }
}
