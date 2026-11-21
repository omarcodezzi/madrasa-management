using System.Text.RegularExpressions;
using Madrasa.Application.Accounts;
using Madrasa.Application.Common;
using Madrasa.Domain.Entities;
using Madrasa.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Madrasa.Application.Staffing;

public record StaffRow(int Id, string StaffCode, string Name, string Designation, string? Phone, string? PhotoPath, bool IsActive, decimal? BasicSalary,
    DateOnly? JoinDate, int Subjects, AttendanceStatus? Today);
public record StaffSubject(int ClassSubjectId, string ClassName, string SubjectName);
public record StaffDetail(int Id, string StaffCode, string Name, string Designation, string? Phone, string? Email, string? Nid, string? Address,
    string? Qualification, DateOnly? JoinDate, decimal? BasicSalary, string? PhotoPath, bool IsActive, IReadOnlyList<StaffSubject> Subjects,
    int PresentThisMonth, int WorkDaysThisMonth, IReadOnlyList<SalaryRow> Salaries);
public record StaffInput(string Name, string Designation, string? Phone, string? Email, string? Nid, string? Address, string? Qualification,
    DateOnly? JoinDate, decimal? BasicSalary, bool IsActive, string? Photo);
public record StaffDayRow(int StaffId, string Name, string Designation, string? PhotoPath, AttendanceStatus? Status, TimeOnly? CheckIn);
public record StaffMark(int StaffId, AttendanceStatus? Status);
public record SalaryRow(int? PaymentId, int StaffId, string StaffCode, string Name, string Designation, string Month, decimal Basic,
    decimal Allowance, decimal Deduction, decimal NetPaid, DateOnly? PaidOn, string? Fund, int Present, int WorkDays);
public record PayInput(int StaffId, string Month, decimal Allowance, decimal Deduction, int FundId, DateOnly? PaidOn);

/// <summary>Ustads and staff: records, daily attendance, monthly salary (posted to the ledger).</summary>
public class StaffService(IAppDbContext db, IClock clock, IFileStorage files, ICurrentUser user, AccountsService accounts)
{
    private const int SalaryCategoryId = 3;

    public async Task<List<StaffRow>> ListAsync(string? q, bool? active, CancellationToken ct)
    {
        var sessionId = await db.CurrentSessionIdAsync(ct);
        var today = clock.Today;
        var query = db.Staff.AsNoTracking().AsQueryable();
        if (active is bool a) query = query.Where(s => s.IsActive == a);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var t = q.Trim(); var ph = Digits.PhoneDigits(t);
            query = query.Where(s => s.Name.Contains(t) || s.StaffCode.Contains(t) || s.Designation.Contains(t) || (ph.Length >= 4 && s.Phone != null && s.Phone.Contains(ph)));
        }
        return await query.OrderByDescending(s => s.IsActive).ThenBy(s => s.StaffCode)
            .Select(s => new StaffRow(s.Id, s.StaffCode, s.Name, s.Designation, s.Phone, s.PhotoPath, s.IsActive, s.BasicSalary, s.JoinDate,
                db.ClassSubjects.Count(c => c.TeacherId == s.Id && c.SessionId == sessionId),
                db.StaffAttendance.Where(x => x.StaffId == s.Id && x.Date == today).Select(x => (AttendanceStatus?)x.Status).FirstOrDefault()))
            .ToListAsync(ct);
    }

    public async Task<StaffDetail> GetAsync(int id, CancellationToken ct)
    {
        var sessionId = await db.CurrentSessionIdAsync(ct);
        var s = await db.Staff.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw AppException.NotFound("স্টাফ");
        var subjects = await db.ClassSubjects.AsNoTracking().Where(c => c.TeacherId == id && c.SessionId == sessionId)
            .OrderBy(c => c.Class!.SortOrder).Select(c => new StaffSubject(c.Id, c.Class!.NameBn, c.Subject!.NameBn)).ToListAsync(ct);
        var first = new DateOnly(clock.Today.Year, clock.Today.Month, 1);
        var work = (await db.WorkDaysAsync(first, clock.Today, ct)).Count;
        var present = await db.StaffAttendance.CountAsync(a => a.StaffId == id && a.Date >= first && (a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late), ct);
        var salaries = await db.SalaryPayments.AsNoTracking().Where(p => p.StaffId == id).OrderByDescending(p => p.Month).Take(12)
            .Select(p => new SalaryRow(p.Id, id, s.StaffCode, s.Name, s.Designation, p.Month, p.Basic, p.Allowance, p.Deduction, p.NetPaid, p.PaidOn,
                db.Funds.Where(f => f.Id == p.FundId).Select(f => f.NameBn).FirstOrDefault(), 0, 0)).ToListAsync(ct);
        return new StaffDetail(s.Id, s.StaffCode, s.Name, s.Designation, s.Phone, s.Email, s.Nid, s.Address, s.Qualification, s.JoinDate, s.BasicSalary,
            s.PhotoPath, s.IsActive, subjects, present, work, salaries);
    }

    public async Task<StaffDetail> SaveAsync(int? id, StaffInput input, CancellationToken ct)
    {
        var err = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(input.Name)) err["name"] = "নাম লিখুন";
        if (string.IsNullOrWhiteSpace(input.Designation)) err["designation"] = "পদবি লিখুন";
        if (!string.IsNullOrWhiteSpace(input.Phone) && !Digits.IsBdMobile(input.Phone)) err["phone"] = "সঠিক মোবাইল নম্বর দিন (০১ দিয়ে শুরু, ১১ সংখ্যা)";
        if (!string.IsNullOrWhiteSpace(input.Email) && !Regex.IsMatch(input.Email.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$")) err["email"] = "সঠিক ইমেইল দিন";
        if (input.BasicSalary is < 0 or > 10_000_000) err["basicSalary"] = "বেতনের পরিমাণ ঠিক নেই";
        if (input.JoinDate > clock.Today) err["joinDate"] = "যোগদানের তারিখ ভবিষ্যতে হতে পারে না";
        if (err.Count > 0) throw AppException.Invalid(err);

        Staff s;
        if (id is null)
        {
            var codes = await db.Staff.IgnoreQueryFilters().Select(x => x.StaffCode).ToListAsync(ct);
            var next = codes.Select(c => int.TryParse(c.Split('-').Last(), out var n) ? n : 0).DefaultIfEmpty(0).Max() + 1;
            s = new Staff { StaffCode = $"T-{next:000}", CreatedAt = DateTime.UtcNow, CreatedBy = user.UserId };
            db.Staff.Add(s);
        }
        else
        {
            s = await db.Staff.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw AppException.NotFound("স্টাফ");
            s.UpdatedAt = DateTime.UtcNow; s.UpdatedBy = user.UserId;
        }
        s.Name = input.Name.Trim(); s.Designation = input.Designation.Trim();
        s.Phone = string.IsNullOrWhiteSpace(input.Phone) ? null : Digits.PhoneDigits(input.Phone);
        s.Email = Blank(input.Email); s.Nid = Blank(Digits.ToEnglish(input.Nid)); s.Address = Blank(input.Address); s.Qualification = Blank(input.Qualification);
        s.JoinDate = input.JoinDate; s.BasicSalary = input.BasicSalary; s.IsActive = input.IsActive;
        await db.SaveChangesAsync(ct);
        if (!string.IsNullOrEmpty(input.Photo)) { s.PhotoPath = await Photos.SaveAsync(files, "staff", s.StaffCode, input.Photo, ct); await db.SaveChangesAsync(ct); }
        return await GetAsync(s.Id, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var s = await db.Staff.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw AppException.NotFound("স্টাফ");
        s.IsDeleted = true; s.IsActive = false;
        foreach (var cs in await db.ClassSubjects.Where(c => c.TeacherId == id).ToListAsync(ct)) cs.TeacherId = null;
        await db.SaveChangesAsync(ct);
    }

    // ---- attendance ----
    public async Task<List<StaffDayRow>> DayAsync(DateOnly? date, CancellationToken ct)
    {
        var d = date ?? clock.Today;
        return await db.Staff.AsNoTracking().Where(s => s.IsActive).OrderBy(s => s.StaffCode)
            .Select(s => new { s.Id, s.Name, s.Designation, s.PhotoPath, A = db.StaffAttendance.Where(a => a.StaffId == s.Id && a.Date == d).Select(a => new { a.Status, a.CheckIn }).FirstOrDefault() })
            .Select(x => new StaffDayRow(x.Id, x.Name, x.Designation, x.PhotoPath, x.A == null ? null : x.A.Status, x.A == null ? null : x.A.CheckIn)).ToListAsync(ct);
    }

    public async Task<int> MarkAsync(DateOnly date, IReadOnlyList<StaffMark> marks, CancellationToken ct)
    {
        if (date > clock.Today) throw new AppException("ভবিষ্যতের তারিখে হাজিরা দেওয়া যাবে না");
        var ids = marks.Select(m => m.StaffId).ToList();
        var existing = await db.StaffAttendance.Where(a => a.Date == date && ids.Contains(a.StaffId)).ToDictionaryAsync(a => a.StaffId, ct);
        int n = 0;
        foreach (var m in marks)
        {
            existing.TryGetValue(m.StaffId, out var row);
            if (m.Status is null) { if (row != null) { db.StaffAttendance.Remove(row); n++; } continue; }
            if (row is null) { db.StaffAttendance.Add(new StaffAttendance { StaffId = m.StaffId, Date = date, Status = m.Status.Value, CheckIn = date == clock.Today ? clock.Now : null }); n++; }
            else if (row.Status != m.Status) { row.Status = m.Status.Value; n++; }
        }
        await db.SaveChangesAsync(ct);
        return n;
    }

    // ---- salary ----
    public async Task<List<SalaryRow>> SalarySheetAsync(string? month, CancellationToken ct)
    {
        var m = Months.IsValid(month) ? month! : Months.Of(clock.Today);
        var from = Months.First(m); var to = from.AddMonths(1).AddDays(-1);
        if (to > clock.Today) to = clock.Today;
        var work = from > clock.Today ? 0 : (await db.WorkDaysAsync(from, to, ct)).Count;
        var paid = await db.SalaryPayments.AsNoTracking().Where(p => p.Month == m).ToDictionaryAsync(p => p.StaffId, ct);
        var funds = await db.Funds.AsNoTracking().ToDictionaryAsync(f => f.Id, f => f.NameBn, ct);
        var staff = await db.Staff.AsNoTracking().IgnoreQueryFilters().Where(s => (s.IsActive && !s.IsDeleted) || paid.Keys.Contains(s.Id)).OrderBy(s => s.StaffCode)
            .Select(s => new { s.Id, s.StaffCode, s.Name, s.Designation, s.BasicSalary,
                Present = db.StaffAttendance.Count(a => a.StaffId == s.Id && a.Date >= from && a.Date <= to && (a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late)) })
            .ToListAsync(ct);
        return staff.Select(s => paid.TryGetValue(s.Id, out var p)
            ? new SalaryRow(p.Id, s.Id, s.StaffCode, s.Name, s.Designation, m, p.Basic, p.Allowance, p.Deduction, p.NetPaid, p.PaidOn, p.FundId is int f ? funds.GetValueOrDefault(f) : null, s.Present, work)
            : new SalaryRow(null, s.Id, s.StaffCode, s.Name, s.Designation, m, s.BasicSalary ?? 0, 0, 0, 0, null, null, s.Present, work)).ToList();
    }

    public async Task<SalaryRow> PayAsync(PayInput input, CancellationToken ct)
    {
        var err = new Dictionary<string, string>();
        if (!Months.IsValid(input.Month)) err["month"] = "মাস ঠিক নেই";
        else if (Months.First(input.Month) > clock.Today) err["month"] = "আগামী মাসের বেতন আগে দেওয়া যাবে না";
        var s = await db.Staff.FirstOrDefaultAsync(x => x.Id == input.StaffId, ct) ?? throw AppException.NotFound("স্টাফ");
        if (s.BasicSalary is null or <= 0) err["basic"] = "এই স্টাফের মূল বেতন ঠিক করা নেই";
        if (input.Allowance < 0) err["allowance"] = "ভাতা ঋণাত্মক হতে পারে না";
        if (input.Deduction < 0) err["deduction"] = "কর্তন ঋণাত্মক হতে পারে না";
        var net = (s.BasicSalary ?? 0) + input.Allowance - input.Deduction;
        if (net <= 0 && !err.ContainsKey("basic")) err["deduction"] = "কর্তন বেতনের চেয়ে বেশি";
        if (!await db.Funds.AnyAsync(f => f.Id == input.FundId, ct)) err["fundId"] = "ফান্ড বাছাই করুন";
        if (err.Count == 0 && await db.SalaryPayments.AnyAsync(p => p.StaffId == s.Id && p.Month == input.Month, ct))
            throw new AppException($"{s.Name}-এর এই মাসের বেতন আগেই দেওয়া হয়েছে", 409);
        if (err.Count == 0)
        {
            var bal = await accounts.BalanceAsync(input.FundId, ct);
            if (net > bal) err["fundId"] = $"এই ফান্ডে আছে মাত্র {Digits.Money(bal)} টাকা";
        }
        if (err.Count > 0) throw AppException.Invalid(err);

        var date = input.PaidOn ?? clock.Today;
        var pay = new SalaryPayment { StaffId = s.Id, Month = input.Month, Basic = s.BasicSalary!.Value, Allowance = input.Allowance, Deduction = input.Deduction,
            NetPaid = net, PaidOn = date, FundId = input.FundId };
        db.SalaryPayments.Add(pay);
        await db.SaveChangesAsync(ct);
        db.LedgerEntries.Add(new LedgerEntry { Date = date, FundId = input.FundId, CategoryId = SalaryCategoryId, Direction = Direction.Out, Amount = net,
            Description = $"{s.Name} ({s.Designation}) — {Months.Label(input.Month)} মাসের বেতন", VoucherNo = $"S-{pay.Id:000000}", SalaryPaymentId = pay.Id,
            CreatedBy = user.UserId, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync(ct);
        return (await SalarySheetAsync(input.Month, ct)).First(r => r.StaffId == s.Id);
    }

    public async Task CancelPaymentAsync(int paymentId, CancellationToken ct)
    {
        var p = await db.SalaryPayments.FirstOrDefaultAsync(x => x.Id == paymentId, ct) ?? throw AppException.NotFound("বেতন প্রদান");
        db.LedgerEntries.RemoveRange(db.LedgerEntries.Where(l => l.SalaryPaymentId == paymentId));
        db.SalaryPayments.Remove(p);
        await db.SaveChangesAsync(ct);
    }

    private static string? Blank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
