using System.Text.Json;
using Madrasa.Application.Common;
using Madrasa.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Madrasa.Application.Academic;

/// <summary>Departments, classes, sections, kitabs (subjects per class) and the weekly routine.</summary>
public class AcademicService(IAppDbContext db)
{
    private const string PeriodsKey = "routine.periods";

    public async Task<List<DepartmentOverview>> OverviewAsync(CancellationToken ct)
    {
        var sessionId = await db.CurrentSessionIdAsync(ct);
        var depts = await db.Departments.AsNoTracking().OrderBy(d => d.SortOrder)
            .Select(d => new { d.Id, d.NameBn, d.NameAr, d.Kind }).ToListAsync(ct);
        var classes = await db.Classes.AsNoTracking().OrderBy(c => c.SortOrder)
            .Select(c => new
            {
                c.Id, c.NameBn, c.NameAr, c.DepartmentId, c.MonthlyFee, c.SortOrder,
                Students = db.Enrollments.Count(e => e.ClassId == c.Id && e.SessionId == sessionId && !e.Student!.IsDeleted),
                Subjects = db.ClassSubjects.Count(s => s.ClassId == c.Id && s.SessionId == sessionId),
                Sections = c.Sections.Select(s => new SectionDto(s.Id, s.Name, s.Capacity,
                    db.Enrollments.Count(e => e.SectionId == s.Id && e.SessionId == sessionId && !e.Student!.IsDeleted))).ToList(),
            }).ToListAsync(ct);
        return depts.Select(d => new DepartmentOverview(d.Id, d.NameBn, d.NameAr, d.Kind,
            classes.Where(c => c.DepartmentId == d.Id).Select(c => new ClassOverview(c.Id, c.NameBn, c.NameAr, c.DepartmentId, c.MonthlyFee, c.SortOrder,
                c.Students, c.Subjects, c.Sections)).ToList())).ToList();
    }

    public async Task<int> SaveClassAsync(int? id, ClassInput input, CancellationToken ct)
    {
        var err = new Dictionary<string, string>();
        var name = input.NameBn?.Trim() ?? "";
        if (name.Length == 0) err["nameBn"] = "শ্রেণির নাম লিখুন";
        if (input.MonthlyFee is < 0) err["monthlyFee"] = "বেতন ঋণাত্মক হতে পারে না";
        if (!await db.Departments.AnyAsync(d => d.Id == input.DepartmentId, ct)) err["departmentId"] = "বিভাগ বাছাই করুন";
        if (name.Length > 0 && await db.Classes.AnyAsync(c => c.NameBn == name && c.Id != (id ?? 0), ct)) err["nameBn"] = "এই নামে আরেকটি শ্রেণি আছে";
        if (err.Count > 0) throw AppException.Invalid(err);

        SchoolClass c;
        if (id is null)
        {
            c = new SchoolClass { SortOrder = input.SortOrder ?? (await db.Classes.MaxAsync(x => (int?)x.SortOrder, ct) ?? 0) + 1 };
            db.Classes.Add(c);
        }
        else c = await db.Classes.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw AppException.NotFound("শ্রেণি");
        c.DepartmentId = input.DepartmentId; c.NameBn = name; c.NameAr = Blank(input.NameAr); c.MonthlyFee = input.MonthlyFee;
        if (input.SortOrder is int so) c.SortOrder = so;
        await db.SaveChangesAsync(ct);
        return c.Id;
    }

    public async Task DeleteClassAsync(int id, CancellationToken ct)
    {
        var c = await db.Classes.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw AppException.NotFound("শ্রেণি");
        if (await db.Enrollments.AnyAsync(e => e.ClassId == id, ct))
            throw new AppException("এই শ্রেণিতে শিক্ষার্থী আছে বা ছিল, তাই মুছা যাবে না। নাম বদলে নিতে পারেন।", 409);
        var subjects = await db.ClassSubjects.Where(s => s.ClassId == id).ToListAsync(ct);
        var subjectIds = subjects.Select(s => s.Id).ToList();
        if (await db.ExamMarks.AnyAsync(m => subjectIds.Contains(m.ClassSubjectId), ct))
            throw new AppException("এই শ্রেণির পরীক্ষার নম্বর আছে, তাই মুছা যাবে না।", 409);
        db.RoutineSlots.RemoveRange(db.RoutineSlots.Where(r => subjectIds.Contains(r.ClassSubjectId)));
        db.ClassSubjects.RemoveRange(subjects);
        db.Sections.RemoveRange(db.Sections.Where(s => s.ClassId == id));
        db.Classes.Remove(c);
        await db.SaveChangesAsync(ct);
    }

    public Task<List<SubjectDto>> SubjectsAsync(CancellationToken ct) =>
        db.Subjects.AsNoTracking().OrderBy(s => s.NameBn).Select(s => new SubjectDto(s.Id, s.NameBn, s.NameAr, s.Code)).ToListAsync(ct);

    public Task<List<TeacherDto>> TeachersAsync(CancellationToken ct) =>
        db.Staff.AsNoTracking().Where(s => s.IsActive).OrderBy(s => s.Name).Select(s => new TeacherDto(s.Id, s.Name, s.Designation, s.Phone)).ToListAsync(ct);

    public async Task<List<ClassSubjectDto>> ClassSubjectsAsync(int classId, CancellationToken ct)
    {
        var sessionId = await db.CurrentSessionIdAsync(ct);
        return await db.ClassSubjects.AsNoTracking().Where(s => s.ClassId == classId && s.SessionId == sessionId).OrderBy(s => s.Id)
            .Select(s => new ClassSubjectDto(s.Id, s.SubjectId, s.Subject!.NameBn, s.Subject.NameAr, s.TeacherId, s.Teacher!.Name, s.FullMarks, s.PassMarks))
            .ToListAsync(ct);
    }

    public async Task<ClassSubjectDto> SaveClassSubjectAsync(int classId, int? id, ClassSubjectInput input, CancellationToken ct)
    {
        var sessionId = await db.CurrentSessionIdAsync(ct);
        if (!await db.Classes.AnyAsync(c => c.Id == classId, ct)) throw AppException.NotFound("শ্রেণি");
        var err = new Dictionary<string, string>();
        if (input.FullMarks is < 1 or > 1000) err["fullMarks"] = "পূর্ণমান ১ থেকে ১০০০ এর মধ্যে দিন";
        if (input.PassMarks < 0 || input.PassMarks > input.FullMarks) err["passMarks"] = "পাস নম্বর পূর্ণমানের বেশি হতে পারে না";
        if (input.TeacherId is int t && !await db.Staff.AnyAsync(s => s.Id == t, ct)) err["teacherId"] = "উস্তাদ পাওয়া যায়নি";

        int subjectId;
        if (input.SubjectId is int sid) subjectId = sid;
        else
        {
            var nm = input.NewSubjectName?.Trim() ?? "";
            if (nm.Length == 0) { err["subjectId"] = "কিতাব/বিষয় বাছাই করুন বা নতুন নাম লিখুন"; subjectId = 0; }
            else
            {
                var existing = await db.Subjects.FirstOrDefaultAsync(s => s.NameBn == nm, ct);
                if (existing is null && err.Count == 0)
                {
                    existing = new Subject { NameBn = nm, NameAr = Blank(input.NewSubjectNameAr) };
                    db.Subjects.Add(existing);
                    await db.SaveChangesAsync(ct);
                }
                subjectId = existing?.Id ?? 0;
            }
        }
        if (subjectId > 0 && await db.ClassSubjects.AnyAsync(s => s.ClassId == classId && s.SessionId == sessionId && s.SubjectId == subjectId && s.Id != (id ?? 0), ct))
            err["subjectId"] = "এই কিতাব আগেই এই শ্রেণিতে যোগ করা আছে";
        if (err.Count > 0) throw AppException.Invalid(err);

        ClassSubject cs;
        if (id is null) { cs = new ClassSubject { ClassId = classId, SessionId = sessionId }; db.ClassSubjects.Add(cs); }
        else cs = await db.ClassSubjects.FirstOrDefaultAsync(s => s.Id == id && s.ClassId == classId, ct) ?? throw AppException.NotFound("কিতাব");
        cs.SubjectId = subjectId; cs.TeacherId = input.TeacherId; cs.FullMarks = input.FullMarks; cs.PassMarks = input.PassMarks;
        await db.SaveChangesAsync(ct);
        return (await ClassSubjectsAsync(classId, ct)).First(x => x.Id == cs.Id);
    }

    public async Task RemoveClassSubjectAsync(int classId, int id, CancellationToken ct)
    {
        var cs = await db.ClassSubjects.FirstOrDefaultAsync(s => s.Id == id && s.ClassId == classId, ct) ?? throw AppException.NotFound("কিতাব");
        if (await db.ExamMarks.AnyAsync(m => m.ClassSubjectId == id, ct))
            throw new AppException("এই কিতাবের পরীক্ষার নম্বর আছে, তাই সরানো যাবে না।", 409);
        db.RoutineSlots.RemoveRange(db.RoutineSlots.Where(r => r.ClassSubjectId == id));
        db.ClassSubjects.Remove(cs);
        await db.SaveChangesAsync(ct);
    }

    // ---- Routine ----

    /// <summary>Period times are the same for the whole madrasa; stored once in settings.</summary>
    public async Task<List<PeriodDto>> PeriodsAsync(CancellationToken ct)
    {
        var json = await db.AppSettings.Where(s => s.Key == PeriodsKey).Select(s => s.Value).FirstOrDefaultAsync(ct);
        if (!string.IsNullOrEmpty(json))
            try { return JsonSerializer.Deserialize<List<PeriodDto>>(json, Json) ?? DefaultPeriods(); } catch (JsonException) { }
        return DefaultPeriods();
    }

    public async Task<RoutineDto> RoutineAsync(int classId, CancellationToken ct)
    {
        var sessionId = await db.CurrentSessionIdAsync(ct);
        var cls = await db.Classes.AsNoTracking().Where(c => c.Id == classId).Select(c => c.NameBn).FirstOrDefaultAsync(ct) ?? throw AppException.NotFound("শ্রেণি");
        var section = await db.Sections.Where(s => s.ClassId == classId).OrderBy(s => s.Id).Select(s => (int?)s.Id).FirstOrDefaultAsync(ct);
        var slots = section is null ? new List<SlotDto>() : await db.RoutineSlots.AsNoTracking()
            .Where(r => r.SectionId == section && r.SessionId == sessionId).OrderBy(r => r.DayOfWeek).ThenBy(r => r.PeriodNo)
            .Select(r => new SlotDto(r.DayOfWeek, r.PeriodNo, r.ClassSubjectId, r.ClassSubject!.Subject!.NameBn, r.ClassSubject.Teacher!.Name, r.Room)).ToListAsync(ct);
        return new RoutineDto(classId, cls, await SchoolDaysAsync(ct), await PeriodsAsync(ct), slots);
    }

    public async Task<RoutineDto> SaveRoutineAsync(int classId, SaveRoutineRequest req, CancellationToken ct)
    {
        var sessionId = await db.CurrentSessionIdAsync(ct);
        if (!await db.Classes.AnyAsync(c => c.Id == classId, ct)) throw AppException.NotFound("শ্রেণি");
        var periods = req.Periods.OrderBy(p => p.PeriodNo).ToList();
        var err = new Dictionary<string, string>();
        foreach (var p in periods)
            if (p.End <= p.Start) err[$"periods[{p.PeriodNo}]"] = $"{Digits.ToBangla(p.PeriodNo)} নং ঘণ্টার শেষ সময় শুরুর পরে হতে হবে";
        for (int i = 1; i < periods.Count; i++)
            if (periods[i].Start < periods[i - 1].End) err[$"periods[{periods[i].PeriodNo}]"] = $"{Digits.ToBangla(periods[i].PeriodNo)} নং ঘণ্টা আগের ঘণ্টার সাথে মিলে যাচ্ছে";
        var subjects = await db.ClassSubjects.Where(s => s.ClassId == classId && s.SessionId == sessionId)
            .Select(s => new { s.Id, s.TeacherId, Teacher = s.Teacher!.Name }).ToDictionaryAsync(s => s.Id, ct);
        var byPeriod = periods.ToDictionary(p => p.PeriodNo);
        foreach (var s in req.Slots)
        {
            if (!subjects.ContainsKey(s.ClassSubjectId)) err[$"slot[{s.Day},{s.PeriodNo}]"] = "এই কিতাব এই শ্রেণির নয়";
            if (!byPeriod.ContainsKey(s.PeriodNo)) err[$"slot[{s.Day},{s.PeriodNo}]"] = "ঘণ্টাটি পাওয়া যায়নি";
            if (s.Day > 6) err[$"slot[{s.Day},{s.PeriodNo}]"] = "দিন ঠিক নেই";
        }
        if (req.Slots.GroupBy(s => (s.Day, s.PeriodNo)).Any(g => g.Count() > 1)) err["slots"] = "একই ঘণ্টায় দুটি কিতাব রাখা যাবে না";
        if (err.Count > 0) throw AppException.Invalid(err);

        var sectionId = await EnsureSectionAsync(classId, ct);

        // A teacher cannot be in two classes in the same period
        var teacherSlots = req.Slots.Where(s => subjects[s.ClassSubjectId].TeacherId != null)
            .Select(s => new { s.Day, s.PeriodNo, TeacherId = subjects[s.ClassSubjectId].TeacherId!.Value, subjects[s.ClassSubjectId].Teacher }).ToList();
        var teacherIds = teacherSlots.Select(t => t.TeacherId).Distinct().ToList();
        var others = await db.RoutineSlots.AsNoTracking()
            .Where(r => r.SessionId == sessionId && r.SectionId != sectionId && r.ClassSubject!.TeacherId != null && teacherIds.Contains(r.ClassSubject.TeacherId!.Value))
            .Select(r => new { r.DayOfWeek, r.PeriodNo, TeacherId = r.ClassSubject!.TeacherId!.Value, Class = r.Section!.Class!.NameBn }).ToListAsync(ct);
        foreach (var t in teacherSlots)
        {
            var clash = others.FirstOrDefault(o => o.DayOfWeek == t.Day && o.PeriodNo == t.PeriodNo && o.TeacherId == t.TeacherId);
            if (clash != null) err[$"slot[{t.Day},{t.PeriodNo}]"] = $"{t.Teacher} এই সময়ে {clash.Class} শ্রেণিতে আছেন";
        }
        if (err.Count > 0) throw AppException.Invalid(err);

        await SavePeriodsAsync(periods, ct);
        db.RoutineSlots.RemoveRange(db.RoutineSlots.Where(r => r.SectionId == sectionId && r.SessionId == sessionId));
        foreach (var s in req.Slots)
            db.RoutineSlots.Add(new RoutineSlot { SessionId = sessionId, SectionId = sectionId, DayOfWeek = s.Day, PeriodNo = s.PeriodNo,
                StartTime = byPeriod[s.PeriodNo].Start, EndTime = byPeriod[s.PeriodNo].End, ClassSubjectId = s.ClassSubjectId, Room = Blank(s.Room) });
        await db.SaveChangesAsync(ct);
        return await RoutineAsync(classId, ct);
    }

    private async Task SavePeriodsAsync(List<PeriodDto> periods, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(periods, Json);
        var row = await db.AppSettings.FirstOrDefaultAsync(s => s.Key == PeriodsKey, ct);
        if (row is null) db.AppSettings.Add(new AppSetting { Key = PeriodsKey, Value = json });
        else row.Value = json;
    }

    /// <summary>Routine slots hang off a section; classes without sections get a default "ক".</summary>
    private async Task<int> EnsureSectionAsync(int classId, CancellationToken ct)
    {
        var id = await db.Sections.Where(s => s.ClassId == classId).OrderBy(s => s.Id).Select(s => (int?)s.Id).FirstOrDefaultAsync(ct);
        if (id is int x) return x;
        var s = new Section { ClassId = classId, Name = "ক" }; // i18n-ignore (stored section name)
        db.Sections.Add(s);
        await db.SaveChangesAsync(ct);
        return s.Id;
    }

    /// <summary>Saturday-first week without the weekly holiday (0 = Sunday … 6 = Saturday).</summary>
    private async Task<List<byte>> SchoolDaysAsync(CancellationToken ct)
    {
        var off = await db.WeeklyHolidayAsync(ct);
        return new byte[] { 6, 0, 1, 2, 3, 4, 5 }.Where(d => d != (byte)off).ToList();
    }

    private static List<PeriodDto> DefaultPeriods() => new()
    {
        new(1, new TimeOnly(7, 30), new TimeOnly(8, 15)), new(2, new TimeOnly(8, 15), new TimeOnly(9, 0)),
        new(3, new TimeOnly(9, 0), new TimeOnly(9, 45)), new(4, new TimeOnly(10, 15), new TimeOnly(11, 0)),
        new(5, new TimeOnly(11, 0), new TimeOnly(11, 45)), new(6, new TimeOnly(11, 45), new TimeOnly(12, 30)),
    };
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static string? Blank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
