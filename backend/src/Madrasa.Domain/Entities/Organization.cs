using Madrasa.Domain.Common;
using Madrasa.Domain.Enums;

namespace Madrasa.Domain.Entities;

public class MadrasaProfile : BaseEntity
{
    public string NameBn { get; set; } = "";
    public string? NameEn { get; set; }
    public string? NameAr { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? LogoPath { get; set; }
    public int? EstablishedYear { get; set; }
    public string StudentIdPrefix { get; set; } = "RM";
}

public class AcademicSession : BaseEntity
{
    public string Name { get; set; } = "";
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsCurrent { get; set; }
}

public class AppSetting
{
    public string Key { get; set; } = "";
    public string? Value { get; set; }
}

public class Department : BaseEntity
{
    public string NameBn { get; set; } = "";
    public string? NameAr { get; set; }
    public DepartmentKind Kind { get; set; }
    public int SortOrder { get; set; }
    public List<SchoolClass> Classes { get; set; } = new();
}

public class SchoolClass : BaseEntity
{
    public int DepartmentId { get; set; }
    public Department? Department { get; set; }
    public string NameBn { get; set; } = "";
    public string? NameAr { get; set; }
    public int SortOrder { get; set; }
    public decimal? MonthlyFee { get; set; }
    public List<Section> Sections { get; set; } = new();
}

public class Section : BaseEntity
{
    public int ClassId { get; set; }
    public SchoolClass? Class { get; set; }
    public string Name { get; set; } = "";
    public int? Capacity { get; set; }
}

public class Subject : BaseEntity
{
    public string NameBn { get; set; } = "";
    public string? NameAr { get; set; }
    public string? Code { get; set; }
}

public class ClassSubject : BaseEntity
{
    public int SessionId { get; set; }
    public AcademicSession? Session { get; set; }
    public int ClassId { get; set; }
    public SchoolClass? Class { get; set; }
    public int SubjectId { get; set; }
    public Subject? Subject { get; set; }
    public int? TeacherId { get; set; }
    public Staff? Teacher { get; set; }
    public int FullMarks { get; set; } = 100;
    public int PassMarks { get; set; } = 33;
}

public class RoutineSlot : BaseEntity
{
    public int SessionId { get; set; }
    public int SectionId { get; set; }
    public Section? Section { get; set; }
    public byte DayOfWeek { get; set; }
    public byte PeriodNo { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int ClassSubjectId { get; set; }
    public ClassSubject? ClassSubject { get; set; }
    public string? Room { get; set; }
}
