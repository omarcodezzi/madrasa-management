using Madrasa.Domain.Enums;

namespace Madrasa.Application.Academic;

public record SectionDto(int Id, string Name, int? Capacity, int StudentCount);
public record ClassOverview(int Id, string NameBn, string? NameAr, int DepartmentId, decimal? MonthlyFee, int SortOrder,
    int StudentCount, int SubjectCount, IReadOnlyList<SectionDto> Sections);
public record DepartmentOverview(int Id, string NameBn, string? NameAr, DepartmentKind Kind, IReadOnlyList<ClassOverview> Classes);
public record ClassInput(int DepartmentId, string NameBn, string? NameAr, decimal? MonthlyFee, int? SortOrder);

public record SubjectDto(int Id, string NameBn, string? NameAr, string? Code);
public record TeacherDto(int Id, string Name, string Designation, string? Phone);
public record ClassSubjectDto(int Id, int SubjectId, string SubjectName, string? SubjectNameAr, int? TeacherId, string? TeacherName, int FullMarks, int PassMarks);
/// <summary>Give SubjectId for an existing kitab, or NewSubjectName to create one on the fly.</summary>
public record ClassSubjectInput(int? SubjectId, string? NewSubjectName, string? NewSubjectNameAr, int? TeacherId, int FullMarks, int PassMarks);

public record PeriodDto(byte PeriodNo, TimeOnly Start, TimeOnly End);
public record SlotDto(byte Day, byte PeriodNo, int ClassSubjectId, string SubjectName, string? TeacherName, string? Room);
public record RoutineDto(int ClassId, string ClassName, IReadOnlyList<byte> Days, IReadOnlyList<PeriodDto> Periods, IReadOnlyList<SlotDto> Slots);
public record SlotInput(byte Day, byte PeriodNo, int ClassSubjectId, string? Room);
public record SaveRoutineRequest(IReadOnlyList<PeriodDto> Periods, IReadOnlyList<SlotInput> Slots);
