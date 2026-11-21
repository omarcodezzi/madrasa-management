using Madrasa.Application.Common;
using Madrasa.Application.Fees;
using Madrasa.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Madrasa.Application.Reports;

/// <summary>Kind: text, num, money, pct, date. The UI formats, prints and exports every report the same way.</summary>
public record ReportColumn(string Key, string Label, string Kind = "text");
public record ReportTable(string Key, string Title, string Subtitle, IReadOnlyList<ReportColumn> Columns, IReadOnlyList<Dictionary<string, object?>> Rows,
    Dictionary<string, object?>? Totals);
public record ReportQuery(string? Month, DateOnly? From, DateOnly? To, int? ClassId, int? FundId, int? ExamId);

public class ReportService(IAppDbContext db, IClock clock, FeeService fees)
{
    public Task<ReportTable> RunAsync(string key, ReportQuery q, CancellationToken ct) => key switch
    {
        "attendance" => AttendanceAsync(q, ct),
        "collection" => CollectionAsync(q, ct),
        "dues" => DuesAsync(q, ct),
        "results" => ResultsAsync(q, ct),
        "ledger" => LedgerAsync(q, ct),
        _ => throw AppException.NotFound("রিপোর্ট"),
    };

    private async Task<ReportTable> AttendanceAsync(ReportQuery q, CancellationToken ct)
    {
        var m = Months.IsValid(q.Month) ? q.Month! : Months.Of(clock.Today);
        var from = Months.First(m); var to = from.AddMonths(1).AddDays(-1); if (to > clock.Today) to = clock.Today;
        var work = from > clock.Today ? 0 : (await db.WorkDaysAsync(from, to, ct)).Count;
        var sessionId = await db.CurrentSessionIdAsync(ct);
        var en = db.Enrollments.AsNoTracking().Where(e => e.SessionId == sessionId && e.Student!.Status == StudentStatus.Active);
        if (q.ClassId is int c) en = en.Where(e => e.ClassId == c);
        var rows = await en.OrderBy(e => e.Class!.SortOrder).ThenBy(e => e.Roll)
            .Select(e => new { e.StudentId, e.Student!.StudentCode, e.Student.Name, Cls = e.Class!.NameBn, e.Roll,
                Marks = db.StudentAttendance.Where(a => a.StudentId == e.StudentId && a.Date >= from && a.Date <= to).GroupBy(a => a.Status).Select(g => new { g.Key, N = g.Count() }).ToList() })
            .ToListAsync(ct);
        var list = rows.Select(r =>
        {
            int N(AttendanceStatus s) => r.Marks.FirstOrDefault(x => x.Key == s)?.N ?? 0;
            var p = N(AttendanceStatus.Present); var l = N(AttendanceStatus.Late);
            return new Dictionary<string, object?> { ["code"] = r.StudentCode, ["name"] = r.Name, ["class"] = r.Cls, ["roll"] = r.Roll, ["work"] = work,
                ["present"] = p, ["late"] = l, ["absent"] = N(AttendanceStatus.Absent), ["leave"] = N(AttendanceStatus.Leave),
                ["pct"] = work == 0 ? null : Math.Round(100.0 * (p + l) / work, 1) };
        }).ToList();
        var className = q.ClassId is int cid ? await db.Classes.Where(x => x.Id == cid).Select(x => x.NameBn).FirstOrDefaultAsync(ct) : "সব শ্রেণি";
        return new ReportTable("attendance", "হাজিরা রিপোর্ট", $"{className} · {Months.Label(m)} · কর্মদিবস {Digits.ToBangla(work)}",
            [new("code", "আইডি"), new("name", "নাম"), new("class", "শ্রেণি"), new("roll", "রোল", "num"), new("present", "উপস্থিত", "num"), new("late", "দেরি", "num"),
             new("absent", "অনুপস্থিত", "num"), new("leave", "ছুটি", "num"), new("pct", "হার", "pct")],
            list, new() { ["name"] = $"মোট {Digits.ToBangla(list.Count)} জন",
                ["pct"] = list.Count(x => x["pct"] != null) is var k and > 0 ? Math.Round(list.Where(x => x["pct"] != null).Average(x => (double)x["pct"]!), 1) : null });
    }

    private async Task<ReportTable> CollectionAsync(ReportQuery q, CancellationToken ct)
    {
        var (from, to) = Range(q);
        var rec = await db.FeeReceipts.AsNoTracking().Where(r => !r.IsVoided && r.Date >= from && r.Date <= to)
            .GroupBy(r => new { r.Date, r.Method }).Select(g => new { g.Key.Date, g.Key.Method, N = g.Count(), Sum = g.Sum(r => r.TotalPaid) }).ToListAsync(ct);
        var rows = rec.GroupBy(r => r.Date).OrderBy(g => g.Key).Select(g =>
        {
            decimal M(PaymentMethod p) => g.Where(x => x.Method == p).Sum(x => x.Sum);
            return new Dictionary<string, object?> { ["date"] = g.Key, ["receipts"] = g.Sum(x => x.N), ["cash"] = M(PaymentMethod.Cash), ["bkash"] = M(PaymentMethod.BKash),
                ["nagad"] = M(PaymentMethod.Nagad), ["bank"] = M(PaymentMethod.Bank), ["total"] = g.Sum(x => x.Sum) };
        }).ToList();
        var totals = new Dictionary<string, object?> { ["date"] = "মোট" };
        foreach (var k in new[] { "cash", "bkash", "nagad", "bank", "total" }) totals[k] = rows.Sum(r => (decimal)r[k]!);
        totals["receipts"] = rows.Sum(r => (int)r["receipts"]!);
        return new ReportTable("collection", "ফি আদায় রিপোর্ট", $"{Fmt(from)} থেকে {Fmt(to)}",
            [new("date", "তারিখ", "date"), new("receipts", "রসিদ", "num"), new("cash", "নগদ", "money"), new("bkash", "বিকাশ", "money"), new("nagad", "নগদ (মোবাইল)", "money"),
             new("bank", "ব্যাংক", "money"), new("total", "মোট", "money")], rows, totals);
    }

    private async Task<ReportTable> DuesAsync(ReportQuery q, CancellationToken ct)
    {
        var m = Months.IsValid(q.Month) ? q.Month! : Months.Of(clock.Today);
        var list = await fees.DueListAsync(m, q.ClassId, ct);
        var rows = list.Select(d => new Dictionary<string, object?> { ["code"] = d.StudentCode, ["name"] = d.Name, ["class"] = d.ClassName, ["roll"] = d.Roll,
            ["phone"] = d.Phone, ["months"] = d.MonthsDue, ["oldest"] = d.OldestMonth, ["due"] = d.TotalDue }).ToList();
        return new ReportTable("dues", "বকেয়া তালিকা", $"{Months.Label(m)} পর্যন্ত",
            [new("code", "আইডি"), new("name", "নাম"), new("class", "শ্রেণি"), new("roll", "রোল", "num"), new("phone", "অভিভাবকের ফোন"), new("months", "মাস", "num"),
             new("oldest", "পুরনো মাস", "month"), new("due", "বকেয়া", "money")],
            rows, new() { ["name"] = $"মোট {Digits.ToBangla(rows.Count)} জন", ["due"] = list.Sum(d => d.TotalDue) });
    }

    private async Task<ReportTable> ResultsAsync(ReportQuery q, CancellationToken ct)
    {
        var sessionId = await db.CurrentSessionIdAsync(ct);
        var exam = q.ExamId is int eid ? await db.Exams.AsNoTracking().FirstOrDefaultAsync(e => e.Id == eid, ct)
            : await db.Exams.AsNoTracking().Where(e => e.SessionId == sessionId && e.IsPublished).OrderByDescending(e => e.StartDate).FirstOrDefaultAsync(ct);
        if (exam is null) return new ReportTable("results", "ফলাফল সারসংক্ষেপ", "কোনো প্রকাশিত পরীক্ষা নেই", [new("class", "শ্রেণি")], [], null);
        var res = await db.ExamResults.AsNoTracking().Where(r => r.ExamId == exam.Id)
            .Join(db.Enrollments.Where(e => e.SessionId == exam.SessionId), r => r.StudentId, e => e.StudentId, (r, e) => new { r, e.ClassId, e.Class!.NameBn, e.Class.SortOrder, e.Student!.Name })
            .ToListAsync(ct);
        var enrolled = await db.Enrollments.Where(e => e.SessionId == exam.SessionId && e.Student!.Status == StudentStatus.Active).GroupBy(e => e.ClassId)
            .Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.N, ct);
        var rows = res.GroupBy(x => new { x.ClassId, x.NameBn, x.SortOrder }).OrderBy(g => g.Key.SortOrder).Select(g =>
        {
            var pass = g.Count(x => x.r.FailedSubjects == 0);
            var top = g.Where(x => x.r.MeritPosition == 1).Select(x => x.Name).ToList();
            return new Dictionary<string, object?> { ["class"] = g.Key.NameBn, ["students"] = enrolled.GetValueOrDefault(g.Key.ClassId), ["appeared"] = g.Count(),
                ["passed"] = pass, ["failed"] = g.Count() - pass, ["pct"] = Math.Round(100.0 * pass / g.Count(), 1),
                ["avg"] = Math.Round(g.Average(x => x.r.Average), 1), ["top"] = string.Join(", ", top) };
        }).ToList();
        var appeared = rows.Sum(r => (int)r["appeared"]!); var passed = rows.Sum(r => (int)r["passed"]!);
        return new ReportTable("results", "ফলাফল সারসংক্ষেপ", exam.Name + (exam.IsPublished ? "" : " (অপ্রকাশিত)"),
            [new("class", "শ্রেণি"), new("students", "শিক্ষার্থী", "num"), new("appeared", "অংশগ্রহণ", "num"), new("passed", "পাস", "num"), new("failed", "ফেল", "num"),
             new("pct", "পাসের হার", "pct"), new("avg", "গড় নম্বর", "num"), new("top", "প্রথম স্থান")],
            rows, new() { ["class"] = "মোট", ["students"] = rows.Sum(r => (int)r["students"]!), ["appeared"] = appeared, ["passed"] = passed, ["failed"] = appeared - passed,
                ["pct"] = appeared == 0 ? null : Math.Round(100.0 * passed / appeared, 1) });
    }

    private async Task<ReportTable> LedgerAsync(ReportQuery q, CancellationToken ct)
    {
        var (from, to) = Range(q);
        var baseQ = db.LedgerEntries.AsNoTracking().AsQueryable();
        if (q.FundId is int f) baseQ = baseQ.Where(l => l.FundId == f);
        var opening = await baseQ.Where(l => l.Date < from).SumAsync(l => (decimal?)(l.Direction == Direction.In ? l.Amount : -l.Amount), ct) ?? 0;
        var entries = await baseQ.Where(l => l.Date >= from && l.Date <= to).OrderBy(l => l.Date).ThenBy(l => l.Id)
            .Select(l => new { l.Date, l.VoucherNo, l.Description, Cat = l.Category!.NameBn, Fund = l.Fund!.NameBn, l.Direction, l.Amount }).ToListAsync(ct);
        var bal = opening;
        var rows = new List<Dictionary<string, object?>> { new() { ["description"] = "প্রারম্ভিক জের", ["balance"] = opening } };
        foreach (var e in entries)
        {
            bal += e.Direction == Direction.In ? e.Amount : -e.Amount;
            rows.Add(new() { ["date"] = e.Date, ["voucher"] = e.VoucherNo, ["description"] = e.Description, ["category"] = e.Cat, ["fund"] = e.Fund,
                ["in"] = e.Direction == Direction.In ? e.Amount : null, ["out"] = e.Direction == Direction.Out ? e.Amount : null, ["balance"] = bal });
        }
        var fund = q.FundId is int fid ? await db.Funds.Where(x => x.Id == fid).Select(x => x.NameBn).FirstOrDefaultAsync(ct) : "সব ফান্ড";
        return new ReportTable("ledger", "আয়-ব্যয় বিবরণী", $"{fund} · {Fmt(from)} থেকে {Fmt(to)}",
            [new("date", "তারিখ", "date"), new("voucher", "ভাউচার"), new("description", "বিবরণ"), new("category", "খাত"), new("fund", "ফান্ড"),
             new("in", "আয়", "money"), new("out", "ব্যয়", "money"), new("balance", "জের", "money")],
            rows, new() { ["description"] = "সমাপনী জের", ["in"] = entries.Where(e => e.Direction == Direction.In).Sum(e => e.Amount),
                ["out"] = entries.Where(e => e.Direction == Direction.Out).Sum(e => e.Amount), ["balance"] = bal });
    }

    private (DateOnly, DateOnly) Range(ReportQuery q)
    {
        var to = q.To ?? clock.Today;
        var from = q.From ?? new DateOnly(to.Year, to.Month, 1);
        if (from > to) throw AppException.Invalid(new Dictionary<string, string> { ["from"] = "শুরুর তারিখ শেষের পরে হতে পারে না" });
        if (to.DayNumber - from.DayNumber > 800) throw AppException.Invalid(new Dictionary<string, string> { ["from"] = "একবারে দুই বছরের বেশি দেখানো যাবে না" });
        return (from, to);
    }

    private static string Fmt(DateOnly d) => Digits.ToBangla(d.ToString("dd/MM/yyyy"));
}
