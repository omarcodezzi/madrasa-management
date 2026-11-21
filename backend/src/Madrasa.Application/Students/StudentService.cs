using System.Text.RegularExpressions;
using Madrasa.Application.Common;
using Madrasa.Domain.Entities;
using Madrasa.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Madrasa.Application.Students;

public partial class StudentService(IAppDbContext db, IClock clock, IFileStorage files)
{
    public async Task<PagedResult<StudentRow>> ListAsync(StudentListQuery q, CancellationToken ct)
    {
        var sessionId = await db.CurrentSessionIdAsync(ct);
        var pageSize = Math.Clamp(q.PageSize, 1, 500);
        var page = Math.Max(1, q.Page);
        var query = db.Students.AsNoTracking();
        if (q.ClassId is not null) query = query.Where(s => s.Enrollments.Any(e => e.SessionId == sessionId && e.ClassId == q.ClassId));
        if (q.Gender is not null) query = query.Where(s => s.Gender == q.Gender);
        if (q.Status is not null) query = query.Where(s => s.Status == q.Status);
        if (!string.IsNullOrWhiteSpace(q.Q))
        {
            var term = Digits.ToEnglish(q.Q.Trim());
            var phoneTerm = Digits.PhoneDigits(term);
            int.TryParse(term, out var roll);
            query = query.Where(s => s.Name.Contains(term) || s.StudentCode.Contains(term)
                || (s.FatherName != null && s.FatherName.Contains(term))
                || (phoneTerm.Length >= 4 && s.Phone != null && s.Phone.Replace("-", "").Replace(" ", "").Contains(phoneTerm))
                || (phoneTerm.Length >= 4 && s.Guardians.Any(g => g.Guardian!.Phone.Replace("-", "").Replace(" ", "").Contains(phoneTerm)))
                || (roll > 0 && s.Enrollments.Any(e => e.SessionId == sessionId && e.Roll == roll)));
        }

        var rows = query.Select(s => new
        {
            S = s,
            E = s.Enrollments.Where(e => e.SessionId == sessionId).Select(e => new { e.ClassId, e.Class!.NameBn, e.Class.SortOrder, e.Roll }).FirstOrDefault(),
        });
        rows = q.Sort switch
        {
            "name" => rows.OrderBy(r => r.S.Name),
            "-name" => rows.OrderByDescending(r => r.S.Name),
            "admitted" => rows.OrderBy(r => r.S.AdmissionDate),
            "-admitted" => rows.OrderByDescending(r => r.S.AdmissionDate),
            "code" => rows.OrderBy(r => r.S.StudentCode),
            _ => rows.OrderBy(r => r.E!.SortOrder).ThenBy(r => r.E!.Roll).ThenBy(r => r.S.StudentCode),
        };
        var total = await rows.CountAsync(ct);
        var items = await rows.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(r => new StudentRow(r.S.Id, r.S.StudentCode, r.S.Name, r.S.Gender, r.E == null ? null : r.E.ClassId, r.E == null ? null : r.E.NameBn,
                r.E == null ? null : r.E.Roll, r.S.FatherName, r.S.Phone, r.S.PhotoPath, r.S.Status, r.S.IsOrphan, r.S.IsResidential, r.S.AdmissionDate))
            .ToListAsync(ct);
        return new PagedResult<StudentRow>(items, total, page, pageSize);
    }

    public async Task<StudentDetail> GetAsync(int id, CancellationToken ct)
    {
        var sessionId = await db.CurrentSessionIdAsync(ct);
        var s = await db.Students.AsNoTracking()
            .Include(x => x.Guardians).ThenInclude(g => g.Guardian)
            .Include(x => x.Enrollments.Where(e => e.SessionId == sessionId)).ThenInclude(e => e.Class!).ThenInclude(c => c.Department)
            .FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw AppException.NotFound("শিক্ষার্থী");
        var e = s.Enrollments.FirstOrDefault();
        return new StudentDetail(s.Id, s.StudentCode, s.Name, s.NameAr, s.Gender, s.DateOfBirth, s.BirthRegNo, s.BloodGroup, s.FatherName, s.MotherName,
            s.Phone, s.PresentAddress, s.PermanentAddress, s.PhotoPath, s.AdmissionDate, s.IsOrphan, s.IsResidential, s.Status,
            e?.ClassId, e?.Class?.NameBn, e?.Class?.Department?.NameBn, e?.SectionId, e?.Roll, e?.Class?.MonthlyFee, e?.MonthlyFeeOverride,
            s.Guardians.OrderByDescending(g => g.IsPrimary).Select(g => new GuardianDto(g.GuardianId, g.Guardian!.Name, g.Guardian.Relation, g.Guardian.Phone,
                g.Guardian.AltPhone, g.Guardian.Occupation, g.Guardian.Nid, g.Guardian.Address, g.IsPrimary)).ToList());
    }

    /// <summary>Suggested ID (RM-2026-019) and next free roll for a class.</summary>
    public async Task<NextCodes> NextCodesAsync(int? classId, CancellationToken ct)
    {
        var sessionId = await db.CurrentSessionIdAsync(ct);
        return new NextCodes(await NextStudentCodeAsync(sessionId, ct), classId is null ? 1 : await NextRollAsync(sessionId, classId.Value, null, ct));
    }

    public async Task<StudentDetail> CreateAsync(StudentInput input, CancellationToken ct)
    {
        var sessionId = await db.CurrentSessionIdAsync(ct);
        await ValidateAsync(input, sessionId, null, ct);
        var student = new Student { StudentCode = await NextStudentCodeAsync(sessionId, ct), AdmissionDate = input.AdmissionDate ?? clock.Today };
        Apply(student, input);
        var roll = input.Roll is > 0 ? input.Roll.Value : await NextRollAsync(sessionId, input.ClassId, input.SectionId, ct);
        student.Enrollments.Add(new Enrollment { SessionId = sessionId, ClassId = input.ClassId, SectionId = input.SectionId, Roll = roll, MonthlyFeeOverride = input.MonthlyFeeOverride });
        await SyncGuardiansAsync(student, input.Guardians, ct);
        db.Students.Add(student);
        await db.SaveChangesAsync(ct);
        if (!string.IsNullOrEmpty(input.PhotoDataUrl)) { student.PhotoPath = await SavePhotoAsync(student.StudentCode, input.PhotoDataUrl, ct); await db.SaveChangesAsync(ct); }
        return await GetAsync(student.Id, ct);
    }

    public async Task<StudentDetail> UpdateAsync(int id, StudentInput input, CancellationToken ct)
    {
        var sessionId = await db.CurrentSessionIdAsync(ct);
        var student = await db.Students.Include(s => s.Guardians).ThenInclude(g => g.Guardian)
            .Include(s => s.Enrollments.Where(e => e.SessionId == sessionId))
            .FirstOrDefaultAsync(s => s.Id == id, ct) ?? throw AppException.NotFound("শিক্ষার্থী");
        await ValidateAsync(input, sessionId, id, ct);
        Apply(student, input);
        if (input.AdmissionDate is not null) student.AdmissionDate = input.AdmissionDate.Value;
        var e = student.Enrollments.FirstOrDefault();
        if (e is null)
        {
            e = new Enrollment { SessionId = sessionId };
            student.Enrollments.Add(e);
        }
        var classChanged = e.ClassId != input.ClassId || e.SectionId != input.SectionId;
        e.ClassId = input.ClassId;
        e.SectionId = input.SectionId;
        e.MonthlyFeeOverride = input.MonthlyFeeOverride;
        if (input.Roll is > 0) e.Roll = input.Roll.Value;
        else if (classChanged || e.Roll == 0) e.Roll = await NextRollAsync(sessionId, input.ClassId, input.SectionId, ct);
        await SyncGuardiansAsync(student, input.Guardians, ct);
        if (!string.IsNullOrEmpty(input.PhotoDataUrl)) student.PhotoPath = await SavePhotoAsync(student.StudentCode, input.PhotoDataUrl, ct);
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    /// <summary>Soft delete: the record and its payments stay for reports.</summary>
    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var s = await db.Students.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw AppException.NotFound("শিক্ষার্থী");
        s.IsDeleted = true;
        await db.SaveChangesAsync(ct);
    }

    // ---------- helpers ----------

    private static void Apply(Student s, StudentInput i)
    {
        s.Name = i.Name.Trim();
        s.NameAr = Clean(i.NameAr);
        s.Gender = i.Gender;
        s.DateOfBirth = i.DateOfBirth;
        s.BirthRegNo = Clean(Digits.ToEnglish(i.BirthRegNo));
        s.BloodGroup = Clean(i.BloodGroup);
        s.FatherName = Clean(i.FatherName);
        s.MotherName = Clean(i.MotherName);
        s.Phone = Clean(Digits.ToEnglish(i.Phone));
        s.PresentAddress = Clean(i.PresentAddress);
        s.PermanentAddress = Clean(i.PermanentAddress);
        s.IsOrphan = i.IsOrphan;
        s.IsResidential = i.IsResidential;
        if (i.Status is not null) s.Status = i.Status.Value;
    }

    private async Task ValidateAsync(StudentInput i, int sessionId, int? studentId, CancellationToken ct)
    {
        var err = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(i.Name)) err["name"] = "শিক্ষার্থীর নাম লিখুন";
        else if (i.Name.Length > 150) err["name"] = "নাম ১৫০ অক্ষরের মধ্যে লিখুন";
        if (!string.IsNullOrWhiteSpace(i.Phone) && !Digits.IsBdMobile(i.Phone)) err["phone"] = "সঠিক মোবাইল নম্বর লিখুন (১১ সংখ্যা, ০১ দিয়ে শুরু)";
        if (i.DateOfBirth is { } dob && (dob > clock.Today || dob < clock.Today.AddYears(-60))) err["dateOfBirth"] = "জন্ম তারিখ ঠিক নয়";
        if (!await db.Classes.AnyAsync(c => c.Id == i.ClassId, ct)) err["classId"] = "শ্রেণি বাছাই করুন";
        if (i.SectionId is not null && !await db.Sections.AnyAsync(x => x.Id == i.SectionId && x.ClassId == i.ClassId, ct)) err["sectionId"] = "শাখাটি এই শ্রেণির নয়";
        if (i.MonthlyFeeOverride is < 0) err["monthlyFeeOverride"] = "বেতন ঋণাত্মক হতে পারে না";
        if (i.Roll is > 0 && await db.Enrollments.AnyAsync(e => e.SessionId == sessionId && e.ClassId == i.ClassId && e.SectionId == i.SectionId
                && e.Roll == i.Roll && e.StudentId != studentId && !e.Student!.IsDeleted, ct))
            err["roll"] = "এই শ্রেণিতে এই রোল আগে থেকেই আছে";
        var gs = i.Guardians ?? [];
        for (var k = 0; k < gs.Count; k++)
        {
            if (string.IsNullOrWhiteSpace(gs[k].Name)) err[$"guardians[{k}].name"] = "অভিভাবকের নাম লিখুন";
            if (!Digits.IsBdMobile(gs[k].Phone)) err[$"guardians[{k}].phone"] = "সঠিক মোবাইল নম্বর লিখুন (১১ সংখ্যা)";
        }
        if (err.Count > 0) throw AppException.Invalid(err);
    }

    private async Task SyncGuardiansAsync(Student s, IReadOnlyList<GuardianDto>? input, CancellationToken ct)
    {
        if (input is null) return;
        var keep = new HashSet<int>();
        var primarySet = false;
        foreach (var g in input)
        {
            var phone = Digits.ToEnglish(g.Phone)!.Trim();
            // Siblings share a guardian: reuse an existing record with the same phone and name
            Guardian? entity = g.Id is > 0 ? await db.Guardians.FindAsync([g.Id.Value], ct) : null;
            entity ??= await db.Guardians.FirstOrDefaultAsync(x => x.Phone == phone && x.Name == g.Name.Trim(), ct);
            entity ??= new Guardian();
            entity.Name = g.Name.Trim();
            entity.Relation = string.IsNullOrWhiteSpace(g.Relation) ? "অভিভাবক" : g.Relation.Trim();
            entity.Phone = phone;
            entity.AltPhone = Clean(Digits.ToEnglish(g.AltPhone));
            entity.Occupation = Clean(g.Occupation);
            entity.Nid = Clean(Digits.ToEnglish(g.Nid));
            entity.Address = Clean(g.Address);
            var isPrimary = g.IsPrimary && !primarySet;
            primarySet |= isPrimary;
            var link = s.Guardians.FirstOrDefault(x => x.Guardian == entity || (entity.Id > 0 && x.GuardianId == entity.Id));
            if (link is null) s.Guardians.Add(link = new StudentGuardian { Guardian = entity });
            link.IsPrimary = isPrimary;
            if (entity.Id > 0) keep.Add(entity.Id);
        }
        foreach (var old in s.Guardians.Where(x => x.GuardianId > 0 && !keep.Contains(x.GuardianId)).ToList()) s.Guardians.Remove(old);
        if (!primarySet && s.Guardians.Count > 0) s.Guardians[0].IsPrimary = true;
    }

    private async Task<string> NextStudentCodeAsync(int sessionId, CancellationToken ct)
    {
        var prefix = await db.MadrasaProfiles.Select(p => p.StudentIdPrefix).FirstOrDefaultAsync(ct) ?? "RM";
        var year = await db.AcademicSessions.Where(s => s.Id == sessionId).Select(s => s.StartDate.Year).FirstAsync(ct);
        var head = $"{prefix}-{year}-";
        // Include soft-deleted rows so a code is never reused (printed cards must stay unique)
        var codes = await db.Students.IgnoreQueryFilters().Where(s => s.StudentCode.StartsWith(head)).Select(s => s.StudentCode).ToListAsync(ct);
        var max = codes.Select(c => int.TryParse(c[head.Length..], out var n) ? n : 0).DefaultIfEmpty(0).Max();
        return $"{head}{max + 1:000}";
    }

    private async Task<int> NextRollAsync(int sessionId, int classId, int? sectionId, CancellationToken ct) =>
        (await db.Enrollments.Where(e => e.SessionId == sessionId && e.ClassId == classId && e.SectionId == sectionId)
            .Select(e => (int?)e.Roll).MaxAsync(ct) ?? 0) + 1;

    private async Task<string> SavePhotoAsync(string code, string dataUrl, CancellationToken ct)
    {
        var m = DataUrlRx().Match(dataUrl);
        if (!m.Success) throw AppException.Invalid(new Dictionary<string, string> { ["photo"] = "ছবিটি JPG বা PNG হতে হবে" });
        var bytes = Convert.FromBase64String(m.Groups[2].Value);
        if (bytes.Length > 2 * 1024 * 1024) throw AppException.Invalid(new Dictionary<string, string> { ["photo"] = "ছবিটি ২ MB-এর বেশি বড়" });
        var ext = m.Groups[1].Value == "png" ? "png" : "jpg";
        return await files.SaveAsync("students", $"{code}-{Guid.NewGuid():N}.{ext}", bytes, ct);
    }

    private static string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    [GeneratedRegex(@"^data:image/(jpeg|jpg|png);base64,([A-Za-z0-9+/=]+)$")]
    private static partial Regex DataUrlRx();
}
