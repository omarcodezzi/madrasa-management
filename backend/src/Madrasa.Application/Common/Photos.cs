using System.Text.RegularExpressions;

namespace Madrasa.Application.Common;

/// <summary>Validates a data-URL photo (JPG/PNG, at most 2 MB) and stores it.</summary>
public static partial class Photos
{
    public static async Task<string> SaveAsync(IFileStorage files, string folder, string code, string dataUrl, CancellationToken ct)
    {
        var m = DataUrlRx().Match(dataUrl);
        if (!m.Success) throw AppException.Invalid(new Dictionary<string, string> { ["photo"] = "ছবিটি JPG বা PNG হতে হবে" });
        byte[] bytes;
        try { bytes = Convert.FromBase64String(m.Groups[2].Value); }
        catch (FormatException) { throw AppException.Invalid(new Dictionary<string, string> { ["photo"] = "ছবিটি ঠিকমতো পড়া যায়নি" }); }
        if (bytes.Length > 2 * 1024 * 1024) throw AppException.Invalid(new Dictionary<string, string> { ["photo"] = "ছবিটি ২ MB-এর বেশি বড়" });
        // Trust the file's own signature, not the label in the data URL
        var ext = IsPng(bytes) ? "png" : IsJpeg(bytes) ? "jpg" : null;
        if (ext is null) throw AppException.Invalid(new Dictionary<string, string> { ["photo"] = "ছবিটি JPG বা PNG হতে হবে" });
        return await files.SaveAsync(folder, $"{code}-{Guid.NewGuid():N}.{ext}", bytes, ct);
    }

    private static bool IsPng(byte[] b) => b.Length > 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47;
    private static bool IsJpeg(byte[] b) => b.Length > 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF;

    [GeneratedRegex(@"^data:image/(jpeg|jpg|png);base64,([A-Za-z0-9+/=]+)$")]
    private static partial Regex DataUrlRx();
}
