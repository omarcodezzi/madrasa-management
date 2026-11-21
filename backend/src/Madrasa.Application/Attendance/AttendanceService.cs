using System.Text.Json;
using Madrasa.Application.Common;
using Madrasa.Domain.Entities;
using Madrasa.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Madrasa.Application.Attendance;

public record AttendanceRow(int StudentId, string StudentCode, string Name, string? PhotoPath, int ClassId, string ClassName, int Roll,
    AttendanceStatus? Status, TimeOnly? CheckIn, AttendanceMethod? Method);
public record ClassTally(int ClassId, string ClassName, int Total, int Present, int Late, int Absent, int Leave);
public record AttendanceDay(DateOnly Date, bool IsWorkDay, string? HolidayTitle, IReadOnlyList<AttendanceRow> Rows, IReadOnlyList<ClassTally> Classes);
public record MarkEntry(int StudentId, AttendanceStatus? Status);
public record MarkRequest(DateOnly Date, IReadOnlyList<MarkEntry> Entries);
public record ScanRequest(string Code, DateOnly? Date);
public record ScanResult(bool Ok, bool AlreadyMarked, string Message, int? StudentId, string? Name, string? ClassName, int? Roll, string? PhotoPath, TimeOnly? CheckIn);
public record DayMark(DateOnly Date, AttendanceStatus? Status, TimeOnly? CheckIn, bool IsWorkDay);
public record StudentMonth(string Month, int WorkDays, int Present, int Late, int Absent, int Leave, double Percent, IReadOnlyList<DayMark> Days);

public class AttendanceService(IAppDbContext db, IClock clock, ICurrentUser user)
{
    /// <summary>Roll for one day; classId null = whole madrasa (used by the dashboard roll call).</summary>
    public async Task<AttendanceDay> DayAsync(DateOnly? date, int? classId, CancellationToken ct)
    {
        var d = date ?? clock.Today;
        var sessionId = await db.CurrentSessionIdAsync(ct);
        var holiday = await db.Holidays.Where(h => h.Date == d).Select(h => h.Title).FirstOrDefaultAsync(ct);
        var weekly = await db.WeeklyHolidayAsync(ct);
        var isWork = holiday is null && d.DayOfWeek != weekly;

        var q = db.Enrollments.AsNoTracking().Where(e => e.SessionId == sessionId && e.Student!.Status == StudentStatus.Active && !e.Student.IsDeleted);
        if (classId is not null) q = q.Where(e => e.ClassId == classId);
        var rows = await q.OrderBy(e => e.Class!.SortOrder).ThenBy(e => e.Roll)
            .Select(e => new
            {
                e.StudentId, e.Student!.StudentCode, e.Student.Name, e.Student.PhotoPath, e.ClassId, ClassName = e.Class!.NameBn, e.Roll,
                A = db.StudentAttendance.Where(a => a.StudentId == e.StudentId && a.Date == d).Select(a => new { a.Status, a.CheckIn, a.Method }).FirstOrDefault(),
            }).ToListAsync(ct);
        var list = rows.Select(r => new AttendanceRow(r.StudentId, r.StudentCode, r.Name, r.PhotoPath, r.ClassId, r.ClassName, r.Roll,
            r.A?.Status, r.A?.CheckIn, r.A?.Method)).ToList();
        var tallies = list.GroupBy(r => (r.ClassId, r.ClassName)).Select(g => new ClassTally(g.Key.ClassId, g.Key.ClassName, g.Count(),
            g.Count(r => r.Status == AttendanceStatus.Present), g.Count(r => r.Status == AttendanceStatus.Late),
            g.Count(r => r.Status == AttendanceStatus.Absent), g.Count(r => r.Status == AttendanceStatus.Leave))).ToList();
        return new AttendanceDay(d, isWork, holiday, list, tallies);
    }

    /// <summary>Upserts many marks at once (one-tap screen, "mark all present"). Status null clears a mark.</summary>
    public async Task<int> MarkAsync(MarkRequest req, CancellationToken ct)
    {
        if (req.Date > clock.Today) throw new AppException("ভবিষ্যতের তারিখে হাজিরা নেওয়া যায় না");
        var ids = req.Entries.Select(e => e.StudentId).Distinct().ToList();
        var existing = await db.StudentAttendance.Where(a => a.Date == req.Date && ids.Contains(a.StudentId)).ToDictionaryAsync(a => a.StudentId, ct);
        var valid = (await db.Students.Where(s => ids.Contains(s.Id)).Select(s => s.Id).ToListAsync(ct)).ToHashSet();
        var now = req.Date == clock.Today ? clock.Now : (TimeOnly?)null;
        var n = 0;
        foreach (var e in req.Entries.Where(e => valid.Contains(e.StudentId)))
        {
            existing.TryGetValue(e.StudentId, out var a);
            if (e.Status is null) { if (a is not null) { db.StudentAttendance.Remove(a); n++; } continue; }
            if (a is null)
            {
                a = new StudentAttendance { StudentId = e.StudentId, Date = req.Date, Method = AttendanceMethod.Manual };
                db.StudentAttendance.Add(a);
                existing[e.StudentId] = a;
            }
            if (a.Status != e.Status)
            {
                a.Status = e.Status.Value;
                if (e.Status is AttendanceStatus.Present or AttendanceStatus.Late) a.CheckIn ??= now; else a.CheckIn = null;
                a.MarkedBy = user.UserId;
                n++;
            }
        }
        await db.SaveChangesAsync(ct);
        return n;
    }

    /// <summary>
    /// QR from the ID card: {"type":"RMTC_STUDENT","id":"RM-2026-001"} (format kept from the prototype so printed cards keep working),
    /// or the bare student code typed by hand.
    /// </summary>
    public async Task<ScanResult> ScanAsync(ScanRequest req, CancellationToken ct)
    {
        var code = ParseCode(req.Code);
        if (code is null) return new ScanResult(false, false, "এই QR কোডটি এই মাদরাসার আইডি কার্ড নয়", null, null, null, null, null, null);
        var d = req.Date ?? clock.Today;
        if (d > clock.Today) throw new AppException("ভবিষ্যতের তারিখে হাজিরা নেওয়া যায় না");
        var sessionId = await db.CurrentSessionIdAsync(ct);
        var s = await db.Students.Where(x => x.StudentCode == code)
            .Select(x => new { x.Id, x.Name, x.PhotoPath, x.Status, E = x.Enrollments.Where(e => e.SessionId == sessionId).Select(e => new { e.Class!.NameBn, e.Roll }).FirstOrDefault() })
            .FirstOrDefaultAsync(ct);
        if (s is null) return new ScanResult(false, false, $"{code} আইডির কোনো শিক্ষার্থী পাওয়া যায়নি", null, null, null, null, null, null);
        if (s.Status != StudentStatus.Active) return new ScanResult(false, false, $"{s.Name} এখন সক্রিয় শিক্ষার্থী নন", s.Id, s.Name, s.E?.NameBn, s.E?.Roll, s.PhotoPath, null);

        var a = await db.StudentAttendance.FirstOrDefaultAsync(x => x.StudentId == s.Id && x.Date == d, ct);
        if (a is { Status: AttendanceStatus.Present or AttendanceStatus.Late })
            return new ScanResult(true, true, $"{s.Name} আগেই উপস্থিত", s.Id, s.Name, s.E?.NameBn, s.E?.Roll, s.PhotoPath, a.CheckIn);
        if (a is null) { a = new StudentAttendance { StudentId = s.Id, Date = d }; db.StudentAttendance.Add(a); }
        a.Status = AttendanceStatus.Present;
        a.Method = AttendanceMethod.Qr;
        a.CheckIn = d == clock.Today ? clock.Now : null;
        a.MarkedBy = user.UserId;
        await db.SaveChangesAsync(ct);
        return new ScanResult(true, false, $"{s.Name} উপস্থিত", s.Id, s.Name, s.E?.NameBn, s.E?.Roll, s.PhotoPath, a.CheckIn);
    }

    public static string? ParseCode(string? raw)
    {
        var t = raw?.Trim();
        if (string.IsNullOrEmpty(t)) return null;
        if (t.StartsWith('{'))
        {
            try
            {
                using var doc = JsonDocument.Parse(t);
                var r = doc.RootElement;
                return r.TryGetProperty("type", out var ty) && ty.GetString() == "RMTC_STUDENT" && r.TryGetProperty("id", out var id) ? id.GetString()?.Trim() : null;
            }
            catch (JsonException) { return null; }
        }
        return Digits.ToEnglish(t)!.ToUpperInvariant();
    }

    /// <summary>Calendar heatmap for one student and month.</summary>
    public async Task<StudentMonth> StudentMonthAsync(int studentId, string? month, CancellationToken ct)
    {
        var ym = Months.IsValid(month) ? month! : Months.Of(clock.Today);
        var from = Months.First(ym);
        var to = from.AddMonths(1).AddDays(-1);
        var lastCounted = to < clock.Today ? to : clock.Today;
        var work = (await db.WorkDaysAsync(from, to, ct)).ToHashSet();
        var marks = await db.StudentAttendance.AsNoTracking().Where(a => a.StudentId == studentId && a.Date >= from && a.Date <= to)
            .ToDictionaryAsync(a => a.Date, ct);
        var days = new List<DayMark>();
        for (var d = from; d <= to; d = d.AddDays(1))
        {
            marks.TryGetValue(d, out var m);
            days.Add(new DayMark(d, m?.Status, m?.CheckIn, work.Contains(d)));
        }
        var counted = days.Where(x => x.IsWorkDay && x.Date <= lastCounted).ToList();
        int present = counted.Count(x => x.Status == AttendanceStatus.Present), late = counted.Count(x => x.Status == AttendanceStatus.Late),
            leave = counted.Count(x => x.Status == AttendanceStatus.Leave);
        // A school day with no mark counts as absent once it has passed
        int absent = counted.Count(x => x.Status == AttendanceStatus.Absent || (x.Status is null && x.Date < clock.Today));
        var pct = counted.Count == 0 ? 0 : Math.Round((present + late) * 100.0 / counted.Count, 1);
        return new StudentMonth(ym, counted.Count, present, late, absent, leave, pct, days);
    }
}
