using Madrasa.Application.Accounts;
using Madrasa.Application.Common;
using Madrasa.Application.Hostel;
using Madrasa.Application.Library;
using Madrasa.Application.Staffing;
using Madrasa.Domain.Entities;
using Madrasa.Domain.Enums;
using Madrasa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Madrasa.Tests;

/// <summary>Business rules of the phase 6 office modules, on an in-memory database with the reference seed (funds, categories).</summary>
public class OfficeServiceTests
{
    private sealed class Clock : IClock { public DateOnly Today => new(2026, 11, 21); public TimeOnly Now => new(9, 0); public DateTime ToLocal(DateTime utc) => utc.AddHours(6); }
    private sealed class User : ICurrentUser { public string? UserId => "u1"; }
    private sealed class Files : IFileStorage { public Task<string> SaveAsync(string f, string n, byte[] d, CancellationToken ct = default) => Task.FromResult($"/uploads/{f}/{n}"); }

    private static AppDbContext Db()
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new User());
        db.Database.EnsureCreated();
        return db;
    }
    private static readonly Clock C = new();

    [Fact]
    public async Task Expense_cannot_overspend_a_fund()
    {
        using var db = Db();
        var acc = new AccountsService(db, C, new User());
        await acc.AddAsync(new(null, 2, 2, 1000, "যাকাত", "দাতা", "01711111111", null), default);   // Zakat fund +1000
        var ex = await Assert.ThrowsAsync<AppException>(() => acc.AddAsync(new(null, 2, 7, 1500, "বই", null, null, null), default));
        Assert.Contains("amount", ex.FieldErrors!.Keys);
        var ok = await acc.AddAsync(new(null, 2, 7, 600, "বই", null, null, null), default);
        Assert.Equal(Direction.Out, ok.Direction);
        Assert.Equal(400, await acc.BalanceAsync(2, default));
    }

    [Fact]
    public async Task Fee_category_is_only_for_receipts_and_donor_is_reused_by_phone()
    {
        using var db = Db();
        var acc = new AccountsService(db, C, new User());
        await Assert.ThrowsAsync<AppException>(() => acc.AddAsync(new(null, 1, 1, 500, "বেতন", null, null, null), default));
        await acc.AddAsync(new(null, 1, 2, 500, null, "হাজী সাহেব", "০১৭১১১১১১১১", null), default);
        await acc.AddAsync(new(null, 1, 2, 700, null, "হাজী সাহেব", "01711111111", null), default);
        var donors = await acc.DonorsAsync(null, default);
        Assert.Single(donors);
        Assert.Equal(1200, donors[0].Total);
    }

    [Fact]
    public async Task Salary_posts_to_ledger_once_and_respects_the_fund()
    {
        using var db = Db();
        db.Staff.Add(new Staff { StaffCode = "T-001", Name = "উস্তাদ", Designation = "মুহাদ্দিস", BasicSalary = 10000 });
        await db.SaveChangesAsync();
        var acc = new AccountsService(db, C, new User());
        var staff = new StaffService(db, C, new Files(), new User(), acc);
        var sid = db.Staff.Single().Id;

        var noMoney = await Assert.ThrowsAsync<AppException>(() => staff.PayAsync(new(sid, "2026-10", 0, 0, 1, null), default));
        Assert.Contains("fundId", noMoney.FieldErrors!.Keys);

        await acc.AddAsync(new(null, 1, 2, 20000, "দান", null, null, null), default);
        var paid = await staff.PayAsync(new(sid, "2026-10", 500, 200, 1, null), default);
        Assert.Equal(10300, paid.NetPaid);
        Assert.Equal(9700, await acc.BalanceAsync(1, default));
        var dup = await Assert.ThrowsAsync<AppException>(() => staff.PayAsync(new(sid, "2026-10", 0, 0, 1, null), default));
        Assert.Equal(409, dup.Status);
        await Assert.ThrowsAsync<AppException>(() => staff.PayAsync(new(sid, "2026-12", 0, 0, 1, null), default));   // next month

        await staff.CancelPaymentAsync(paid.PaymentId!.Value, default);
        Assert.Equal(20000, await acc.BalanceAsync(1, default));
    }

    private static async Task<(Student boy, Student girl)> StudentsAsync(AppDbContext db)
    {
        var boy = new Student { StudentCode = "RM-1", Name = "ছাত্র", Gender = Gender.Male, AdmissionDate = C.Today };
        var girl = new Student { StudentCode = "RM-2", Name = "ছাত্রী", Gender = Gender.Female, AdmissionDate = C.Today };
        db.Students.AddRange(boy, girl);
        await db.SaveChangesAsync();
        return (boy, girl);
    }

    [Fact]
    public async Task Hostel_checks_gender_and_seats()
    {
        using var db = Db();
        var (boy, girl) = await StudentsAsync(db);
        var hostel = new HostelService(db, C);
        var room = await hostel.SaveRoomAsync(null, new("ক", "১০১", 1, Gender.Male), default);
        Assert.Equal("101", room.RoomNo);   // Bangla digits are stored as English

        await Assert.ThrowsAsync<AppException>(() => hostel.AllocateAsync(new(girl.Id, room.Id, null, null), default));
        await hostel.AllocateAsync(new(boy.Id, room.Id, null, null), default);
        Assert.True(db.Students.Single(s => s.Id == boy.Id).IsResidential);
        await Assert.ThrowsAsync<AppException>(() => hostel.SaveRoomAsync(room.Id, new("ক", "101", 1, Gender.Female), default));
        await Assert.ThrowsAsync<AppException>(() => hostel.DeleteRoomAsync(room.Id, default));

        await hostel.AddLeaveAsync(new(boy.Id, C.Today.AddDays(-5), C.Today.AddDays(-1), "বাড়ি", "পিতা"), default);
        var s = await db.HostelLeaves.SingleAsync();
        Assert.Null(s.ReturnedOn);
        await Assert.ThrowsAsync<AppException>(() => hostel.AddLeaveAsync(new(boy.Id, C.Today, null, null, null), default));   // still away
    }

    [Fact]
    public async Task Library_limits_copies_and_charges_late_fine()
    {
        using var db = Db();
        var (boy, girl) = await StudentsAsync(db);
        var lib = new LibraryService(db, C);
        var book = await lib.SaveBookAsync(null, new("নাহবেমীর", null, null, BookLanguage.Urdu, "নাহু", "D-1", 1), default);
        await lib.IssueAsync(new(book.Id, boy.Id, null, 14), default);
        var none = await Assert.ThrowsAsync<AppException>(() => lib.IssueAsync(new(book.Id, girl.Id, null, 14), default));
        Assert.Equal(409, none.Status);
        await Assert.ThrowsAsync<AppException>(() => lib.SaveBookAsync(book.Id, new("নাহবেমীর", null, null, BookLanguage.Urdu, null, null, 0), default));

        // Pretend it was issued 20 days ago: 6 days late at the default 2 taka a day
        var issue = await db.BookIssues.SingleAsync();
        issue.IssuedOn = C.Today.AddDays(-20); issue.DueOn = C.Today.AddDays(-6);
        await db.SaveChangesAsync();
        var outRow = (await lib.IssuesAsync("overdue", default)).Single();
        Assert.Equal(6, outRow.LateDays);
        Assert.Equal(12, outRow.FineDue);
        var back = await lib.ReturnAsync(issue.Id, new(null), default);
        Assert.Equal(12, back.Fine);
        Assert.Equal(1, (await lib.BooksAsync(null, null, null, default)).Single().Available);
    }
}
