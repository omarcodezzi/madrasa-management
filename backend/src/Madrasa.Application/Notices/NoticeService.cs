using Madrasa.Application.Common;
using Madrasa.Domain.Entities;
using Madrasa.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Madrasa.Application.Notices;

public record NoticeDto(int Id, string Title, string Body, DateOnly Date, NoticeAudience Audience, bool IsPublic, string? CreatedBy, int SmsQueued);
public record NoticeInput(string Title, string Body, DateOnly? Date, NoticeAudience Audience, bool IsPublic, bool SendSms);
public record EventDto(int Id, string Title, DateOnly StartDate, DateOnly? EndDate, EventKind Kind);
public record EventInput(string Title, DateOnly StartDate, DateOnly? EndDate, EventKind Kind);
public record NoticeSaved(NoticeDto Notice, int SmsQueued);
public record MessageRow(long Id, string ToPhone, string Body, MessageStatus Status, DateTime? SentAt, int? NoticeId);

/// <summary>Notice board, events calendar, and an SMS queue (no gateway is configured, so messages stay queued).</summary>
public class NoticeService(IAppDbContext db, IClock clock, ICurrentUser user)
{
    public async Task<List<NoticeDto>> ListAsync(bool publicOnly, int take, CancellationToken ct)
    {
        var q = db.Notices.AsNoTracking().AsQueryable();
        if (publicOnly) q = q.Where(n => n.IsPublic);
        var rows = await q.OrderByDescending(n => n.Date).ThenByDescending(n => n.Id).Take(Math.Clamp(take, 1, 200))
            .Select(n => new { n, Sms = db.MessageLogs.Count(m => m.RelatedNoticeId == n.Id) }).ToListAsync(ct);
        var names = new Dictionary<string, string?>();
        foreach (var uid in rows.Select(r => r.n.CreatedBy).Where(x => x != null).Distinct())
            names[uid!] = await db.UserNameAsync(uid, ct);
        return rows.Select(r => new NoticeDto(r.n.Id, r.n.Title, r.n.Body, r.n.Date, r.n.Audience, r.n.IsPublic,
            publicOnly ? null : r.n.CreatedBy is string c ? names.GetValueOrDefault(c) : null, publicOnly ? 0 : r.Sms)).ToList();
    }

    public async Task<NoticeSaved> SaveAsync(int? id, NoticeInput input, CancellationToken ct)
    {
        var err = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(input.Title)) err["title"] = "শিরোনাম লিখুন";
        else if (input.Title.Trim().Length > 200) err["title"] = "শিরোনাম ২০০ অক্ষরের মধ্যে রাখুন";
        if (string.IsNullOrWhiteSpace(input.Body)) err["body"] = "নোটিশের বিবরণ লিখুন";
        if (!Enum.IsDefined(input.Audience)) err["audience"] = "কাদের জন্য, বাছাই করুন";
        if (err.Count > 0) throw AppException.Invalid(err);
        Notice n;
        if (id is int nid) n = await db.Notices.FirstOrDefaultAsync(x => x.Id == nid, ct) ?? throw AppException.NotFound("নোটিশ");
        else { n = new Notice { CreatedBy = user.UserId }; db.Notices.Add(n); }
        n.Title = input.Title.Trim(); n.Body = input.Body.Trim(); n.Date = input.Date ?? clock.Today; n.Audience = input.Audience; n.IsPublic = input.IsPublic;
        await db.SaveChangesAsync(ct);
        var queued = input.SendSms ? await QueueSmsAsync(n, ct) : 0;
        return new NoticeSaved((await ListAsync(false, 200, ct)).First(x => x.Id == n.Id), queued);
    }

    private async Task<int> QueueSmsAsync(Notice n, CancellationToken ct)
    {
        var phones = new List<string>();
        if (n.Audience is NoticeAudience.All or NoticeAudience.Guardians or NoticeAudience.Students)
            phones.AddRange(await db.StudentGuardians.Where(g => g.IsPrimary && g.Student!.Status == StudentStatus.Active).Select(g => g.Guardian!.Phone).ToListAsync(ct));
        if (n.Audience is NoticeAudience.All or NoticeAudience.Staff)
            phones.AddRange(await db.Staff.Where(s => s.IsActive && s.Phone != null).Select(s => s.Phone!).ToListAsync(ct));
        var already = (await db.MessageLogs.Where(m => m.RelatedNoticeId == n.Id).Select(m => m.ToPhone).ToListAsync(ct)).ToHashSet();
        var body = n.Title.Length + n.Body.Length > 300 ? n.Title : $"{n.Title}: {n.Body}";
        var list = phones.Select(Digits.PhoneDigits).Where(p => p.Length == 11 && !already.Contains(p)).Distinct().ToList();
        foreach (var p in list) db.MessageLogs.Add(new MessageLog { Channel = MessageChannel.Sms, ToPhone = p, Body = body, Status = MessageStatus.Queued, RelatedNoticeId = n.Id });
        await db.SaveChangesAsync(ct);
        return list.Count;
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var n = await db.Notices.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw AppException.NotFound("নোটিশ");
        db.MessageLogs.RemoveRange(db.MessageLogs.Where(m => m.RelatedNoticeId == id && m.Status == MessageStatus.Queued));
        db.Notices.Remove(n);
        await db.SaveChangesAsync(ct);
    }

    public Task<List<MessageRow>> MessagesAsync(CancellationToken ct) =>
        db.MessageLogs.AsNoTracking().OrderByDescending(m => m.Id).Take(200)
            .Select(m => new MessageRow(m.Id, m.ToPhone, m.Body, m.Status, m.SentAt, m.RelatedNoticeId)).ToListAsync(ct);

    // ---- events ----
    public async Task<List<EventDto>> EventsAsync(DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        var f = from ?? clock.Today.AddDays(-30); var t = to ?? clock.Today.AddDays(180);
        return await db.Events.AsNoTracking().Where(e => e.StartDate <= t && (e.EndDate ?? e.StartDate) >= f).OrderBy(e => e.StartDate)
            .Select(e => new EventDto(e.Id, e.Title, e.StartDate, e.EndDate, e.Kind)).ToListAsync(ct);
    }

    public async Task<EventDto> SaveEventAsync(int? id, EventInput input, CancellationToken ct)
    {
        var err = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(input.Title)) err["title"] = "ইভেন্টের নাম লিখুন";
        if (input.EndDate is DateOnly e && e < input.StartDate) err["endDate"] = "শেষ তারিখ শুরুর আগে হতে পারে না";
        if (!Enum.IsDefined(input.Kind)) err["kind"] = "ধরন বাছাই করুন";
        if (err.Count > 0) throw AppException.Invalid(err);
        SchoolEvent ev;
        if (id is int eid) ev = await db.Events.FirstOrDefaultAsync(x => x.Id == eid, ct) ?? throw AppException.NotFound("ইভেন্ট");
        else { ev = new SchoolEvent(); db.Events.Add(ev); }
        ev.Title = input.Title.Trim(); ev.StartDate = input.StartDate; ev.EndDate = input.EndDate == input.StartDate ? null : input.EndDate; ev.Kind = input.Kind;
        await db.SaveChangesAsync(ct);
        return new EventDto(ev.Id, ev.Title, ev.StartDate, ev.EndDate, ev.Kind);
    }

    public async Task DeleteEventAsync(int id, CancellationToken ct)
    {
        var ev = await db.Events.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw AppException.NotFound("ইভেন্ট");
        db.Events.Remove(ev);
        await db.SaveChangesAsync(ct);
    }
}
