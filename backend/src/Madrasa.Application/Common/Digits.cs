namespace Madrasa.Application.Common;

/// <summary>Users type Bangla digits (০১৭…) as often as English ones; store English.</summary>
public static class Digits
{
    private const string Bn = "০১২৩৪৫৬৭৮৯";

    public static string? ToEnglish(string? s) =>
        s is null ? null : new string(s.Select(c => Bn.IndexOf(c) is var i and >= 0 ? (char)('0' + i) : c).ToArray());

    /// <summary>English digits → Bangla, for numbers inside user-facing messages.</summary>
    public static string ToBangla(object? v) => string.Concat((Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture) ?? "").Select(c => c is >= '0' and <= '9' ? (char)('০' + (c - '0')) : c));
    /// <summary>Taka in a message: 91500 → "৯১,৫০০" (Bangladeshi grouping).</summary>
    public static string Money(decimal v) => ToBangla(v.ToString(v % 1 == 0 ? "#,##,##0" : "#,##,##0.##", new System.Globalization.CultureInfo("en-IN")));
    public static string PhoneDigits(string? s) => new((ToEnglish(s) ?? "").Where(char.IsAsciiDigit).ToArray());

    /// <summary>Bangladeshi mobile: 01XXXXXXXXX (11 digits), optional +88 / 88 prefix.</summary>
    public static bool IsBdMobile(string? s)
    {
        var d = PhoneDigits(s);
        if (d.StartsWith("88")) d = d[2..];
        return d.Length == 11 && d.StartsWith("01") && d[2] is >= '3' and <= '9';
    }
}
