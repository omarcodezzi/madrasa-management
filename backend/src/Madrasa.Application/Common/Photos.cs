using System.Text.RegularExpressions;

namespace Madrasa.Application.Common;

/// <summary>Validates a data-URL photo (JPG/PNG, at most 2 MB) and stores it.</summary>
public static partial class Photos
{
    public static async Task<string> SaveAsync(IFileStorage files, string folder, string code, string dataUrl, CancellationToken ct)
    {
        var m = DataUrlRx().Match(dataUrl);
        if (!m.Success) throw AppException.Invalid(new Dictionary<string, string> { ["photo"] = "ছবিটি JPG বা PNG হতে হবে" });
        var bytes = Convert.FromBase64String(m.Groups[2].Value);
        if (bytes.Length > 2 * 1024 * 1024) throw AppException.Invalid(new Dictionary<string, string> { ["photo"] = "ছবিটি ২ MB-এর বেশি বড়" });
        var ext = m.Groups[1].Value == "png" ? "png" : "jpg";
        return await files.SaveAsync(folder, $"{code}-{Guid.NewGuid():N}.{ext}", bytes, ct);
    }

    [GeneratedRegex(@"^data:image/(jpeg|jpg|png);base64,([A-Za-z0-9+/=]+)$")]
    private static partial Regex DataUrlRx();
}
