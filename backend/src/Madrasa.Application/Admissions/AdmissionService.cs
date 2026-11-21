using Madrasa.Application.Common;
using Madrasa.Application.Notices;
using Madrasa.Domain.Entities;
using Madrasa.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Madrasa.Application.Admissions;

public record PublicClass(int Id, string NameBn, string? NameAr, string Department, decimal? MonthlyFee);
public record PublicInfo(string NameBn, string? NameEn, string? NameAr, string? Address, string? Phone, string? Email, string? LogoPath, int? EstablishedYear,
    int Students, int Teachers, int Huffaz, bool OnlineAdmission, string? SessionName, IReadOnlyList<PublicClass> Classes,
    IReadOnlyList<NoticeDto> Notices, IReadOnlyList<EventDto> Events);
public record ApplyInput(int ClassId, string ApplicantName, Gender Gender, DateOnly? DateOfBirth, string? FatherName, string GuardianPhone,
    string? Address, string? PreviousInstitution, string? Photo, string? Website);
public record ApplyResult(string ApplicationNo, string Message);
public record ApplicationStatus(string ApplicationNo, string ApplicantName, string ClassName, AdmissionStage Stage, DateTime AppliedAt, string? Remarks);
public record ApplicationRow(int Id, string ApplicationNo, string ApplicantName, Gender Gender, DateOnly? DateOfBirth, string? FatherName, string GuardianPhone,
    string? Address, string? PreviousInstitution, string? PhotoPath, int ClassId, string ClassName, AdmissionSource Source, AdmissionStage Stage,
    decimal? TestMarks, string? Remarks, int? StudentId, DateTime AppliedAt);
public record StageInput(AdmissionStage Stage, decimal? TestMarks, string? Remarks);
public record AdmittedInput(int StudentId);
public record PipelineCount(AdmissionStage Stage, int Count);

/// <summary>Online / office admission applications: Applied → Test → Approved → Admitted (or Rejected).</summary>
public class AdmissionService(IAppDbContext db, IClock clock, IFileStorage files, NoticeService notices)
{
    public async Task<PublicInfo> PublicInfoAsync(CancellationToken ct)
    {
        var p = await db.MadrasaProfiles.AsNoTracking().FirstOrDefaultAsync(ct) ?? new MadrasaProfile { NameBn = "মাদরাসা" };
        var session = await db.AcademicSessions.AsNoTracking().FirstOrDefaultAsync(s => s.IsCurrent, ct);
        var classes = await db.Classes.AsNoTracking().OrderBy(c => c.SortOrder)
            .Select(c => new PublicClass(c.Id, c.NameBn, c.NameAr, c.Department!.NameBn, c.MonthlyFee)).ToListAsync(ct);
        var students = await db.Students.CountAsync(s => s.Status == StudentStatus.Active, ct);
        var teachers = await db.Staff.CountAsync(s => s.IsActive, ct);
        var huffaz = await db.HifzJuzProgress.Where(j => j.Status == JuzStatus.Completed).GroupBy(j => j.StudentId).CountAsync(g => g.Count() == 30, ct);
        var online = await db.AppSettings.Where(s => s.Key == "admission.online").Select(s => s.Value).FirstOrDefaultAsync(ct) != "false";
        return new PublicInfo(p.NameBn, p.NameEn, p.NameAr, p.Address, p.Phone, p.Email, p.LogoPath, p.EstablishedYear, students, teachers, huffaz, online && session != null,
            session?.Name, classes, await notices.ListAsync(true, 6, ct), await notices.EventsAsync(clock.Today, clock.Today.AddDays(120), ct));
    }

    public async Task<ApplyResult> ApplyAsync(ApplyInput input, CancellationToken ct)
    {
        // Honeypot: a hidden "website" field only bots fill in. Answer as if it worked.
        if (!string.IsNullOrWhiteSpace(input.Website)) return new ApplyResult("A-000000", "আবেদন জমা হয়েছে");
        if (await db.AppSettings.Where(s => s.Key == "admission.online").Select(s => s.Value).FirstOrDefaultAsync(ct) == "false")
            throw new AppException("এখন অনলাইনে ভর্তির আবেদন নেওয়া হচ্ছে না। অফিসে যোগাযোগ করুন।", 403);
        var sessionId = await db.CurrentSessionIdAsync(ct);
        var a = await BuildAsync(input, sessionId, AdmissionSource.Online, ct);
        var phone = a.GuardianPhone;
        var since = DateTime.UtcNow.AddTicks(-clock.Now.Ticks); // local midnight, in UTC
        if (await db.AdmissionApplications.CountAsync(x => x.GuardianPhone == phone && x.AppliedAt >= since, ct) >= 5)
            throw new AppException("এই নম্বর থেকে আজ অনেকগুলো আবেদন হয়েছে। কাল আবার চেষ্টা করুন বা অফিসে যোগাযোগ করুন।", 429);
        if (await db.AdmissionApplications.AnyAsync(x => x.GuardianPhone == phone && x.ApplicantName == a.ApplicantName && x.SessionId == sessionId
                && x.Stage != AdmissionStage.Rejected, ct))
            throw new AppException("এই নামে ও নম্বরে আগেই আবেদন করা হয়েছে। আবেদন নম্বর দিয়ে অবস্থা দেখুন।", 409);
        await SaveNewAsync(a, input.Photo, ct);
        return new ApplyResult(a.ApplicationNo, "আবেদন জমা হয়েছে। ভর্তি পরীক্ষার তারিখ জানাতে অফিস থেকে ফোন করা হবে।");
    }

    public async Task<ApplicationRow> CreateOfficeAsync(ApplyInput input, CancellationToken ct)
    {
        var a = await BuildAsync(input, await db.CurrentSessionIdAsync(ct), AdmissionSource.Office, ct);
        await SaveNewAsync(a, input.Photo, ct);
        return (await ListAsync(null, null, ct)).First(x => x.Id == a.Id);
    }

    private async Task<AdmissionApplication> BuildAsync(ApplyInput input, int sessionId, AdmissionSource source, CancellationToken ct)
    {
        var err = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(input.ApplicantName)) err["applicantName"] = "শিক্ষার্থীর নাম লিখুন";
        else if (input.ApplicantName.Trim().Length > 100) err["applicantName"] = "নাম ১০০ অক্ষরের মধ্যে";
        if (!Enum.IsDefined(input.Gender)) err["gender"] = "ছাত্র না ছাত্রী, বাছাই করুন";
        if (!Digits.IsBdMobile(input.GuardianPhone)) err["guardianPhone"] = "সঠিক মোবাইল নম্বর দিন (০১ দিয়ে শুরু, ১১ সংখ্যা)";
        if (input.DateOfBirth is DateOnly d && (d > clock.Today.AddYears(-3) || d < clock.Today.AddYears(-40))) err["dateOfBirth"] = "জন্মতারিখ ঠিক নেই";
        if (!await db.Classes.AnyAsync(c => c.Id == input.ClassId, ct)) err["classId"] = "শ্রেণি বাছাই করুন";
        if ((input.Address?.Length ?? 0) > 300) err["address"] = "ঠিকানা ৩০০ অক্ষরের মধ্যে";
        if (err.Count > 0) throw AppException.Invalid(err);
        return new AdmissionApplication { SessionId = sessionId, ClassId = input.ClassId, ApplicantName = input.ApplicantName.Trim(), Gender = input.Gender,
            DateOfBirth = input.DateOfBirth, FatherName = Blank(input.FatherName), GuardianPhone = Digits.PhoneDigits(input.GuardianPhone), Address = Blank(input.Address),
            PreviousInstitution = Blank(input.PreviousInstitution), Source = source, Stage = AdmissionStage.Applied };
    }

    private async Task SaveNewAsync(AdmissionApplication a, string? photo, CancellationToken ct)
    {
        a.ApplicationNo = "pending-" + Guid.NewGuid().ToString("N")[..10];
        a.AppliedAt = DateTime.UtcNow;
        db.AdmissionApplications.Add(a);
        await db.SaveChangesAsync(ct);
        a.ApplicationNo = $"A-{clock.Today.Year}-{a.Id:0000}";
        if (!string.IsNullOrEmpty(photo)) a.PhotoPath = await Photos.SaveAsync(files, "applications", a.ApplicationNo, photo, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task<ApplicationStatus> StatusAsync(string applicationNo, string phone, CancellationToken ct)
    {
        var no = (Digits.ToEnglish(applicationNo) ?? "").Trim().ToUpperInvariant();
        var ph = Digits.PhoneDigits(phone);
        var a = await db.AdmissionApplications.AsNoTracking().Where(x => x.ApplicationNo == no && x.GuardianPhone == ph)
            .Select(x => new ApplicationStatus(x.ApplicationNo, x.ApplicantName, x.Class!.NameBn, x.Stage, x.AppliedAt, null)).FirstOrDefaultAsync(ct);
        return a is null ? throw new AppException("এই আবেদন নম্বর ও মোবাইল নম্বরে কোনো আবেদন পাওয়া যায়নি", 404)
            : a with { AppliedAt = clock.ToLocal(a.AppliedAt) };
    }

    // ---- office ----
    public async Task<List<ApplicationRow>> ListAsync(AdmissionStage? stage, string? q, CancellationToken ct)
    {
        var sessionId = await db.CurrentSessionIdAsync(ct);
        var query = db.AdmissionApplications.AsNoTracking().Where(a => a.SessionId == sessionId);
        if (stage is AdmissionStage s) query = query.Where(a => a.Stage == s);
        if (!string.IsNullOrWhiteSpace(q)) { var t = q.Trim(); var e = Digits.ToEnglish(t)!; query = query.Where(a => a.ApplicantName.Contains(t) || a.ApplicationNo.Contains(e) || a.GuardianPhone.Contains(e)); }
        var rows = await query.OrderByDescending(a => a.Id).Take(300)
            .Select(a => new ApplicationRow(a.Id, a.ApplicationNo, a.ApplicantName, a.Gender, a.DateOfBirth, a.FatherName, a.GuardianPhone, a.Address, a.PreviousInstitution,
                a.PhotoPath, a.ClassId, a.Class!.NameBn, a.Source, a.Stage, a.TestMarks, a.Remarks, a.StudentId, a.AppliedAt)).ToListAsync(ct);
        return rows.Select(r => r with { AppliedAt = clock.ToLocal(r.AppliedAt) }).ToList();
    }

    public async Task<List<PipelineCount>> PipelineAsync(CancellationToken ct)
    {
        var sessionId = await db.CurrentSessionIdAsync(ct);
        var c = await db.AdmissionApplications.Where(a => a.SessionId == sessionId).GroupBy(a => a.Stage).Select(g => new { g.Key, N = g.Count() }).ToListAsync(ct);
        return Enum.GetValues<AdmissionStage>().Select(s => new PipelineCount(s, c.FirstOrDefault(x => x.Key == s)?.N ?? 0)).ToList();
    }

    public async Task<ApplicationRow> SetStageAsync(int id, StageInput input, CancellationToken ct)
    {
        var a = await db.AdmissionApplications.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw AppException.NotFound("আবেদন");
        if (a.Stage == AdmissionStage.Admitted) throw new AppException("ভর্তি হয়ে গেছে, ধাপ বদলানো যাবে না", 409);
        if (input.Stage == AdmissionStage.Admitted) throw new AppException("ভর্তি করতে শিক্ষার্থীর ফর্ম পূরণ করুন");
        if (!Enum.IsDefined(input.Stage)) throw AppException.Invalid(new Dictionary<string, string> { ["stage"] = "ধাপ ঠিক নেই" });
        if (input.TestMarks is < 0 or > 100) throw AppException.Invalid(new Dictionary<string, string> { ["testMarks"] = "নম্বর ০ থেকে ১০০" });
        a.Stage = input.Stage; a.TestMarks = input.TestMarks ?? a.TestMarks; a.Remarks = Blank(input.Remarks) ?? a.Remarks;
        await db.SaveChangesAsync(ct);
        return (await ListAsync(null, null, ct)).First(x => x.Id == id);
    }

    public async Task<ApplicationRow> MarkAdmittedAsync(int id, AdmittedInput input, CancellationToken ct)
    {
        var a = await db.AdmissionApplications.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw AppException.NotFound("আবেদন");
        if (!await db.Students.AnyAsync(s => s.Id == input.StudentId, ct)) throw AppException.NotFound("শিক্ষার্থী");
        a.Stage = AdmissionStage.Admitted; a.StudentId = input.StudentId;
        await db.SaveChangesAsync(ct);
        return (await ListAsync(null, null, ct)).First(x => x.Id == id);
    }

    private static string? Blank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
