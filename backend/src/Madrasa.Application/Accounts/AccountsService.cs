using Madrasa.Application.Common;
using Madrasa.Domain.Entities;
using Madrasa.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Madrasa.Application.Accounts;

public record FundBalance(int Id, string NameBn, string Code, bool IsRestricted, decimal In, decimal Out, decimal Balance, decimal InThisMonth, decimal OutThisMonth);
public record CategoryDto(int Id, string NameBn, LedgerKind Kind);
public record LedgerRow(long Id, DateOnly Date, int FundId, string Fund, int CategoryId, string Category, LedgerKind Kind, Direction Direction, decimal Amount,
    string? Description, string? VoucherNo, string? Donor, string? DonorPhone, string Source, bool CanDelete);
public record LedgerPage(IReadOnlyList<LedgerRow> Items, int Total, int Page, int PageSize, decimal TotalIn, decimal TotalOut);
public record LedgerQuery(DateOnly? From, DateOnly? To, int? FundId, int? CategoryId, Direction? Direction, string? Q, int Page = 1, int PageSize = 50);
/// <summary>A manual voucher. Direction follows the category (income → in, expense → out).</summary>
public record EntryInput(DateOnly? Date, int FundId, int CategoryId, decimal Amount, string? Description, string? DonorName, string? DonorPhone, string? DonorAddress);
public record DonorDto(int Id, string Name, string? Phone, string? Address, decimal Total, int Count, DateOnly? Last);
public record CategoryTotal(string Category, LedgerKind Kind, decimal Amount);
public record AccountsSummary(string Month, decimal Income, decimal Expense, decimal TodayIn, decimal TodayOut, IReadOnlyList<FundBalance> Funds, IReadOnlyList<CategoryTotal> ByCategory);

/// <summary>Income/expense ledger with separate funds (Zakat, Sadaqah, Lillah, General) and donors.</summary>
public class AccountsService(IAppDbContext db, IClock clock, ICurrentUser user)
{
    public async Task<List<FundBalance>> FundsAsync(CancellationToken ct)
    {
        var first = new DateOnly(clock.Today.Year, clock.Today.Month, 1);
        var sums = await db.LedgerEntries.GroupBy(l => l.FundId).Select(g => new
        {
            g.Key,
            In = g.Where(l => l.Direction == Direction.In).Sum(l => (decimal?)l.Amount) ?? 0,
            Out = g.Where(l => l.Direction == Direction.Out).Sum(l => (decimal?)l.Amount) ?? 0,
            InM = g.Where(l => l.Direction == Direction.In && l.Date >= first).Sum(l => (decimal?)l.Amount) ?? 0,
            OutM = g.Where(l => l.Direction == Direction.Out && l.Date >= first).Sum(l => (decimal?)l.Amount) ?? 0,
        }).ToListAsync(ct);
        var funds = await db.Funds.AsNoTracking().OrderBy(f => f.Id).ToListAsync(ct);
        return funds.Select(f =>
        {
            var s = sums.FirstOrDefault(x => x.Key == f.Id);
            return new FundBalance(f.Id, f.NameBn, f.Code, f.IsRestricted, s?.In ?? 0, s?.Out ?? 0, (s?.In ?? 0) - (s?.Out ?? 0), s?.InM ?? 0, s?.OutM ?? 0);
        }).ToList();
    }

    public Task<List<CategoryDto>> CategoriesAsync(CancellationToken ct) =>
        db.LedgerCategories.AsNoTracking().OrderBy(c => c.Kind).ThenBy(c => c.Id).Select(c => new CategoryDto(c.Id, c.NameBn, c.Kind)).ToListAsync(ct);

    public async Task<LedgerPage> LedgerAsync(LedgerQuery q, CancellationToken ct)
    {
        var query = db.LedgerEntries.AsNoTracking().AsQueryable();
        if (q.From is DateOnly f) query = query.Where(l => l.Date >= f);
        if (q.To is DateOnly t) query = query.Where(l => l.Date <= t);
        if (q.FundId is int fid) query = query.Where(l => l.FundId == fid);
        if (q.CategoryId is int cid) query = query.Where(l => l.CategoryId == cid);
        if (q.Direction is Direction d) query = query.Where(l => l.Direction == d);
        if (!string.IsNullOrWhiteSpace(q.Q))
        {
            var term = q.Q.Trim();
            query = query.Where(l => (l.Description != null && l.Description.Contains(term)) || (l.VoucherNo != null && l.VoucherNo.Contains(term))
                || db.Donors.Any(x => x.Id == l.DonorId && (x.Name.Contains(term) || (x.Phone != null && x.Phone.Contains(term)))));
        }
        var total = await query.CountAsync(ct);
        var tin = await query.Where(l => l.Direction == Direction.In).SumAsync(l => (decimal?)l.Amount, ct) ?? 0;
        var tout = await query.Where(l => l.Direction == Direction.Out).SumAsync(l => (decimal?)l.Amount, ct) ?? 0;
        var size = Math.Clamp(q.PageSize, 1, 500);
        var page = Math.Max(1, q.Page);
        var items = await query.OrderByDescending(l => l.Date).ThenByDescending(l => l.Id).Skip((page - 1) * size).Take(size)
            .Select(l => new
            {
                l.Id, l.Date, l.FundId, Fund = l.Fund!.NameBn, l.CategoryId, Category = l.Category!.NameBn, l.Category.Kind, l.Direction, l.Amount, l.Description, l.VoucherNo,
                Donor = db.Donors.Where(x => x.Id == l.DonorId).Select(x => new { x.Name, x.Phone }).FirstOrDefault(), l.FeeReceiptId, l.SalaryPaymentId, l.BookIssueId,
            }).ToListAsync(ct);
        return new LedgerPage(items.Select(l => new LedgerRow(l.Id, l.Date, l.FundId, l.Fund, l.CategoryId, l.Category, l.Kind, l.Direction, l.Amount, l.Description, l.VoucherNo,
            l.Donor?.Name, l.Donor?.Phone, l.FeeReceiptId != null ? "Fee" : l.SalaryPaymentId != null ? "Salary" : l.BookIssueId != null ? "Library" : "Manual",
            l.FeeReceiptId == null && l.SalaryPaymentId == null)).ToList(), total, page, size, tin, tout);
    }

    public async Task<LedgerRow> AddAsync(EntryInput input, CancellationToken ct)
    {
        var err = new Dictionary<string, string>();
        var cat = await db.LedgerCategories.FirstOrDefaultAsync(c => c.Id == input.CategoryId, ct);
        var fund = await db.Funds.FirstOrDefaultAsync(f => f.Id == input.FundId, ct);
        if (cat is null) err["categoryId"] = "খাত বাছাই করুন";
        if (fund is null) err["fundId"] = "ফান্ড বাছাই করুন";
        if (input.Amount <= 0) err["amount"] = "টাকার পরিমাণ লিখুন";
        else if (input.Amount > 100_000_000) err["amount"] = "পরিমাণ খুব বেশি";
        var date = input.Date ?? clock.Today;
        if (date > clock.Today) err["date"] = "ভবিষ্যতের তারিখে এন্ট্রি দেওয়া যাবে না";
        if (cat?.Id == 1) err["categoryId"] = "ছাত্র বেতন বেতন-ফি পাতা থেকে আদায় করুন, যাতে রসিদ তৈরি হয়";
        if (cat?.Id == Library.LibraryService.FineCategoryId) err["categoryId"] = "লাইব্রেরির জরিমানা লাইব্রেরি পাতা থেকে আদায় করুন";
        if (cat is { Kind: LedgerKind.Expense } && string.IsNullOrWhiteSpace(input.Description)) err["description"] = "খরচের বিবরণ লিখুন";
        if (input.DonorPhone is { Length: > 0 } ph && !Digits.IsBdMobile(ph)) err["donorPhone"] = "সঠিক মোবাইল নম্বর দিন (০১ দিয়ে শুরু, ১১ সংখ্যা)";
        if (err.Count > 0) throw AppException.Invalid(err);

        var dir = cat!.Kind == LedgerKind.Income ? Direction.In : Direction.Out;
        // Restricted money (Zakat, Lillah…) and the general fund must never be overspent
        if (dir == Direction.Out)
        {
            var bal = await BalanceAsync(fund!.Id, ct);
            if (input.Amount > bal) throw AppException.Invalid(new Dictionary<string, string> { ["amount"] = $"{fund.NameBn}-এ আছে মাত্র {Digits.Money(bal)} টাকা" });
        }

        int? donorId = null;
        if (!string.IsNullOrWhiteSpace(input.DonorName))
        {
            var phone = string.IsNullOrWhiteSpace(input.DonorPhone) ? null : Digits.PhoneDigits(input.DonorPhone);
            var name = input.DonorName.Trim();
            var donor = phone is null ? await db.Donors.FirstOrDefaultAsync(x => x.Name == name && x.Phone == null, ct)
                                      : await db.Donors.FirstOrDefaultAsync(x => x.Phone == phone, ct);
            if (donor is null) { donor = new Donor { Name = name, Phone = phone, Address = input.DonorAddress?.Trim() }; db.Donors.Add(donor); await db.SaveChangesAsync(ct); }
            donorId = donor.Id;
        }

        var entry = new LedgerEntry { Date = date, FundId = fund!.Id, CategoryId = cat.Id, Direction = dir, Amount = input.Amount, Description = input.Description?.Trim(),
            DonorId = donorId, CreatedBy = user.UserId, CreatedAt = DateTime.UtcNow };
        db.LedgerEntries.Add(entry);
        await db.SaveChangesAsync(ct);
        // Voucher numbers: D-000123 for money in, V-000123 for money out
        entry.VoucherNo = $"{(dir == Direction.In ? "D" : "V")}-{entry.Id:000000}";
        await db.SaveChangesAsync(ct);
        return (await LedgerAsync(new LedgerQuery(date, date, null, null, null, entry.VoucherNo), ct)).Items.First(x => x.Id == entry.Id);
    }

    public async Task DeleteAsync(long id, CancellationToken ct)
    {
        var e = await db.LedgerEntries.FirstOrDefaultAsync(l => l.Id == id, ct) ?? throw AppException.NotFound("এন্ট্রি");
        if (e.FeeReceiptId != null) throw new AppException("বেতনের এন্ট্রি এখান থেকে মুছা যাবে না। রসিদ বাতিল করুন।", 409);
        if (e.SalaryPaymentId != null) throw new AppException("বেতন প্রদানের এন্ট্রি স্টাফ পাতা থেকে বাতিল করুন।", 409);
        if (e.Direction == Direction.In && await BalanceAsync(e.FundId, ct) < e.Amount)
            throw new AppException("এই টাকা থেকে খরচ হয়ে গেছে, তাই এন্ট্রিটি মুছলে ফান্ড ঋণাত্মক হবে।", 409);
        // Deleting a collected library fine puts it back on the borrower's pending list
        if (e.BookIssueId is int iid && await db.BookIssues.FirstOrDefaultAsync(i => i.Id == iid, ct) is { } issue)
            issue.FinePaid = Math.Max(0, issue.FinePaid - e.Amount);
        db.LedgerEntries.Remove(e);
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<DonorDto>> DonorsAsync(string? q, CancellationToken ct)
    {
        var query = db.Donors.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) { var t = q.Trim(); query = query.Where(d => d.Name.Contains(t) || (d.Phone != null && d.Phone.Contains(t))); }
        var rows = await query.Select(d => new { d.Id, d.Name, d.Phone, d.Address,
                Total = db.LedgerEntries.Where(l => l.DonorId == d.Id && l.Direction == Direction.In).Sum(l => (decimal?)l.Amount) ?? 0,
                Count = db.LedgerEntries.Count(l => l.DonorId == d.Id),
                Last = db.LedgerEntries.Where(l => l.DonorId == d.Id).Max(l => (DateOnly?)l.Date) })
            .OrderByDescending(d => d.Total).Take(300).ToListAsync(ct);
        return rows.Select(d => new DonorDto(d.Id, d.Name, d.Phone, d.Address, d.Total, d.Count, d.Last)).ToList();
    }

    public async Task<AccountsSummary> SummaryAsync(string? month, CancellationToken ct)
    {
        var m = Months.IsValid(month) ? month! : Months.Of(clock.Today);
        var from = Months.First(m); var to = from.AddMonths(1).AddDays(-1);
        var rows = await db.LedgerEntries.AsNoTracking().Where(l => l.Date >= from && l.Date <= to)
            .Select(l => new { l.Category!.NameBn, l.Category.Kind, l.Direction, l.Amount }).ToListAsync(ct);
        // Net by category kind: a voided fee receipt is an "out" under an income category
        decimal Net(LedgerKind k) => rows.Where(r => r.Kind == k).Sum(r => (r.Direction == Direction.In) == (k == LedgerKind.Income) ? r.Amount : -r.Amount);
        var byCat = rows.GroupBy(r => (r.NameBn, r.Kind)).Select(g => new CategoryTotal(g.Key.NameBn, g.Key.Kind,
            g.Sum(r => (r.Direction == Direction.In) == (g.Key.Kind == LedgerKind.Income) ? r.Amount : -r.Amount))).OrderBy(c => c.Kind).ThenByDescending(c => c.Amount).ToList();
        var today = clock.Today;
        var t = await db.LedgerEntries.Where(l => l.Date == today).GroupBy(l => l.Direction).Select(g => new { g.Key, S = g.Sum(l => l.Amount) }).ToListAsync(ct);
        return new AccountsSummary(m, Net(LedgerKind.Income), Net(LedgerKind.Expense),
            t.FirstOrDefault(x => x.Key == Direction.In)?.S ?? 0, t.FirstOrDefault(x => x.Key == Direction.Out)?.S ?? 0, await FundsAsync(ct), byCat);
    }

    public async Task<decimal> BalanceAsync(int fundId, CancellationToken ct) =>
        await db.LedgerEntries.Where(l => l.FundId == fundId).SumAsync(l => (decimal?)(l.Direction == Direction.In ? l.Amount : -l.Amount), ct) ?? 0;
}
