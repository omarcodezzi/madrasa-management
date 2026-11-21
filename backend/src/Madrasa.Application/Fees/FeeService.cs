using Madrasa.Application.Common;
using Madrasa.Domain.Entities;
using Madrasa.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Madrasa.Application.Fees;

public class FeeService(IAppDbContext db, IClock clock, ICurrentUser user)
{
    private const int TuitionHeadId = 2;   // seeded "মাসিক বেতন"
    private const int BoardingHeadId = 4;  // seeded "বোর্ডিং ফি" (residential students)
    private const int FeeIncomeCategoryId = 1;
    private const string GeneralFund = "GENERAL";

    public Task<List<FeeHeadDto>> HeadsAsync(CancellationToken ct) =>
        db.FeeHeads.AsNoTracking().Where(h => h.IsActive).OrderBy(h => h.Id)
            .Select(h => new FeeHeadDto(h.Id, h.NameBn, h.Frequency, h.DefaultAmount)).ToListAsync(ct);

    /// <summary>
    /// Creates the missing monthly tuition (and boarding) invoices for active students up to <paramref name="upTo"/>.
    /// Runs on demand before any dues query, so there is no background job to forget.
    /// </summary>
    public async Task EnsureMonthlyInvoicesAsync(string upTo, CancellationToken ct, int? studentId = null)
    {
        var session = await db.AcademicSessions.FirstOrDefaultAsync(s => s.IsCurrent, ct) ?? throw new AppException("চলতি শিক্ষাবর্ষ নেই");
        var last = Months.First(upTo);
        if (last > session.EndDate) last = new DateOnly(session.EndDate.Year, session.EndDate.Month, 1);
        var heads = await db.FeeHeads.Where(h => h.Id == TuitionHeadId || h.Id == BoardingHeadId).ToDictionaryAsync(h => h.Id, ct);
        var q = db.Enrollments.Where(e => e.SessionId == session.Id && e.Student!.Status == StudentStatus.Active && !e.Student.IsDeleted);
        if (studentId is not null) q = q.Where(e => e.StudentId == studentId);
        var enrolls = await q.Select(e => new { e.StudentId, e.Student!.AdmissionDate, e.Student.IsResidential, e.MonthlyFeeOverride, ClassFee = e.Class!.MonthlyFee }).ToListAsync(ct);
        if (enrolls.Count == 0) return;
        var ids = enrolls.Select(e => e.StudentId).ToList();
        var have = (await db.FeeInvoices.Where(i => i.SessionId == session.Id && ids.Contains(i.StudentId) && (i.FeeHeadId == TuitionHeadId || i.FeeHeadId == BoardingHeadId))
            .Select(i => new { i.StudentId, i.FeeHeadId, i.Month }).ToListAsync(ct)).Select(x => (x.StudentId, x.FeeHeadId, x.Month)).ToHashSet();
        var discounts = await db.StudentDiscounts.Where(d => ids.Contains(d.StudentId)).ToListAsync(ct);

        foreach (var e in enrolls)
        {
            var start = e.AdmissionDate > session.StartDate ? e.AdmissionDate : session.StartDate;
            foreach (var m in Months.Range(start, last))
            {
                AddIfMissing(e.StudentId, TuitionHeadId, m, e.MonthlyFeeOverride ?? e.ClassFee ?? heads.GetValueOrDefault(TuitionHeadId)?.DefaultAmount ?? 0);
                if (e.IsResidential && heads.TryGetValue(BoardingHeadId, out var b) && b.IsActive) AddIfMissing(e.StudentId, BoardingHeadId, m, b.DefaultAmount);
            }
        }
        await db.SaveChangesAsync(ct);

        void AddIfMissing(int sid, int headId, string month, decimal amount)
        {
            if (amount <= 0 || !have.Add((sid, headId, month))) return;
            var first = Months.First(month);
            var disc = discounts.Where(d => d.StudentId == sid && d.FeeHeadId == headId && d.ValidFrom <= first.AddMonths(1).AddDays(-1) && (d.ValidTo == null || d.ValidTo >= first))
                .Sum(d => d.Kind == DiscountKind.Percent ? Math.Round(amount * d.Value / 100, 0) : d.Value);
            db.FeeInvoices.Add(new FeeInvoice { StudentId = sid, SessionId = session.Id, FeeHeadId = headId, Month = month, Amount = amount,
                Discount = Math.Min(disc, amount), DueDate = first.AddDays(9) });
        }
    }

    public async Task<StudentDues> StudentDuesAsync(int studentId, CancellationToken ct)
    {
        await EnsureMonthlyInvoicesAsync(Months.Of(clock.Today), ct, studentId);
        var sessionId = await db.CurrentSessionIdAsync(ct);
        var s = await db.Students.AsNoTracking().Where(x => x.Id == studentId)
            .Select(x => new { x.Id, x.StudentCode, x.Name, x.Phone, E = x.Enrollments.Where(e => e.SessionId == sessionId).Select(e => new { e.Class!.NameBn, e.Roll }).FirstOrDefault() })
            .FirstOrDefaultAsync(ct) ?? throw AppException.NotFound("শিক্ষার্থী");
        var items = await OpenInvoicesQuery().Where(i => i.StudentId == studentId).ToListAsync(ct);
        var dueItems = items.Where(i => i.Due > 0).OrderBy(i => i.Month).ThenBy(i => i.FeeHeadId)
            .Select(i => new DueItem(i.InvoiceId, i.FeeHeadId, i.FeeHead, i.Month, i.Amount, i.Discount, i.Paid, i.Due)).ToList();
        var recent = await ReceiptSummariesAsync(db.FeeReceipts.Where(r => r.StudentId == studentId).OrderByDescending(r => r.ReceiptNo).Take(10), ct);
        return new StudentDues(s.Id, s.StudentCode, s.Name, s.E?.NameBn, s.E?.Roll, s.Phone, dueItems.Sum(i => i.Due), dueItems, recent);
    }

    public async Task<ReceiptDto> CollectAsync(CollectRequest req, CancellationToken ct)
    {
        var err = new Dictionary<string, string>();
        if (req.Lines.Count == 0) err["lines"] = "অন্তত একটি খাত বাছাই করুন";
        if (req.Lines.Any(l => l.Amount <= 0)) err["lines"] = "টাকার পরিমাণ শূন্যের বেশি হতে হবে";
        var date = req.Date ?? clock.Today;
        if (date > clock.Today) err["date"] = "ভবিষ্যতের তারিখে রসিদ কাটা যায় না";
        if (req.Method != PaymentMethod.Cash && string.IsNullOrWhiteSpace(req.Reference)) err["reference"] = "লেনদেন নম্বর (TrxID) লিখুন";
        if (err.Count > 0) throw AppException.Invalid(err);
        if (!await db.Students.AnyAsync(s => s.Id == req.StudentId, ct)) throw AppException.NotFound("শিক্ষার্থী");

        var invIds = req.Lines.Where(l => l.InvoiceId is not null).Select(l => l.InvoiceId!.Value).ToList();
        var open = await OpenInvoicesQuery().Where(i => invIds.Contains(i.InvoiceId)).ToDictionaryAsync(i => i.InvoiceId, ct);
        var receipt = new FeeReceipt { StudentId = req.StudentId, Date = date, Method = req.Method, Reference = req.Reference?.Trim(), CollectedBy = user.UserId };
        foreach (var l in req.Lines)
        {
            if (l.InvoiceId is { } iid)
            {
                if (!open.TryGetValue(iid, out var inv) || inv.Due <= 0) throw new AppException("এই বকেয়াটি আগেই পরিশোধ হয়েছে। পাতাটি আবার খুলুন।", 409);
                if (await db.FeeInvoices.Where(i => i.Id == iid).Select(i => i.StudentId).FirstAsync(ct) != req.StudentId) throw new AppException("ভুল শিক্ষার্থীর বকেয়া", 400);
                if (l.Amount > inv.Due) throw new AppException($"{inv.FeeHead} {inv.Month}: বকেয়ার চেয়ে বেশি টাকা ({inv.Due:0}) দেওয়া যায় না");
                receipt.Lines.Add(new FeeReceiptLine { InvoiceId = iid, FeeHeadId = inv.FeeHeadId, Month = inv.Month, Amount = l.Amount });
            }
            else
            {
                if (l.FeeHeadId is null || !await db.FeeHeads.AnyAsync(h => h.Id == l.FeeHeadId && h.IsActive, ct)) throw new AppException("খাত বাছাই করুন");
                if (l.Month is not null && !Months.IsValid(l.Month)) throw new AppException("মাস ঠিক নয়");
                receipt.Lines.Add(new FeeReceiptLine { FeeHeadId = l.FeeHeadId.Value, Month = l.Month, Amount = l.Amount });
            }
        }
        receipt.TotalPaid = receipt.Lines.Sum(x => x.Amount);
        var fundId = await db.Funds.Where(f => f.Code == GeneralFund).Select(f => f.Id).FirstAsync(ct);

        var ledger = new LedgerEntry { Date = date, FundId = fundId, CategoryId = FeeIncomeCategoryId, Direction = Direction.In, Amount = receipt.TotalPaid,
            Description = "ছাত্র বেতন ও ফি", FeeReceipt = receipt, CreatedBy = user.UserId, CreatedAt = DateTime.UtcNow };
        db.FeeReceipts.Add(receipt);
        db.LedgerEntries.Add(ledger);

        // Receipt numbers are unique; if two counters save at the same moment, take the next number and retry
        for (var attempt = 0; ; attempt++)
        {
            receipt.ReceiptNo = (await db.FeeReceipts.MaxAsync(r => (int?)r.ReceiptNo, ct) ?? 0) + 1;
            ledger.VoucherNo = $"R-{receipt.ReceiptNo}";
            try { await db.SaveChangesAsync(ct); break; }
            catch (DbUpdateException) when (attempt < 3) { }
        }
        return await ReceiptAsync(receipt.ReceiptNo, ct);
    }

    public async Task<ReceiptDto> ReceiptAsync(int receiptNo, CancellationToken ct)
    {
        var sessionId = await db.CurrentSessionIdAsync(ct);
        var r = await db.FeeReceipts.AsNoTracking().Where(x => x.ReceiptNo == receiptNo)
            .Select(x => new
            {
                x.Id, x.ReceiptNo, x.Date, x.StudentId, x.Student!.StudentCode, x.Student.Name, x.Student.FatherName, x.TotalPaid, x.Method, x.Reference, x.IsVoided, x.CollectedBy,
                E = x.Student.Enrollments.Where(e => e.SessionId == sessionId).Select(e => new { e.Class!.NameBn, e.Roll }).FirstOrDefault(),
                Lines = x.Lines.Select(l => new ReceiptLineDto(l.FeeHead!.NameBn, l.Month, l.Amount)).ToList(),
            }).FirstOrDefaultAsync(ct) ?? throw AppException.NotFound("রসিদ");
        var remaining = (await OpenInvoicesQuery().Where(i => i.StudentId == r.StudentId).Select(i => i.Due).ToListAsync(ct)).Where(d => d > 0).Sum();
        return new ReceiptDto(r.Id, r.ReceiptNo, r.Date, r.StudentId, r.StudentCode, r.Name, r.FatherName, r.E?.NameBn, r.E?.Roll, r.TotalPaid, r.Method, r.Reference,
            r.IsVoided, r.CollectedBy, r.Lines.OrderBy(l => l.Month).ToList(), remaining);
    }

    public async Task<ReceiptDto> VoidAsync(int receiptNo, CancellationToken ct)
    {
        var r = await db.FeeReceipts.FirstOrDefaultAsync(x => x.ReceiptNo == receiptNo, ct) ?? throw AppException.NotFound("রসিদ");
        if (r.IsVoided) throw new AppException("রসিদটি আগেই বাতিল করা হয়েছে", 409);
        r.IsVoided = true;
        var fundId = await db.Funds.Where(f => f.Code == GeneralFund).Select(f => f.Id).FirstAsync(ct);
        // Keep the books honest: reverse instead of deleting
        db.LedgerEntries.Add(new LedgerEntry { Date = clock.Today, FundId = fundId, CategoryId = FeeIncomeCategoryId, Direction = Direction.Out, Amount = r.TotalPaid,
            Description = $"বাতিল রসিদ নং {r.ReceiptNo}", VoucherNo = $"R-{r.ReceiptNo}-V", FeeReceiptId = r.Id, CreatedBy = user.UserId, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync(ct);
        return await ReceiptAsync(receiptNo, ct);
    }

    public async Task<PagedResult<ReceiptSummary>> ReceiptsAsync(string? month, string? q, int page, int pageSize, CancellationToken ct)
    {
        var query = db.FeeReceipts.AsNoTracking();
        if (Months.IsValid(month)) { var f = Months.First(month!); var t = f.AddMonths(1); query = query.Where(r => r.Date >= f && r.Date < t); }
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = Digits.ToEnglish(q.Trim())!;
            int.TryParse(term, out var no);
            query = query.Where(r => r.Student!.Name.Contains(term) || r.Student.StudentCode.Contains(term) || (no > 0 && r.ReceiptNo == no));
        }
        pageSize = Math.Clamp(pageSize, 1, 200);
        var total = await query.CountAsync(ct);
        var items = await ReceiptSummariesAsync(query.OrderByDescending(r => r.ReceiptNo).Skip((Math.Max(page, 1) - 1) * pageSize).Take(pageSize), ct);
        return new PagedResult<ReceiptSummary>(items, total, page, pageSize);
    }

    /// <summary>Who owes what, up to and including the given month.</summary>
    public async Task<List<DueListRow>> DueListAsync(string? month, int? classId, CancellationToken ct)
    {
        var ym = Months.IsValid(month) ? month! : Months.Of(clock.Today);
        await EnsureMonthlyInvoicesAsync(ym, ct);
        var sessionId = await db.CurrentSessionIdAsync(ct);
        // SQL Server cannot aggregate over the "paid" subquery, so group the open invoices in memory (a few thousand rows at most)
        var open = await OpenInvoicesQuery().Where(i => i.Due > 0 && (i.Month == null || string.Compare(i.Month, ym) <= 0)).ToListAsync(ct);
        var grouped = open.GroupBy(i => i.StudentId)
            .Select(g => new { StudentId = g.Key, Total = g.Sum(i => i.Due), Months = g.Where(i => i.Month != null).Select(i => i.Month).Distinct().Count(), Oldest = g.Min(i => i.Month) })
            .ToList();
        var ids = grouped.Select(g => g.StudentId).ToList();
        var stuQ = db.Students.AsNoTracking().Where(s => ids.Contains(s.Id) && s.Status == StudentStatus.Active);
        if (classId is not null) stuQ = stuQ.Where(s => s.Enrollments.Any(e => e.SessionId == sessionId && e.ClassId == classId));
        var stu = await stuQ.Select(s => new
        {
            s.Id, s.StudentCode, s.Name,
            Phone = s.Guardians.Where(g => g.IsPrimary).Select(g => g.Guardian!.Phone).FirstOrDefault() ?? s.Phone,
            E = s.Enrollments.Where(e => e.SessionId == sessionId).Select(e => new { e.Class!.NameBn, e.Class.SortOrder, e.Roll }).FirstOrDefault(),
        }).ToListAsync(ct);
        var byId = grouped.ToDictionary(g => g.StudentId);
        return stu.OrderBy(s => s.E?.SortOrder).ThenBy(s => s.E?.Roll)
            .Select(s => new DueListRow(s.Id, s.StudentCode, s.Name, s.E?.NameBn, s.E?.Roll, s.Phone, byId[s.Id].Total, byId[s.Id].Months, byId[s.Id].Oldest))
            .ToList();
    }

    public async Task<MonthSummary> MonthSummaryAsync(string? month, CancellationToken ct)
    {
        var ym = Months.IsValid(month) ? month! : Months.Of(clock.Today);
        await EnsureMonthlyInvoicesAsync(ym, ct);
        var inv = await OpenInvoicesQuery().Where(i => i.Month == ym && i.FeeHeadId == TuitionHeadId).ToListAsync(ct);
        var f = Months.First(ym); var t = f.AddMonths(1);
        var inMonth = await db.FeeReceipts.Where(r => !r.IsVoided && r.Date >= f && r.Date < t).SumAsync(r => (decimal?)r.TotalPaid, ct) ?? 0;
        return new MonthSummary(ym, inv.Sum(i => i.Amount - i.Discount), inv.Sum(i => i.Paid), inMonth, inv.Count(i => i.Due <= 0), inv.Count(i => i.Due > 0));
    }

    // ---------- queries ----------

    private IQueryable<DueItemRow> OpenInvoicesQuery() =>
        db.FeeInvoices.AsNoTracking().Select(i => new
        {
            i,
            Paid = db.FeeReceiptLines.Where(l => l.InvoiceId == i.Id && !db.FeeReceipts.Any(r => r.Id == l.ReceiptId && r.IsVoided)).Sum(l => (decimal?)l.Amount) ?? 0,
        }).Select(x => new DueItemRow
        {
            InvoiceId = x.i.Id, StudentId = x.i.StudentId, FeeHeadId = x.i.FeeHeadId, FeeHead = x.i.FeeHead!.NameBn, Month = x.i.Month,
            Amount = x.i.Amount, Discount = x.i.Discount, Paid = x.Paid, Due = x.i.Amount - x.i.Discount - x.Paid,
        });

    /// <summary>Order and page the receipts first; the month list is joined in memory.</summary>
    private static async Task<List<ReceiptSummary>> ReceiptSummariesAsync(IQueryable<FeeReceipt> q, CancellationToken ct)
    {
        var rows = await q.Select(r => new
        {
            r.Id, r.ReceiptNo, r.Date, r.StudentId, r.Student!.Name, r.TotalPaid, r.Method, r.IsVoided,
            ClassName = r.Student.Enrollments.OrderByDescending(e => e.SessionId).Select(e => e.Class!.NameBn).FirstOrDefault(),
            Months = r.Lines.Where(l => l.Month != null).Select(l => l.Month!).ToList(),
        }).ToListAsync(ct);
        return rows.Select(r => new ReceiptSummary(r.Id, r.ReceiptNo, r.Date, r.StudentId, r.Name, r.ClassName, r.TotalPaid, r.Method, r.IsVoided,
            string.Join(", ", r.Months.Distinct().Order()))).ToList();
    }
}

/// <summary>Projection row: an invoice with what has been paid against it (voided receipts excluded).</summary>
public class DueItemRow
{
    public int InvoiceId { get; set; }
    public int StudentId { get; set; }
    public int FeeHeadId { get; set; }
    public string FeeHead { get; set; } = "";
    public string? Month { get; set; }
    public decimal Amount { get; set; }
    public decimal Discount { get; set; }
    public decimal Paid { get; set; }
    public decimal Due { get; set; }
}
