using Madrasa.Application.Attendance;
using Madrasa.Application.Common;
using Madrasa.Application.Fees;
using Madrasa.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Madrasa.Application.Dashboard;

public record RollToken(int StudentId, int Roll, string Name, bool Present);
public record RollClass(int ClassId, string ClassName, int Total, int Present, IReadOnlyList<RollToken> Students);
public record TrendPoint(DateOnly Date, double Percent);
public record MoneyPoint(string Month, decimal Income, decimal Expense);
public record NamedCount(string Name, int Count);
public record RecentStudent(int Id, string Name, string? ClassName, DateOnly AdmissionDate);
public record NoticeItem(int Id, string Title, DateOnly Date);
public record DashboardDto(
    DateOnly Today, bool IsWorkDay, string? HolidayTitle,
    int TotalStudents, int NewThisMonth, int PresentToday, double TodayPercent, double YesterdayPercent,
    decimal CollectedThisMonth, decimal ExpectedThisMonth, decimal CollectedLastMonth, decimal TotalDue, int DueStudents,
    IReadOnlyList<RollClass> RollCall, IReadOnlyList<TrendPoint> AttendanceTrend, IReadOnlyList<MoneyPoint> IncomeExpense,
    IReadOnlyList<NamedCount> ByDepartment, IReadOnlyList<RecentStudent> RecentAdmissions, IReadOnlyList<DueListRow> TopDues,
    IReadOnlyList<NoticeItem> Notices);

public class DashboardService(IAppDbContext db, IClock clock, AttendanceService attendance, FeeService fees)
{
    public async Task<DashboardDto> GetAsync(CancellationToken ct)
    {
        var today = clock.Today;
        var sessionId = await db.CurrentSessionIdAsync(ct);
        var thisMonth = Months.Of(today);
        var monthStart = Months.First(thisMonth);

        var day = await attendance.DayAsync(today, null, ct);
        var roll = day.Rows.GroupBy(r => (r.ClassId, r.ClassName)).Select(g => new RollClass(g.Key.ClassId, g.Key.ClassName, g.Count(),
            g.Count(r => r.Status is AttendanceStatus.Present or AttendanceStatus.Late),
            g.OrderBy(r => r.Roll).Select(r => new RollToken(r.StudentId, r.Roll, r.Name, r.Status is AttendanceStatus.Present or AttendanceStatus.Late)).ToList())).ToList();
        var total = day.Rows.Count;
        var presentToday = roll.Sum(r => r.Present);

        // Attendance trend: last 30 school days
        var from = today.AddDays(-45);
        var workDays = (await db.WorkDaysAsync(from, today, ct)).TakeLast(30).ToList();
        var counts = await db.StudentAttendance.Where(a => a.Date >= from && a.Date <= today && (a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late))
            .GroupBy(a => a.Date).Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.N, ct);
        var trend = workDays.Select(d => new TrendPoint(d, total == 0 ? 0 : Math.Round(counts.GetValueOrDefault(d) * 100.0 / total, 1))).ToList();
        var yesterday = trend.Where(t => t.Date < today).Select(t => t.Percent).LastOrDefault();

        var summary = await fees.MonthSummaryAsync(thisMonth, ct);
        var lastStart = monthStart.AddMonths(-1);
        var collectedLast = await db.FeeReceipts.Where(r => !r.IsVoided && r.Date >= lastStart && r.Date < monthStart).SumAsync(r => (decimal?)r.TotalPaid, ct) ?? 0;
        var dues = await fees.DueListAsync(thisMonth, null, ct);

        // Income vs expense, last 6 months, from the ledger (net of reversals)
        var sixStart = monthStart.AddMonths(-5);
        var ledger = await db.LedgerEntries.Where(l => l.Date >= sixStart)
            .Select(l => new { l.Date, l.Amount, l.Direction, Kind = l.Category!.Kind }).ToListAsync(ct);
        var money = Months.Range(sixStart, monthStart).Select(m =>
        {
            var rows = ledger.Where(l => Months.Of(l.Date) == m).ToList();
            decimal Signed(bool income) => rows.Where(l => (l.Kind == LedgerKind.Income) == income)
                .Sum(l => income ? (l.Direction == Direction.In ? l.Amount : -l.Amount) : (l.Direction == Direction.Out ? l.Amount : -l.Amount));
            return new MoneyPoint(m, Signed(true), Signed(false));
        }).ToList();

        var byDept = await db.Enrollments.Where(e => e.SessionId == sessionId && e.Student!.Status == StudentStatus.Active && !e.Student.IsDeleted)
            .GroupBy(e => new { e.Class!.Department!.NameBn, e.Class.Department.SortOrder })
            .Select(g => new { g.Key.NameBn, g.Key.SortOrder, N = g.Count() }).OrderBy(x => x.SortOrder).ToListAsync(ct);

        var recent = await db.Students.AsNoTracking().OrderByDescending(s => s.AdmissionDate).ThenByDescending(s => s.Id).Take(5)
            .Select(s => new RecentStudent(s.Id, s.Name, s.Enrollments.Where(e => e.SessionId == sessionId).Select(e => e.Class!.NameBn).FirstOrDefault(), s.AdmissionDate))
            .ToListAsync(ct);
        var newThisMonth = await db.Students.CountAsync(s => s.AdmissionDate >= monthStart, ct);
        var notices = await db.Notices.AsNoTracking().OrderByDescending(n => n.Date).ThenByDescending(n => n.Id).Take(4)
            .Select(n => new NoticeItem(n.Id, n.Title, n.Date)).ToListAsync(ct);

        return new DashboardDto(today, day.IsWorkDay, day.HolidayTitle, total, newThisMonth, presentToday,
            total == 0 ? 0 : Math.Round(presentToday * 100.0 / total, 1), yesterday,
            summary.CollectedInMonth, summary.Expected, collectedLast, dues.Sum(d => d.TotalDue), dues.Count,
            roll, trend, money, byDept.Select(d => new NamedCount(d.NameBn, d.N)).ToList(), recent,
            dues.OrderByDescending(d => d.TotalDue).Take(5).ToList(), notices);
    }
}
