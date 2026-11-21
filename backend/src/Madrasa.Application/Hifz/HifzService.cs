using Madrasa.Application.Common;
using Madrasa.Domain.Entities;
using Madrasa.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Madrasa.Application.Hifz;

public record HifzStudentRow(int StudentId, string StudentCode, string Name, string? PhotoPath, string ClassName, int Roll,
    int CompletedJuz, byte? CurrentJuz, bool LoggedToday, DateOnly? LastDate, byte? LastJuz, short? LastFrom, short? LastTo, string? LastText, double AvgMistakes);
public record HifzOverview(DateOnly Date, int Total, int LoggedToday, IReadOnlyList<HifzStudentRow> Students);
public record JuzCell(byte JuzNo, string Status, DateOnly? CompletedOn, int PagesDone);
public record HifzLogDto(long Id, DateOnly Date, byte? SabakJuz, short? SabakFromPage, short? SabakToPage, string? SabakText,
    string? SabqiText, string? ManzilText, int Mistakes, byte? Quality, string? Remarks);
public record WeekPoint(DateOnly WeekStart, int Pages, double AvgMistakes, int Days);
public record HifzStudentDetail(int StudentId, string StudentCode, string Name, string? PhotoPath, string? ClassName, int? Roll,
    int CompletedJuz, IReadOnlyList<JuzCell> Juz, IReadOnlyList<HifzLogDto> Logs, IReadOnlyList<WeekPoint> Weekly, HifzLogDto? Today);
public record HifzLogInput(DateOnly? Date, byte? SabakJuz, short? SabakFromPage, short? SabakToPage, string? SabakText,
    string? SabqiText, string? ManzilText, int Mistakes, byte? Quality, string? Remarks);
/// <summary>Status: "Completed", "InProgress" or null to clear.</summary>
public record SetJuzRequest(byte JuzNo, string? Status, DateOnly? CompletedOn);

/// <summary>Daily sabak / sabqi / manzil register and the 30-juz map.</summary>
public class HifzService(IAppDbContext db, IClock clock)
{
    /// <summary>Pages in one juz of the common 15-line Bangladeshi mushaf.</summary>
    public const int PagesPerJuz = 20;

    public async Task<HifzOverview> OverviewAsync(DateOnly? date, CancellationToken ct)
    {
        var d = date ?? clock.Today;
        var sessionId = await db.CurrentSessionIdAsync(ct);
        var since = d.AddDays(-6);
        var rows = await db.Enrollments.AsNoTracking()
            .Where(e => e.SessionId == sessionId && e.Class!.Department!.Kind == DepartmentKind.Hifz && !e.Student!.IsDeleted && e.Student.Status == StudentStatus.Active)
            .OrderBy(e => e.Class!.SortOrder).ThenBy(e => e.Roll)
            .Select(e => new
            {
                e.StudentId, e.Student!.StudentCode, e.Student.Name, e.Student.PhotoPath, ClassName = e.Class!.NameBn, e.Roll,
                Completed = db.HifzJuzProgress.Count(j => j.StudentId == e.StudentId && j.Status == JuzStatus.Completed),
                Current = db.HifzJuzProgress.Where(j => j.StudentId == e.StudentId && j.Status == JuzStatus.InProgress).OrderByDescending(j => j.JuzNo).Select(j => (byte?)j.JuzNo).FirstOrDefault(),
                Today = db.HifzDailyLogs.Any(l => l.StudentId == e.StudentId && l.Date == d),
                Last = db.HifzDailyLogs.Where(l => l.StudentId == e.StudentId && l.Date <= d).OrderByDescending(l => l.Date)
                    .Select(l => new { l.Date, l.SabakJuz, l.SabakFromPage, l.SabakToPage, l.SabakText }).FirstOrDefault(),
                Mist = db.HifzDailyLogs.Where(l => l.StudentId == e.StudentId && l.Date >= since && l.Date <= d).Average(l => (double?)l.Mistakes),
            }).ToListAsync(ct);
        var list = rows.Select(r => new HifzStudentRow(r.StudentId, r.StudentCode, r.Name, r.PhotoPath, r.ClassName, r.Roll, r.Completed, r.Current, r.Today,
            r.Last?.Date, r.Last?.SabakJuz, r.Last?.SabakFromPage, r.Last?.SabakToPage, r.Last?.SabakText,
            Math.Round(r.Mist ?? 0, 1))).ToList();
        return new HifzOverview(d, list.Count, list.Count(x => x.LoggedToday), list);
    }

    public async Task<HifzStudentDetail> StudentAsync(int studentId, CancellationToken ct)
    {
        var sessionId = await db.CurrentSessionIdAsync(ct);
        var s = await db.Students.AsNoTracking().Where(x => x.Id == studentId)
            .Select(x => new { x.Id, x.StudentCode, x.Name, x.PhotoPath,
                E = x.Enrollments.Where(e => e.SessionId == sessionId).Select(e => new { e.Class!.NameBn, e.Roll }).FirstOrDefault() })
            .FirstOrDefaultAsync(ct) ?? throw AppException.NotFound("শিক্ষার্থী");
        var juz = await db.HifzJuzProgress.AsNoTracking().Where(j => j.StudentId == studentId).ToListAsync(ct);
        var logs = await db.HifzDailyLogs.AsNoTracking().Where(l => l.StudentId == studentId).OrderByDescending(l => l.Date).Take(120).ToListAsync(ct);
        // Furthest page reached in each juz, for the partial fill of in-progress cells
        var pages = logs.Where(l => l.SabakJuz != null && l.SabakToPage != null).GroupBy(l => l.SabakJuz!.Value)
            .ToDictionary(g => g.Key, g => (int)g.Max(l => l.SabakToPage!.Value));
        var cells = Enumerable.Range(1, 30).Select(n =>
        {
            var j = juz.FirstOrDefault(x => x.JuzNo == n);
            var status = j?.Status switch { JuzStatus.Completed => "Completed", JuzStatus.InProgress => "InProgress", _ => "None" };
            var done = status == "Completed" ? PagesPerJuz : Math.Min(PagesPerJuz, pages.GetValueOrDefault((byte)n));
            return new JuzCell((byte)n, status, j?.CompletedOn, done);
        }).ToList();

        var today = clock.Today;
        var weekStart = today.AddDays(-(((int)today.DayOfWeek + 1) % 7));   // week starts Saturday
        var weekly = Enumerable.Range(0, 8).Select(i => weekStart.AddDays(-7 * (7 - i))).Select(ws =>
        {
            var w = logs.Where(l => l.Date >= ws && l.Date < ws.AddDays(7)).ToList();
            return new WeekPoint(ws, w.Sum(PagesOf), w.Count == 0 ? 0 : Math.Round(w.Average(l => l.Mistakes), 1), w.Count);
        }).ToList();

        return new HifzStudentDetail(s.Id, s.StudentCode, s.Name, s.PhotoPath, s.E?.NameBn, s.E?.Roll, cells.Count(c => c.Status == "Completed"),
            cells, logs.Take(30).Select(ToDto).ToList(), weekly, logs.Where(l => l.Date == today).Select(ToDto).FirstOrDefault());
    }

    public async Task<HifzLogDto> SaveLogAsync(int studentId, HifzLogInput input, CancellationToken ct)
    {
        if (!await db.Students.AnyAsync(s => s.Id == studentId, ct)) throw AppException.NotFound("শিক্ষার্থী");
        var d = input.Date ?? clock.Today;
        var err = new Dictionary<string, string>();
        if (d > clock.Today) err["date"] = "ভবিষ্যতের তারিখে সবক লেখা যাবে না";
        if (input.SabakJuz is < 1 or > 30) err["sabakJuz"] = "পারা ১ থেকে ৩০ এর মধ্যে দিন";
        if (input.SabakFromPage is < 1 or > PagesPerJuz) err["sabakFromPage"] = $"পৃষ্ঠা ১ থেকে {Digits.ToBangla(PagesPerJuz)} এর মধ্যে দিন";
        if (input.SabakToPage is < 1 or > PagesPerJuz) err["sabakToPage"] = $"পৃষ্ঠা ১ থেকে {Digits.ToBangla(PagesPerJuz)} এর মধ্যে দিন";
        if (input.SabakFromPage is short f && input.SabakToPage is short t && t < f) err["sabakToPage"] = "শেষ পৃষ্ঠা শুরুর পৃষ্ঠার আগে হতে পারে না";
        if ((input.SabakFromPage != null || input.SabakToPage != null) && input.SabakJuz is null) err["sabakJuz"] = "কোন পারার সবক তা দিন";
        if (input.Mistakes is < 0 or > 200) err["mistakes"] = "ভুলের সংখ্যা ঠিক নেই";
        if (input.Quality is < 1 or > 5) err["quality"] = "মান ১ থেকে ৫ এর মধ্যে দিন";
        bool empty = input.SabakJuz is null && string.IsNullOrWhiteSpace(input.SabakText) && string.IsNullOrWhiteSpace(input.SabqiText) && string.IsNullOrWhiteSpace(input.ManzilText);
        if (empty) err["sabakJuz"] = "সবক, সবকি বা মানযিলের অন্তত একটি লিখুন";
        if (err.Count > 0) throw AppException.Invalid(err);

        var log = await db.HifzDailyLogs.FirstOrDefaultAsync(l => l.StudentId == studentId && l.Date == d, ct);
        if (log is null) { log = new HifzDailyLog { StudentId = studentId, Date = d }; db.HifzDailyLogs.Add(log); }
        log.SabakJuz = input.SabakJuz; log.SabakFromPage = input.SabakFromPage; log.SabakToPage = input.SabakToPage;
        log.SabakText = Blank(input.SabakText); log.SabqiText = Blank(input.SabqiText); log.ManzilText = Blank(input.ManzilText);
        log.Mistakes = input.Mistakes; log.Quality = input.Quality; log.Remarks = Blank(input.Remarks);

        // A sabak in a juz means that juz is being memorised now
        if (input.SabakJuz is byte jn)
        {
            var j = await db.HifzJuzProgress.FirstOrDefaultAsync(x => x.StudentId == studentId && x.JuzNo == jn, ct);
            if (j is null) db.HifzJuzProgress.Add(new HifzJuzProgress { StudentId = studentId, JuzNo = jn, Status = JuzStatus.InProgress });
        }
        await db.SaveChangesAsync(ct);
        return ToDto(log);
    }

    public async Task DeleteLogAsync(int studentId, long id, CancellationToken ct)
    {
        var log = await db.HifzDailyLogs.FirstOrDefaultAsync(l => l.Id == id && l.StudentId == studentId, ct) ?? throw AppException.NotFound("সবক");
        db.HifzDailyLogs.Remove(log);
        await db.SaveChangesAsync(ct);
    }

    public async Task SetJuzAsync(int studentId, SetJuzRequest req, CancellationToken ct)
    {
        if (req.JuzNo is < 1 or > 30) throw AppException.Invalid(new Dictionary<string, string> { ["juzNo"] = "পারা ১ থেকে ৩০ এর মধ্যে" });
        if (!await db.Students.AnyAsync(s => s.Id == studentId, ct)) throw AppException.NotFound("শিক্ষার্থী");
        var j = await db.HifzJuzProgress.FirstOrDefaultAsync(x => x.StudentId == studentId && x.JuzNo == req.JuzNo, ct);
        if (req.Status is null) { if (j != null) db.HifzJuzProgress.Remove(j); }
        else
        {
            var st = req.Status == "Completed" ? JuzStatus.Completed : JuzStatus.InProgress;
            if (j is null) { j = new HifzJuzProgress { StudentId = studentId, JuzNo = req.JuzNo }; db.HifzJuzProgress.Add(j); }
            j.Status = st;
            j.CompletedOn = st == JuzStatus.Completed ? (req.CompletedOn ?? j.CompletedOn ?? clock.Today) : null;
        }
        await db.SaveChangesAsync(ct);
    }

    private static int PagesOf(HifzDailyLog l) => l.SabakFromPage is short f && l.SabakToPage is short t ? t - f + 1 : 0;
    private static HifzLogDto ToDto(HifzDailyLog l) => new(l.Id, l.Date, l.SabakJuz, l.SabakFromPage, l.SabakToPage, l.SabakText, l.SabqiText, l.ManzilText, l.Mistakes, l.Quality, l.Remarks);
    private static string? Blank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
