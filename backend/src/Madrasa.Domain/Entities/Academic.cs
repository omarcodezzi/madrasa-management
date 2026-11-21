using Madrasa.Domain.Common;
using Madrasa.Domain.Enums;

namespace Madrasa.Domain.Entities;

public class StudentAttendance
{
    public long Id { get; set; }
    public int StudentId { get; set; }
    public Student? Student { get; set; }
    public DateOnly Date { get; set; }
    public AttendanceStatus Status { get; set; }
    public TimeOnly? CheckIn { get; set; }
    public AttendanceMethod Method { get; set; } = AttendanceMethod.Manual;
    public string? MarkedBy { get; set; }
}

public class Holiday : BaseEntity
{
    public DateOnly Date { get; set; }
    public string Title { get; set; } = "";
}

public class HifzJuzProgress
{
    public int StudentId { get; set; }
    public byte JuzNo { get; set; }
    public JuzStatus Status { get; set; }
    public DateOnly? CompletedOn { get; set; }
}

public class HifzDailyLog
{
    public long Id { get; set; }
    public int StudentId { get; set; }
    public DateOnly Date { get; set; }
    public byte? SabakJuz { get; set; }
    public short? SabakFromPage { get; set; }
    public short? SabakToPage { get; set; }
    public string? SabakText { get; set; }
    public string? SabqiText { get; set; }
    public string? ManzilText { get; set; }
    public int Mistakes { get; set; }
    public byte? Quality { get; set; }
    public string? Remarks { get; set; }
    public int? TeacherId { get; set; }
}

public class Exam : BaseEntity
{
    public int SessionId { get; set; }
    public string Name { get; set; } = "";
    public DateOnly? StartDate { get; set; }
    public bool IsPublished { get; set; }
}

public class ExamMark
{
    public long Id { get; set; }
    public int ExamId { get; set; }
    public int StudentId { get; set; }
    public int ClassSubjectId { get; set; }
    public ClassSubject? ClassSubject { get; set; }
    public decimal? Marks { get; set; }
    public bool IsAbsent { get; set; }
}

public class GradeRule : BaseEntity
{
    public string NameBn { get; set; } = "";
    public string? NameAr { get; set; }
    public decimal MinAverage { get; set; }
    public int SortOrder { get; set; }
}

public class ExamResult
{
    public int ExamId { get; set; }
    public int StudentId { get; set; }
    public decimal Total { get; set; }
    public decimal Average { get; set; }
    public int? GradeRuleId { get; set; }
    public GradeRule? GradeRule { get; set; }
    public int FailedSubjects { get; set; }
    public int? MeritPosition { get; set; }
}
