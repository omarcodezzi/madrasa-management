using Madrasa.Application.Academic;
using Madrasa.Application.Common;
using Madrasa.Domain.Entities;
using Madrasa.Domain.Enums;
using Madrasa.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace Madrasa.Application.Exams;

public record ExamDto(int Id, string Name, DateOnly? StartDate, bool IsPublished, int MarksEntered, int MarksExpected);
public record ExamInput(string Name, DateOnly? StartDate);
public record MarkCell(int ClassSubjectId, decimal? Marks, bool IsAbsent);
public record MarkRow(int StudentId, string StudentCode, string Name, int Roll, IReadOnlyList<MarkCell> Cells);
public record MarkGrid(int ExamId, string ExamName, bool IsPublished, int ClassId, string ClassName, IReadOnlyList<ClassSubjectDto> Subjects, IReadOnlyList<MarkRow> Rows);
public record MarkEntryInput(int StudentId, int ClassSubjectId, decimal? Marks, bool IsAbsent);
public record SaveMarksRequest(int ClassId, IReadOnlyList<MarkEntryInput> Entries);
public record GradeDto(int Id, string NameBn, string? NameAr, decimal MinAverage);
public record ResultRow(int StudentId, string StudentCode, string Name, int Roll, IReadOnlyList<MarkCell> Cells,
    decimal Total, decimal Average, int FailedSubjects, int? GradeId, string? Grade, string? GradeAr, int? Merit, bool Complete);
public record ResultSheet(int ExamId, string ExamName, bool IsPublished, int ClassId, string ClassName, IReadOnlyList<ClassSubjectDto> Subjects,
    IReadOnlyList<GradeDto> Grades, IReadOnlyList<ResultRow> Rows);
public record StudentExamResult(int ExamId, string ExamName, DateOnly? StartDate, bool IsPublished, string? ClassName,
    IReadOnlyList<SubjectResult> Subjects, decimal Total, decimal Average, int FailedSubjects, string? Grade, string? GradeAr, int? Merit, int ClassSize);
public record SubjectResult(string Subject, string? SubjectAr, int FullMarks, int PassMarks, decimal? Marks, bool IsAbsent);

/// <summary>Exams, the marks grid, Befaq-style division and merit list.</summary>
public class ExamService(IAppDbContext db, AcademicService academic)
{
    public async Task<List<ExamDto>> ListAsync(CancellationToken ct)
    {
        var sessionId = await db.CurrentSessionIdAsync(ct);
        // Expected marks = Σ over classes (students × kitabs)
        var perClass = await db.Classes.AsNoTracking().Select(c => new
        {
            Students = db.Enrollments.Count(e => e.ClassId == c.Id && e.SessionId == sessionId && !e.Student!.IsDeleted && e.Student.Status == StudentStatus.Active),
            Subjects = db.ClassSubjects.Count(s => s.ClassId == c.Id && s.SessionId == sessionId),
        }).ToListAsync(ct);
        var expected = perClass.Sum(x => x.Students * x.Subjects);
        return await db.Exams.AsNoTracking().Where(e => e.SessionId == sessionId).OrderByDescending(e => e.StartDate).ThenByDescending(e => e.Id)
            .Select(e => new ExamDto(e.Id, e.Name, e.StartDate, e.IsPublished,
                db.ExamMarks.Count(m => m.ExamId == e.Id && (m.Marks != null || m.IsAbsent)), expected)).ToListAsync(ct);
    }

    public async Task<ExamDto> SaveAsync(int? id, ExamInput input, CancellationToken ct)
    {
        var sessionId = await db.CurrentSessionIdAsync(ct);
        var name = input.Name?.Trim() ?? "";
        var err = new Dictionary<string, string>();
        if (name.Length == 0) err["name"] = "পরীক্ষার নাম লিখুন";
        else if (await db.Exams.AnyAsync(e => e.SessionId == sessionId && e.Name == name && e.Id != (id ?? 0), ct)) err["name"] = "এই নামে এই শিক্ষাবর্ষে আরেকটি পরীক্ষা আছে";
        if (err.Count > 0) throw AppException.Invalid(err);
        Exam e;
        if (id is null) { e = new Exam { SessionId = sessionId }; db.Exams.Add(e); }
        else e = await db.Exams.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw AppException.NotFound("পরীক্ষা");
        e.Name = name; e.StartDate = input.StartDate;
        await db.SaveChangesAsync(ct);
        return (await ListAsync(ct)).First(x => x.Id == e.Id);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var e = await db.Exams.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw AppException.NotFound("পরীক্ষা");
        if (e.IsPublished) throw new AppException("প্রকাশিত পরীক্ষা মুছা যাবে না। আগে প্রকাশ বাতিল করুন।", 409);
        if (await db.ExamMarks.AnyAsync(m => m.ExamId == id && (m.Marks != null || m.IsAbsent), ct))
            throw new AppException("এই পরীক্ষার নম্বর দেওয়া হয়েছে, তাই মুছা যাবে না।", 409);
        db.ExamMarks.RemoveRange(db.ExamMarks.Where(m => m.ExamId == id));
        db.ExamResults.RemoveRange(db.ExamResults.Where(r => r.ExamId == id));
        db.Exams.Remove(e);
        await db.SaveChangesAsync(ct);
    }

    public async Task<MarkGrid> GridAsync(int examId, int classId, CancellationToken ct)
    {
        var (exam, cls, subjects, students) = await LoadAsync(examId, classId, ct);
        var ids = subjects.Select(s => s.Id).ToList();
        var marks = await db.ExamMarks.AsNoTracking().Where(m => m.ExamId == examId && ids.Contains(m.ClassSubjectId)).ToListAsync(ct);
        var rows = students.Select(s => new MarkRow(s.StudentId, s.StudentCode, s.Name, s.Roll, Cells(subjects, marks, s.StudentId))).ToList();
        return new MarkGrid(exam.Id, exam.Name, exam.IsPublished, classId, cls, subjects, rows);
    }

    public async Task<int> SaveMarksAsync(int examId, SaveMarksRequest req, CancellationToken ct)
    {
        var (exam, _, subjects, students) = await LoadAsync(examId, req.ClassId, ct);
        if (exam.IsPublished) throw new AppException("ফলাফল প্রকাশিত হয়েছে। নম্বর বদলাতে আগে প্রকাশ বাতিল করুন।", 409);
        var subj = subjects.ToDictionary(s => s.Id);
        var studentIds = students.Select(s => s.StudentId).ToHashSet();
        var err = new Dictionary<string, string>();
        foreach (var e in req.Entries)
        {
            var key = $"{e.StudentId}:{e.ClassSubjectId}";
            if (!subj.TryGetValue(e.ClassSubjectId, out var cs)) { err[key] = "এই কিতাব এই শ্রেণির নয়"; continue; }
            if (!studentIds.Contains(e.StudentId)) { err[key] = "শিক্ষার্থী এই শ্রেণির নয়"; continue; }
            if (e.Marks is decimal m && (m < 0 || m > cs.FullMarks)) err[key] = $"নম্বর ০ থেকে {cs.FullMarks} এর মধ্যে দিন";
            if (e.Marks is decimal m2 && decimal.Round(m2 * 2) != m2 * 2) err[key] = "নম্বর পূর্ণ বা অর্ধেক হতে পারে";
        }
        if (err.Count > 0) throw AppException.Invalid(err);

        var ids = subjects.Select(s => s.Id).ToList();
        var existing = await db.ExamMarks.Where(m => m.ExamId == examId && ids.Contains(m.ClassSubjectId)).ToDictionaryAsync(m => (m.StudentId, m.ClassSubjectId), ct);
        int changed = 0;
        foreach (var e in req.Entries)
        {
            var marks = e.IsAbsent ? null : e.Marks;
            if (existing.TryGetValue((e.StudentId, e.ClassSubjectId), out var row))
            {
                if (row.Marks == marks && row.IsAbsent == e.IsAbsent) continue;
                if (marks is null && !e.IsAbsent) db.ExamMarks.Remove(row);
                else { row.Marks = marks; row.IsAbsent = e.IsAbsent; }
            }
            else if (marks is not null || e.IsAbsent)
                db.ExamMarks.Add(new ExamMark { ExamId = examId, StudentId = e.StudentId, ClassSubjectId = e.ClassSubjectId, Marks = marks, IsAbsent = e.IsAbsent });
            else continue;
            changed++;
        }
        await db.SaveChangesAsync(ct);
        await RecalculateAsync(examId, req.ClassId, ct);
        return changed;
    }

    public async Task<ResultSheet> ResultsAsync(int examId, int classId, CancellationToken ct)
    {
        var (exam, cls, subjects, students) = await LoadAsync(examId, classId, ct);
        var rules = await GradesAsync(ct);
        var ids = subjects.Select(s => s.Id).ToList();
        var marks = await db.ExamMarks.AsNoTracking().Where(m => m.ExamId == examId && ids.Contains(m.ClassSubjectId)).ToListAsync(ct);
        var computed = Compute(subjects, students.Select(s => s.StudentId).ToList(), marks, rules);
        var rows = students.Select(s =>
        {
            var c = computed[s.StudentId];
            return new ResultRow(s.StudentId, s.StudentCode, s.Name, s.Roll, Cells(subjects, marks, s.StudentId), c.Total, c.Average, c.Failed,
                c.Grade?.Id, c.Grade?.NameBn, c.Grade?.NameAr, c.Merit, c.Complete);
        }).OrderBy(r => r.Merit ?? int.MaxValue).ThenBy(r => r.Roll).ToList();
        return new ResultSheet(exam.Id, exam.Name, exam.IsPublished, classId, cls, subjects,
            rules.Select(r => new GradeDto(r.Id, r.NameBn, r.NameAr, r.MinAverage)).ToList(), rows);
    }

    /// <summary>Publishing freezes marks and stores results for every class (portal and mark sheets read them).</summary>
    public async Task<ExamDto> PublishAsync(int examId, bool publish, CancellationToken ct)
    {
        var exam = await db.Exams.FirstOrDefaultAsync(e => e.Id == examId, ct) ?? throw AppException.NotFound("পরীক্ষা");
        if (publish)
        {
            if (!await db.ExamMarks.AnyAsync(m => m.ExamId == examId, ct)) throw new AppException("কোনো নম্বর দেওয়া হয়নি, তাই প্রকাশ করা যাবে না।", 409);
            var sessionId = exam.SessionId;
            var classIds = await db.ClassSubjects.Where(s => s.SessionId == sessionId).Select(s => s.ClassId).Distinct().ToListAsync(ct);
            foreach (var c in classIds) await RecalculateAsync(examId, c, ct);
        }
        exam.IsPublished = publish;
        await db.SaveChangesAsync(ct);
        return (await ListAsync(ct)).First(x => x.Id == examId);
    }

    public async Task<List<StudentExamResult>> StudentResultsAsync(int studentId, int? examId, CancellationToken ct)
    {
        var sessionId = await db.CurrentSessionIdAsync(ct);
        var enr = await db.Enrollments.AsNoTracking().Where(e => e.StudentId == studentId && e.SessionId == sessionId)
            .Select(e => new { e.ClassId, e.Class!.NameBn }).FirstOrDefaultAsync(ct);
        if (enr is null) return new();
        var exams = await db.Exams.AsNoTracking().Where(e => e.SessionId == sessionId && (examId == null || e.Id == examId))
            .OrderByDescending(e => e.StartDate).ThenByDescending(e => e.Id).ToListAsync(ct);
        var subjects = await academic.ClassSubjectsAsync(enr.ClassId, ct);
        var rules = await GradesAsync(ct);
        var students = await ClassStudentsAsync(enr.ClassId, sessionId, ct);
        var ids = subjects.Select(s => s.Id).ToList();
        var list = new List<StudentExamResult>();
        foreach (var ex in exams)
        {
            var marks = await db.ExamMarks.AsNoTracking().Where(m => m.ExamId == ex.Id && ids.Contains(m.ClassSubjectId)).ToListAsync(ct);
            if (!marks.Any(m => m.StudentId == studentId)) continue;
            var c = Compute(subjects, students.Select(s => s.StudentId).ToList(), marks, rules)[studentId];
            list.Add(new StudentExamResult(ex.Id, ex.Name, ex.StartDate, ex.IsPublished, enr.NameBn,
                subjects.Select(s => { var m = marks.FirstOrDefault(x => x.StudentId == studentId && x.ClassSubjectId == s.Id);
                    return new SubjectResult(s.SubjectName, s.SubjectNameAr, s.FullMarks, s.PassMarks, m?.Marks, m?.IsAbsent ?? false); }).ToList(),
                c.Total, c.Average, c.Failed, c.Grade?.NameBn, c.Grade?.NameAr, c.Merit, students.Count));
        }
        return list;
    }

    // ---- helpers ----

    private record StudentLite(int StudentId, string StudentCode, string Name, int Roll);
    private record Computed(decimal Total, decimal Average, int Failed, GradeRule? Grade, int? Merit, bool Complete);

    private async Task<(Exam exam, string cls, List<ClassSubjectDto> subjects, List<StudentLite> students)> LoadAsync(int examId, int classId, CancellationToken ct)
    {
        var exam = await db.Exams.AsNoTracking().FirstOrDefaultAsync(e => e.Id == examId, ct) ?? throw AppException.NotFound("পরীক্ষা");
        var cls = await db.Classes.AsNoTracking().Where(c => c.Id == classId).Select(c => c.NameBn).FirstOrDefaultAsync(ct) ?? throw AppException.NotFound("শ্রেণি");
        var subjects = await academic.ClassSubjectsAsync(classId, ct);
        return (exam, cls, subjects, await ClassStudentsAsync(classId, exam.SessionId, ct));
    }

    private Task<List<StudentLite>> ClassStudentsAsync(int classId, int sessionId, CancellationToken ct) =>
        db.Enrollments.AsNoTracking().Where(e => e.ClassId == classId && e.SessionId == sessionId && !e.Student!.IsDeleted && e.Student.Status == StudentStatus.Active)
            .OrderBy(e => e.Roll).Select(e => new StudentLite(e.StudentId, e.Student!.StudentCode, e.Student.Name, e.Roll)).ToListAsync(ct);

    private Task<List<GradeRule>> GradesAsync(CancellationToken ct) => db.GradeRules.AsNoTracking().OrderBy(r => r.SortOrder).ToListAsync(ct);

    private static List<MarkCell> Cells(List<ClassSubjectDto> subjects, List<ExamMark> marks, int studentId) =>
        subjects.Select(s => { var m = marks.FirstOrDefault(x => x.StudentId == studentId && x.ClassSubjectId == s.Id);
            return new MarkCell(s.Id, m?.Marks, m?.IsAbsent ?? false); }).ToList();

    /// <summary>
    /// Division per student, then merit among those who passed every kitab, by total (ties share a place: 1, 2, 2, 4).
    /// Students with missing marks get no division or merit until every kitab has a mark.
    /// </summary>
    private static Dictionary<int, Computed> Compute(List<ClassSubjectDto> subjects, List<int> students, List<ExamMark> marks, List<GradeRule> rules)
    {
        var raw = students.ToDictionary(id => id, id =>
        {
            var mine = marks.Where(m => m.StudentId == id).ToList();
            bool complete = subjects.Count > 0 && subjects.All(s => mine.Any(m => m.ClassSubjectId == s.Id && (m.Marks != null || m.IsAbsent)));
            var outcome = ResultCalculator.Calculate(subjects.Select(s =>
            {
                var m = mine.FirstOrDefault(x => x.ClassSubjectId == s.Id);
                return new ResultCalculator.SubjectMark(m?.Marks, m?.IsAbsent ?? false, s.PassMarks);
            }).ToList(), rules);
            return (outcome, complete);
        });
        var ranked = raw.Where(r => r.Value.complete && r.Value.outcome.FailedSubjects == 0).OrderByDescending(r => r.Value.outcome.Total).ToList();
        var merit = new Dictionary<int, int>();
        for (int i = 0; i < ranked.Count; i++)
            merit[ranked[i].Key] = i > 0 && ranked[i].Value.outcome.Total == ranked[i - 1].Value.outcome.Total ? merit[ranked[i - 1].Key] : i + 1;
        return raw.ToDictionary(r => r.Key, r => new Computed(r.Value.outcome.Total, r.Value.outcome.Average, r.Value.outcome.FailedSubjects,
            r.Value.complete ? r.Value.outcome.Grade : null, merit.TryGetValue(r.Key, out var p) ? p : null, r.Value.complete));
    }

    private async Task RecalculateAsync(int examId, int classId, CancellationToken ct)
    {
        var (_, _, subjects, students) = await LoadAsync(examId, classId, ct);
        var ids = subjects.Select(s => s.Id).ToList();
        var marks = await db.ExamMarks.AsNoTracking().Where(m => m.ExamId == examId && ids.Contains(m.ClassSubjectId)).ToListAsync(ct);
        var computed = Compute(subjects, students.Select(s => s.StudentId).ToList(), marks, await GradesAsync(ct));
        var sIds = students.Select(s => s.StudentId).ToList();
        var old = await db.ExamResults.Where(r => r.ExamId == examId && sIds.Contains(r.StudentId)).ToDictionaryAsync(r => r.StudentId, ct);
        foreach (var (sid, c) in computed)
        {
            if (!marks.Any(m => m.StudentId == sid)) { if (old.TryGetValue(sid, out var o)) db.ExamResults.Remove(o); continue; }
            if (!old.TryGetValue(sid, out var r)) { r = new ExamResult { ExamId = examId, StudentId = sid }; db.ExamResults.Add(r); }
            r.Total = c.Total; r.Average = c.Average; r.FailedSubjects = c.Failed; r.GradeRuleId = c.Grade?.Id; r.MeritPosition = c.Merit;
        }
        await db.SaveChangesAsync(ct);
    }
}
