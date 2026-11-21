using Microsoft.EntityFrameworkCore;

namespace Madrasa.Application.Common;

public static class Lookups
{
    public static async Task<int> CurrentSessionIdAsync(this IAppDbContext db, CancellationToken ct) =>
        await db.AcademicSessions.Where(s => s.IsCurrent).Select(s => s.Id).FirstOrDefaultAsync(ct) is var id and > 0
            ? id
            : throw new AppException("কোনো চলতি শিক্ষাবর্ষ ঠিক করা নেই। সেটিংস থেকে শিক্ষাবর্ষ চালু করুন।");

    /// <summary>Weekly holiday (0=Sunday … 5=Friday, .NET DayOfWeek) from settings; Friday by default.</summary>
    public static async Task<DayOfWeek> WeeklyHolidayAsync(this IAppDbContext db, CancellationToken ct)
    {
        var v = await db.AppSettings.Where(s => s.Key == "weeklyHoliday").Select(s => s.Value).FirstOrDefaultAsync(ct);
        return int.TryParse(v, out var d) && d is >= 0 and <= 6 ? (DayOfWeek)d : DayOfWeek.Friday;
    }

    /// <summary>School days in [from, to]: excludes the weekly holiday and listed holidays.</summary>
    public static async Task<List<DateOnly>> WorkDaysAsync(this IAppDbContext db, DateOnly from, DateOnly to, CancellationToken ct)
    {
        var weekly = await db.WeeklyHolidayAsync(ct);
        var holidays = (await db.Holidays.Where(h => h.Date >= from && h.Date <= to).Select(h => h.Date).ToListAsync(ct)).ToHashSet();
        var days = new List<DateOnly>();
        for (var d = from; d <= to; d = d.AddDays(1))
            if (d.DayOfWeek != weekly && !holidays.Contains(d)) days.Add(d);
        return days;
    }
}
