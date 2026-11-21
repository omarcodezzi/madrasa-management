using Madrasa.Application.Common;
using Madrasa.Domain.Entities;
using Madrasa.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Madrasa.Application.Hostel;

public record Occupant(int AllocationId, int StudentId, string StudentCode, string Name, string? PhotoPath, string? ClassName, int SeatNo, DateOnly FromDate, bool OnLeave);
public record RoomDto(int Id, string? Building, string RoomNo, int Seats, Gender ForGender, IReadOnlyList<Occupant> Occupants);
public record RoomInput(string? Building, string RoomNo, int Seats, Gender ForGender);
public record AllocateInput(int StudentId, int RoomId, int? SeatNo, DateOnly? FromDate);
public record LeaveRow(int Id, int StudentId, string StudentCode, string Name, string? ClassName, string? RoomNo, DateOnly FromDate, DateOnly? ToDate,
    string? Reason, string? TakenBy, DateOnly? ReturnedOn, bool Overdue);
public record LeaveInput(int StudentId, DateOnly FromDate, DateOnly? ToDate, string? Reason, string? TakenBy);
public record HostelSummary(int Rooms, int Seats, int Occupied, int OnLeave, int Overdue, IReadOnlyList<RoomDto> RoomList);
public record Resident(int StudentId, string StudentCode, string Name, string? ClassName, Gender Gender, string? RoomNo);

/// <summary>Hostel rooms, seat allocation and the leave register.</summary>
public class HostelService(IAppDbContext db, IClock clock)
{
    public async Task<HostelSummary> SummaryAsync(CancellationToken ct)
    {
        var today = clock.Today;
        var sessionId = await db.CurrentSessionIdAsync(ct);
        var rooms = await db.HostelRooms.AsNoTracking().OrderBy(r => r.Building).ThenBy(r => r.RoomNo).ToListAsync(ct);
        var onLeave = await db.HostelLeaves.Where(l => l.ReturnedOn == null && l.FromDate <= today).Select(l => l.StudentId).ToListAsync(ct);
        var occ = await db.HostelAllocations.AsNoTracking().Where(a => a.ToDate == null)
            .Join(db.Students, a => a.StudentId, s => s.Id, (a, s) => new { a, s })
            .Select(x => new { x.a.RoomId, x.a.Id, x.s, x.a.SeatNo, x.a.FromDate,
                ClassName = db.Enrollments.Where(e => e.StudentId == x.s.Id && e.SessionId == sessionId).Select(e => e.Class!.NameBn).FirstOrDefault() })
            .ToListAsync(ct);
        var leaveSet = onLeave.ToHashSet();
        var list = rooms.Select(r => new RoomDto(r.Id, r.Building, r.RoomNo, r.Seats, r.ForGender,
            occ.Where(o => o.RoomId == r.Id).OrderBy(o => o.SeatNo)
               .Select(o => new Occupant(o.Id, o.s.Id, o.s.StudentCode, o.s.Name, o.s.PhotoPath, o.ClassName, o.SeatNo, o.FromDate, leaveSet.Contains(o.s.Id))).ToList())).ToList();
        var overdue = await db.HostelLeaves.CountAsync(l => l.ReturnedOn == null && l.ToDate != null && l.ToDate < today, ct);
        return new HostelSummary(rooms.Count, rooms.Sum(r => r.Seats), occ.Count, leaveSet.Count, overdue, list);
    }

    public async Task<RoomDto> SaveRoomAsync(int? id, RoomInput input, CancellationToken ct)
    {
        var err = new Dictionary<string, string>();
        var no = (Digits.ToEnglish(input.RoomNo) ?? "").Trim();
        if (no.Length == 0) err["roomNo"] = "রুম নম্বর লিখুন";
        if (input.Seats is < 1 or > 100) err["seats"] = "সিট সংখ্যা ১ থেকে ১০০";
        if (!Enum.IsDefined(input.ForGender)) err["forGender"] = "ছাত্র না ছাত্রী, বাছাই করুন";
        var building = string.IsNullOrWhiteSpace(input.Building) ? null : input.Building.Trim();
        if (no.Length > 0 && await db.HostelRooms.AnyAsync(r => r.Id != id && r.RoomNo == no && r.Building == building, ct)) err["roomNo"] = "এই নম্বরের রুম আগেই আছে";
        HostelRoom? room = null;
        if (id is int rid)
        {
            room = await db.HostelRooms.FirstOrDefaultAsync(r => r.Id == rid, ct) ?? throw AppException.NotFound("রুম");
            var used = await db.HostelAllocations.Where(a => a.RoomId == rid && a.ToDate == null).Select(a => a.SeatNo).ToListAsync(ct);
            if (used.Count > 0 && used.Max() > input.Seats) err["seats"] = $"{Digits.ToBangla(used.Max())} নম্বর সিটে ছাত্র আছে, এর কম করা যাবে না";
            if (used.Count > 0 && room.ForGender != input.ForGender) err["forGender"] = "রুমে ছাত্র থাকা অবস্থায় বদলানো যাবে না";
        }
        if (err.Count > 0) throw AppException.Invalid(err);
        if (room is null) { room = new HostelRoom(); db.HostelRooms.Add(room); }
        room.Building = building; room.RoomNo = no; room.Seats = input.Seats; room.ForGender = input.ForGender;
        await db.SaveChangesAsync(ct);
        return (await SummaryAsync(ct)).RoomList.First(r => r.Id == room.Id);
    }

    public async Task DeleteRoomAsync(int id, CancellationToken ct)
    {
        var room = await db.HostelRooms.FirstOrDefaultAsync(r => r.Id == id, ct) ?? throw AppException.NotFound("রুম");
        if (await db.HostelAllocations.AnyAsync(a => a.RoomId == id && a.ToDate == null, ct)) throw new AppException("রুমে ছাত্র আছে। আগে সিট খালি করুন।", 409);
        db.HostelAllocations.RemoveRange(db.HostelAllocations.Where(a => a.RoomId == id));
        db.HostelRooms.Remove(room);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Active students not yet in a seat, for the allocate picker.</summary>
    public async Task<List<Resident>> CandidatesAsync(string? q, CancellationToken ct)
    {
        var sessionId = await db.CurrentSessionIdAsync(ct);
        var seated = db.HostelAllocations.Where(a => a.ToDate == null).Select(a => a.StudentId);
        var query = db.Students.AsNoTracking().Where(s => s.Status == StudentStatus.Active && !seated.Contains(s.Id));
        if (!string.IsNullOrWhiteSpace(q)) { var t = q.Trim(); var e = Digits.ToEnglish(t)!; query = query.Where(s => s.Name.Contains(t) || s.StudentCode.Contains(e)); }
        return await query.OrderBy(s => s.StudentCode).Take(30)
            .Select(s => new Resident(s.Id, s.StudentCode, s.Name, db.Enrollments.Where(x => x.StudentId == s.Id && x.SessionId == sessionId).Select(x => x.Class!.NameBn).FirstOrDefault(), s.Gender, null))
            .ToListAsync(ct);
    }

    public async Task AllocateAsync(AllocateInput input, CancellationToken ct)
    {
        var room = await db.HostelRooms.FirstOrDefaultAsync(r => r.Id == input.RoomId, ct) ?? throw AppException.NotFound("রুম");
        var s = await db.Students.FirstOrDefaultAsync(x => x.Id == input.StudentId, ct) ?? throw AppException.NotFound("শিক্ষার্থী");
        if (s.Status != StudentStatus.Active) throw new AppException("শুধু চলমান শিক্ষার্থীকে সিট দেওয়া যায়");
        if (s.Gender != room.ForGender) throw new AppException(room.ForGender == Gender.Male ? "এটি ছাত্রদের রুম" : "এটি ছাত্রীদের রুম");
        if (await db.HostelAllocations.AnyAsync(a => a.StudentId == s.Id && a.ToDate == null, ct)) throw new AppException($"{s.Name} আগেই একটি সিটে আছে", 409);
        var used = await db.HostelAllocations.Where(a => a.RoomId == room.Id && a.ToDate == null).Select(a => a.SeatNo).ToListAsync(ct);
        int seat;
        if (input.SeatNo is int n)
        {
            if (n < 1 || n > room.Seats) throw AppException.Invalid(new Dictionary<string, string> { ["seatNo"] = $"সিট নম্বর ১ থেকে {Digits.ToBangla(room.Seats)}" });
            if (used.Contains(n)) throw AppException.Invalid(new Dictionary<string, string> { ["seatNo"] = "এই সিটে আগেই একজন আছে" });
            seat = n;
        }
        else
        {
            seat = Enumerable.Range(1, room.Seats).FirstOrDefault(x => !used.Contains(x));
            if (seat == 0) throw new AppException("রুমে কোনো খালি সিট নেই", 409);
        }
        var from = input.FromDate ?? clock.Today;
        db.HostelAllocations.Add(new HostelAllocation { StudentId = s.Id, RoomId = room.Id, SeatNo = seat, FromDate = from });
        s.IsResidential = true;
        await db.SaveChangesAsync(ct);
    }

    public async Task VacateAsync(int allocationId, CancellationToken ct)
    {
        var a = await db.HostelAllocations.FirstOrDefaultAsync(x => x.Id == allocationId && x.ToDate == null, ct) ?? throw AppException.NotFound("বরাদ্দ");
        a.ToDate = clock.Today;
        var s = await db.Students.FirstOrDefaultAsync(x => x.Id == a.StudentId, ct);
        if (s != null) s.IsResidential = false;
        foreach (var l in await db.HostelLeaves.Where(l => l.StudentId == a.StudentId && l.ReturnedOn == null).ToListAsync(ct)) l.ReturnedOn = clock.Today;
        await db.SaveChangesAsync(ct);
    }

    // ---- leave register ----
    public async Task<List<LeaveRow>> LeavesAsync(bool openOnly, CancellationToken ct)
    {
        var today = clock.Today;
        var sessionId = await db.CurrentSessionIdAsync(ct);
        var q = db.HostelLeaves.AsNoTracking().AsQueryable();
        q = openOnly ? q.Where(l => l.ReturnedOn == null) : q.Where(l => l.FromDate >= today.AddDays(-60));
        return await q.OrderBy(l => l.ReturnedOn != null).ThenByDescending(l => l.FromDate)
            .Join(db.Students.IgnoreQueryFilters(), l => l.StudentId, s => s.Id, (l, s) => new { l, s })
            .Select(x => new LeaveRow(x.l.Id, x.s.Id, x.s.StudentCode, x.s.Name,
                db.Enrollments.Where(e => e.StudentId == x.s.Id && e.SessionId == sessionId).Select(e => e.Class!.NameBn).FirstOrDefault(),
                db.HostelAllocations.Where(a => a.StudentId == x.s.Id && a.ToDate == null).Select(a => a.Room!.RoomNo).FirstOrDefault(),
                x.l.FromDate, x.l.ToDate, x.l.Reason, x.l.TakenBy, x.l.ReturnedOn, x.l.ReturnedOn == null && x.l.ToDate != null && x.l.ToDate < today))
            .Take(200).ToListAsync(ct);
    }

    public async Task<List<Resident>> ResidentsAsync(CancellationToken ct)
    {
        var sessionId = await db.CurrentSessionIdAsync(ct);
        return await db.HostelAllocations.AsNoTracking().Where(a => a.ToDate == null)
            .Join(db.Students, a => a.StudentId, s => s.Id, (a, s) => new { a, s }).OrderBy(x => x.s.Name)
            .Select(x => new Resident(x.s.Id, x.s.StudentCode, x.s.Name, db.Enrollments.Where(e => e.StudentId == x.s.Id && e.SessionId == sessionId).Select(e => e.Class!.NameBn).FirstOrDefault(),
                x.s.Gender, x.a.Room!.RoomNo)).ToListAsync(ct);
    }

    public async Task AddLeaveAsync(LeaveInput input, CancellationToken ct)
    {
        var err = new Dictionary<string, string>();
        if (input.ToDate is DateOnly t && t < input.FromDate) err["toDate"] = "ফেরার তারিখ যাওয়ার আগে হতে পারে না";
        if (!await db.HostelAllocations.AnyAsync(a => a.StudentId == input.StudentId && a.ToDate == null, ct)) err["studentId"] = "এই শিক্ষার্থী হোস্টেলে থাকে না";
        else if (await db.HostelLeaves.AnyAsync(l => l.StudentId == input.StudentId && l.ReturnedOn == null, ct)) err["studentId"] = "আগের ছুটি থেকে এখনও ফেরেনি";
        if (err.Count > 0) throw AppException.Invalid(err);
        db.HostelLeaves.Add(new HostelLeave { StudentId = input.StudentId, FromDate = input.FromDate, ToDate = input.ToDate,
            Reason = string.IsNullOrWhiteSpace(input.Reason) ? null : input.Reason.Trim(), TakenBy = string.IsNullOrWhiteSpace(input.TakenBy) ? null : input.TakenBy.Trim() });
        await db.SaveChangesAsync(ct);
    }

    public async Task ReturnAsync(int leaveId, DateOnly? on, CancellationToken ct)
    {
        var l = await db.HostelLeaves.FirstOrDefaultAsync(x => x.Id == leaveId, ct) ?? throw AppException.NotFound("ছুটি");
        if (l.ReturnedOn != null) throw new AppException("আগেই ফেরত দেখানো হয়েছে", 409);
        var d = on ?? clock.Today;
        if (d < l.FromDate) throw new AppException("ফেরার তারিখ ছুটির আগে হতে পারে না");
        l.ReturnedOn = d;
        await db.SaveChangesAsync(ct);
    }
}
