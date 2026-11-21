using Madrasa.Domain.Enums;

namespace Madrasa.Application.Students;

public record StudentListQuery(string? Q, int? ClassId, Gender? Gender, StudentStatus? Status, string? Sort, int Page = 1, int PageSize = 25);

public record StudentRow(int Id, string StudentCode, string Name, Gender Gender, int? ClassId, string? ClassName, int? Roll,
    string? FatherName, string? Phone, string? PhotoPath, StudentStatus Status, bool IsOrphan, bool IsResidential, DateOnly AdmissionDate);

public record GuardianDto(int? Id, string Name, string Relation, string Phone, string? AltPhone, string? Occupation, string? Nid, string? Address, bool IsPrimary);

public record StudentDetail(
    int Id, string StudentCode, string Name, string? NameAr, Gender Gender, DateOnly? DateOfBirth, string? BirthRegNo, string? BloodGroup,
    string? FatherName, string? MotherName, string? Phone, string? PresentAddress, string? PermanentAddress, string? PhotoPath,
    DateOnly AdmissionDate, bool IsOrphan, bool IsResidential, StudentStatus Status,
    int? ClassId, string? ClassName, string? DepartmentName, int? SectionId, int? Roll, decimal? MonthlyFee, decimal? MonthlyFeeOverride,
    IReadOnlyList<GuardianDto> Guardians);

/// <summary>Create/update payload. Validation messages are Bangla.</summary>
public record StudentInput(
    string Name, string? NameAr, Gender Gender, DateOnly? DateOfBirth, string? BirthRegNo, string? BloodGroup,
    string? FatherName, string? MotherName, string? Phone, string? PresentAddress, string? PermanentAddress,
    DateOnly? AdmissionDate, bool IsOrphan, bool IsResidential, StudentStatus? Status,
    int ClassId, int? SectionId, int? Roll, decimal? MonthlyFeeOverride,
    IReadOnlyList<GuardianDto>? Guardians,
    /// <summary>Optional data URL (image/jpeg or image/png), resized in the browser.</summary>
    string? PhotoDataUrl);

public record NextCodes(string StudentCode, int Roll);
