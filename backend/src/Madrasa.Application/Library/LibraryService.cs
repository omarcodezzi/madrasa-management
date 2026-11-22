using Madrasa.Application.Common;
using Madrasa.Domain.Entities;
using Madrasa.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Madrasa.Application.Library;

public record BookDto(int Id, string Title, string? TitleAr, string? Author, BookLanguage Language, string? Category, string? ShelfNo, int TotalCopies, int Available);
public record BookInput(string Title, string? TitleAr, string? Author, BookLanguage Language, string? Category, string? ShelfNo, int TotalCopies);
public record IssueRow(int Id, int BookId, string Title, string? TitleAr, string BorrowerKind, int BorrowerId, string BorrowerCode, string BorrowerName,
    DateOnly IssuedOn, DateOnly DueOn, DateOnly? ReturnedOn, decimal Fine, int LateDays, decimal FineDue, decimal FinePaid, decimal FineWaived, decimal FineOutstanding);
public record IssueInput(int BookId, int? StudentId, int? StaffId, int Days);
/// <summary>Collect: take the fine now and post it to the ledger (general fund unless FundId says otherwise).</summary>
public record ReturnInput(decimal? Fine, bool Collect = false, int? FundId = null);
public record FineInput(decimal? Amount, int? FundId);
public record Borrower(string Kind, int Id, string Code, string Name, string? Detail);
public record LibrarySummary(int Titles, int Copies, int Out, int Overdue, decimal FinePerDay, IReadOnlyList<string> Categories, int FinesPending, decimal FineOutstanding);

/// <summary>Books, issue to a student or staff member, return with an overdue fine.</summary>
public class LibraryService(IAppDbContext db, IClock clock, ICurrentUser user)
{
    public const int FineCategoryId = 8;
    private const int GeneralFundId = 1;

    public async Task<decimal> FinePerDayAsync(CancellationToken ct)
    {
        var v = await db.AppSettings.Where(s => s.Key == "library.finePerDay").Select(s => s.Value).FirstOrDefaultAsync(ct);
        return decimal.TryParse(v, out var f) && f >= 0 ? f : 2m;
    }

    public async Task<LibrarySummary> SummaryAsync(CancellationToken ct)
    {
        var today = clock.Today;
        var books = await db.Books.AsNoTracking().Select(b => new { b.TotalCopies, b.Category }).ToListAsync(ct);
        var outN = await db.BookIssues.CountAsync(i => i.ReturnedOn == null, ct);
        var overdue = await db.BookIssues.CountAsync(i => i.ReturnedOn == null && i.DueOn < today, ct);
        var pending = await db.BookIssues.Where(i => i.ReturnedOn != null && i.Fine > i.FinePaid + i.FineWaived)
            .Select(i => i.Fine - i.FinePaid - i.FineWaived).ToListAsync(ct);
        return new LibrarySummary(books.Count, books.Sum(b => b.TotalCopies), outN, overdue, await FinePerDayAsync(ct),
            books.Where(b => b.Category != null).Select(b => b.Category!).Distinct().Order().ToList(), pending.Count, pending.Sum());
    }

    public async Task<List<BookDto>> BooksAsync(string? q, BookLanguage? lang, string? category, CancellationToken ct)
    {
        var query = db.Books.AsNoTracking().AsQueryable();
        if (lang is BookLanguage l) query = query.Where(b => b.Language == l);
        if (!string.IsNullOrWhiteSpace(category)) query = query.Where(b => b.Category == category);
        if (!string.IsNullOrWhiteSpace(q)) { var t = q.Trim(); query = query.Where(b => b.Title.Contains(t) || (b.TitleAr != null && b.TitleAr.Contains(t)) || (b.Author != null && b.Author.Contains(t)) || (b.ShelfNo != null && b.ShelfNo.Contains(t))); }
        return await query.OrderBy(b => b.Title)
            .Select(b => new BookDto(b.Id, b.Title, b.TitleAr, b.Author, b.Language, b.Category, b.ShelfNo, b.TotalCopies,
                b.TotalCopies - db.BookIssues.Count(i => i.BookId == b.Id && i.ReturnedOn == null))).Take(500).ToListAsync(ct);
    }

    public async Task<BookDto> SaveBookAsync(int? id, BookInput input, CancellationToken ct)
    {
        var err = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(input.Title)) err["title"] = "বইয়ের নাম লিখুন";
        if (!Enum.IsDefined(input.Language)) err["language"] = "ভাষা বাছাই করুন";
        if (input.TotalCopies is < 1 or > 10000) err["totalCopies"] = "কপি সংখ্যা ১ বা তার বেশি";
        Book? b = null;
        if (id is int bid)
        {
            b = await db.Books.FirstOrDefaultAsync(x => x.Id == bid, ct) ?? throw AppException.NotFound("বই");
            var outN = await db.BookIssues.CountAsync(i => i.BookId == bid && i.ReturnedOn == null, ct);
            if (input.TotalCopies < outN) err["totalCopies"] = $"{Digits.ToBangla(outN)} কপি এখন ইস্যু করা আছে";
        }
        if (err.Count > 0) throw AppException.Invalid(err);
        if (b is null) { b = new Book(); db.Books.Add(b); }
        b.Title = input.Title.Trim(); b.TitleAr = Blank(input.TitleAr); b.Author = Blank(input.Author); b.Language = input.Language;
        b.Category = Blank(input.Category); b.ShelfNo = Blank(Digits.ToEnglish(input.ShelfNo)); b.TotalCopies = input.TotalCopies;
        await db.SaveChangesAsync(ct);
        return (await BooksAsync(null, null, null, ct)).First(x => x.Id == b.Id);
    }

    public async Task DeleteBookAsync(int id, CancellationToken ct)
    {
        var b = await db.Books.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw AppException.NotFound("বই");
        if (await db.BookIssues.AnyAsync(i => i.BookId == id && i.ReturnedOn == null, ct)) throw new AppException("বইটির কপি এখনও ইস্যু করা আছে", 409);
        // Collected fines stay in the ledger as plain income once the book's history is gone
        var issueIds = await db.BookIssues.Where(i => i.BookId == id).Select(i => i.Id).ToListAsync(ct);
        foreach (var e in await db.LedgerEntries.Where(l => l.BookIssueId != null && issueIds.Contains(l.BookIssueId.Value)).ToListAsync(ct)) e.BookIssueId = null;
        db.BookIssues.RemoveRange(db.BookIssues.Where(i => i.BookId == id));
        db.Books.Remove(b);
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<Borrower>> BorrowersAsync(string? q, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q)) return [];
        var t = q.Trim(); var e = Digits.ToEnglish(t)!;
        var sessionId = await db.CurrentSessionIdAsync(ct);
        var st = await db.Students.AsNoTracking().Where(s => s.Status == StudentStatus.Active && (s.Name.Contains(t) || s.StudentCode.Contains(e))).Take(10)
            .Select(s => new Borrower("student", s.Id, s.StudentCode, s.Name, db.Enrollments.Where(x => x.StudentId == s.Id && x.SessionId == sessionId).Select(x => x.Class!.NameBn).FirstOrDefault()))
            .ToListAsync(ct);
        var sf = await db.Staff.AsNoTracking().Where(s => s.IsActive && (s.Name.Contains(t) || s.StaffCode.Contains(e))).Take(5)
            .Select(s => new Borrower("staff", s.Id, s.StaffCode, s.Name, s.Designation)).ToListAsync(ct);
        return [.. st, .. sf];
    }

    public async Task<List<IssueRow>> IssuesAsync(string filter, CancellationToken ct)
    {
        var today = clock.Today;
        var fine = await FinePerDayAsync(ct);
        var q = db.BookIssues.AsNoTracking().AsQueryable();
        q = filter switch
        {
            "overdue" => q.Where(i => i.ReturnedOn == null && i.DueOn < today),
            "returned" => q.Where(i => i.ReturnedOn != null),
            "fines" => q.Where(i => i.ReturnedOn != null && i.Fine > i.FinePaid + i.FineWaived),
            _ => q.Where(i => i.ReturnedOn == null),
        };
        var rows = await q.OrderBy(i => i.DueOn).Take(300)
            .Select(i => new { i, i.Book!.Title, i.Book.TitleAr,
                Student = db.Students.IgnoreQueryFilters().Where(s => s.Id == i.StudentId).Select(s => new { s.StudentCode, s.Name }).FirstOrDefault(),
                Staff = db.Staff.IgnoreQueryFilters().Where(s => s.Id == i.StaffId).Select(s => new { s.StaffCode, s.Name }).FirstOrDefault() })
            .ToListAsync(ct);
        if (filter is "returned" or "fines") rows = rows.OrderByDescending(r => r.i.ReturnedOn).ToList();
        return rows.Select(r =>
        {
            var late = Math.Max(0, (r.i.ReturnedOn ?? today).DayNumber - r.i.DueOn.DayNumber);
            return new IssueRow(r.i.Id, r.i.BookId, r.Title, r.TitleAr, r.i.StudentId != null ? "student" : "staff", r.i.StudentId ?? r.i.StaffId ?? 0,
                r.Student?.StudentCode ?? r.Staff?.StaffCode ?? "", r.Student?.Name ?? r.Staff?.Name ?? "",
                r.i.IssuedOn, r.i.DueOn, r.i.ReturnedOn, r.i.Fine, late, r.i.ReturnedOn == null ? late * fine : 0,
                r.i.FinePaid, r.i.FineWaived, r.i.ReturnedOn == null ? 0 : r.i.Fine - r.i.FinePaid - r.i.FineWaived);
        }).ToList();
    }

    public async Task IssueAsync(IssueInput input, CancellationToken ct)
    {
        if ((input.StudentId is null) == (input.StaffId is null)) throw AppException.Invalid(new Dictionary<string, string> { ["borrower"] = "কাকে দিচ্ছেন বাছাই করুন" });
        if (input.Days is < 1 or > 365) throw AppException.Invalid(new Dictionary<string, string> { ["days"] = "মেয়াদ ১ থেকে ৩৬৫ দিন" });
        var b = await db.Books.FirstOrDefaultAsync(x => x.Id == input.BookId, ct) ?? throw AppException.NotFound("বই");
        var outN = await db.BookIssues.CountAsync(i => i.BookId == b.Id && i.ReturnedOn == null, ct);
        if (outN >= b.TotalCopies) throw new AppException("এই বইয়ের কোনো কপি এখন তাকে নেই", 409);
        if (input.StudentId is int sid)
        {
            if (!await db.Students.AnyAsync(s => s.Id == sid && s.Status == StudentStatus.Active, ct)) throw AppException.NotFound("শিক্ষার্থী");
            if (await db.BookIssues.AnyAsync(i => i.BookId == b.Id && i.StudentId == sid && i.ReturnedOn == null, ct)) throw new AppException("এই বইটি তার কাছে আগেই আছে", 409);
            if (await db.BookIssues.CountAsync(i => i.StudentId == sid && i.ReturnedOn == null, ct) >= 3) throw new AppException("একজন শিক্ষার্থী একসাথে ৩টির বেশি বই নিতে পারে না", 409);
        }
        else if (!await db.Staff.AnyAsync(s => s.Id == input.StaffId && s.IsActive, ct)) throw AppException.NotFound("স্টাফ");
        db.BookIssues.Add(new BookIssue { BookId = b.Id, StudentId = input.StudentId, StaffId = input.StaffId, IssuedOn = clock.Today, DueOn = clock.Today.AddDays(input.Days) });
        await db.SaveChangesAsync(ct);
    }

    public async Task<IssueRow> ReturnAsync(int issueId, ReturnInput input, CancellationToken ct)
    {
        var i = await db.BookIssues.FirstOrDefaultAsync(x => x.Id == issueId, ct) ?? throw AppException.NotFound("ইস্যু");
        if (i.ReturnedOn != null) throw new AppException("বইটি আগেই ফেরত নেওয়া হয়েছে", 409);
        var late = Math.Max(0, clock.Today.DayNumber - i.DueOn.DayNumber);
        var fine = input.Fine ?? late * await FinePerDayAsync(ct);
        if (fine < 0) throw AppException.Invalid(new Dictionary<string, string> { ["fine"] = "জরিমানা ঋণাত্মক হতে পারে না" });
        if (fine > 100_000) throw AppException.Invalid(new Dictionary<string, string> { ["fine"] = "জরিমানা খুব বেশি" });
        i.ReturnedOn = clock.Today; i.Fine = fine;
        if (input.Collect && fine > 0) await PostFineAsync(i, fine, input.FundId, ct);
        await db.SaveChangesAsync(ct);
        return (await IssuesAsync("returned", ct)).First(r => r.Id == issueId);
    }

    /// <summary>Takes all or part of a pending fine and records it as library-fine income.</summary>
    public async Task<IssueRow> CollectFineAsync(int issueId, FineInput input, CancellationToken ct)
    {
        var i = await PendingAsync(issueId, ct);
        var due = i.Fine - i.FinePaid - i.FineWaived;
        var amount = input.Amount ?? due;
        if (amount <= 0 || amount > due) throw AppException.Invalid(new Dictionary<string, string> { ["amount"] = $"বাকি জরিমানা {Digits.Money(due)} টাকা, এর বেশি নেওয়া যাবে না" });
        await PostFineAsync(i, amount, input.FundId, ct);
        await db.SaveChangesAsync(ct);
        return (await IssuesAsync("returned", ct)).First(r => r.Id == issueId);
    }

    /// <summary>Forgives what is still owed; money already collected stays in the ledger.</summary>
    public async Task<IssueRow> WaiveFineAsync(int issueId, CancellationToken ct)
    {
        var i = await PendingAsync(issueId, ct);
        i.FineWaived += i.Fine - i.FinePaid - i.FineWaived;
        await db.SaveChangesAsync(ct);
        return (await IssuesAsync("returned", ct)).First(r => r.Id == issueId);
    }

    private async Task<BookIssue> PendingAsync(int issueId, CancellationToken ct)
    {
        var i = await db.BookIssues.FirstOrDefaultAsync(x => x.Id == issueId, ct) ?? throw AppException.NotFound("ইস্যু");
        if (i.ReturnedOn == null) throw new AppException("বই ফেরত নেওয়ার সময় জরিমানা ঠিক হবে", 409);
        if (i.Fine <= i.FinePaid + i.FineWaived) throw new AppException("এই ইস্যুর কোনো জরিমানা বাকি নেই", 409);
        return i;
    }

    private async Task PostFineAsync(BookIssue i, decimal amount, int? fundId, CancellationToken ct)
    {
        var fid = fundId ?? GeneralFundId;
        if (!await db.Funds.AnyAsync(f => f.Id == fid, ct)) throw AppException.Invalid(new Dictionary<string, string> { ["fundId"] = "ফান্ড বাছাই করুন" });
        var title = await db.Books.Where(b => b.Id == i.BookId).Select(b => b.Title).FirstAsync(ct);
        var who = i.StudentId is int sid
            ? await db.Students.IgnoreQueryFilters().Where(s => s.Id == sid).Select(s => s.Name + " (" + s.StudentCode + ")").FirstOrDefaultAsync(ct)
            : await db.Staff.IgnoreQueryFilters().Where(s => s.Id == i.StaffId).Select(s => s.Name + " (" + s.StaffCode + ")").FirstOrDefaultAsync(ct);
        i.FinePaid += amount;
        var entry = new LedgerEntry { Date = clock.Today, FundId = fid, CategoryId = FineCategoryId, Direction = Direction.In, Amount = amount,
            Description = $"লাইব্রেরি জরিমানা: {who} — \"{title}\" দেরিতে ফেরত", BookIssueId = i.Id, CreatedBy = user.UserId, CreatedAt = DateTime.UtcNow };
        db.LedgerEntries.Add(entry);
        await db.SaveChangesAsync(ct);
        entry.VoucherNo = $"L-{entry.Id:000000}";
    }

    private static string? Blank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
