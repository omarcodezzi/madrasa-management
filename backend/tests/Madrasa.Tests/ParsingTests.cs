using Madrasa.Application.Attendance;
using Madrasa.Application.Common;

namespace Madrasa.Tests;

public class ParsingTests
{
    [Theory]
    [InlineData("{\"type\":\"RMTC_STUDENT\",\"id\":\"RM-2026-001\"}", "RM-2026-001")]   // printed ID cards
    [InlineData("  rm-2026-007 ", "RM-2026-007")]                                       // typed by hand
    [InlineData("RM-২০২৬-০১২", "RM-2026-012")]                                          // Bangla digits
    [InlineData("{\"type\":\"OTHER\",\"id\":\"X\"}", null)]
    [InlineData("{not json", null)]
    [InlineData("", null)]
    public void Qr_payload_is_parsed(string raw, string? expected) => Assert.Equal(expected, AttendanceService.ParseCode(raw));

    [Theory]
    [InlineData("01711000010", true)]
    [InlineData("০১৭১১-০০০০১০", true)]
    [InlineData("+8801811223344", true)]
    [InlineData("01211000010", false)]
    [InlineData("0171100001", false)]
    [InlineData(null, false)]
    public void Bangladeshi_mobile_numbers(string? phone, bool ok) => Assert.Equal(ok, Digits.IsBdMobile(phone));

    [Fact]
    public void Month_range_is_inclusive()
    {
        var m = Months.Range(new DateOnly(2026, 10, 15), new DateOnly(2027, 1, 1)).ToList();
        Assert.Equal(new[] { "2026-10", "2026-11", "2026-12", "2027-01" }, m);
    }
}
