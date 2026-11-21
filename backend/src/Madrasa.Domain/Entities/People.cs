using Madrasa.Domain.Common;
using Madrasa.Domain.Enums;

namespace Madrasa.Domain.Entities;

public class Staff : AuditableEntity, ISoftDelete
{
    public string StaffCode { get; set; } = "";
    public string Name { get; set; } = "";
    public string Designation { get; set; } = "";
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Nid { get; set; }
    public string? Address { get; set; }
    public string? Qualification { get; set; }
    public DateOnly? JoinDate { get; set; }
    public decimal? BasicSalary { get; set; }
    public string? PhotoPath { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
}

public class StaffAttendance
{
    public long Id { get; set; }
    public int StaffId { get; set; }
    public Staff? Staff { get; set; }
    public DateOnly Date { get; set; }
    public AttendanceStatus Status { get; set; }
    public TimeOnly? CheckIn { get; set; }
}

public class Guardian : BaseEntity
{
    public string Name { get; set; } = "";
    public string Relation { get; set; } = "";
    public string Phone { get; set; } = "";
    public string? AltPhone { get; set; }
    public string? Occupation { get; set; }
    public string? Nid { get; set; }
    public string? Address { get; set; }
}

public class Student : AuditableEntity, ISoftDelete
{
    /// <summary>Printed in the ID card QR ({"type":"RMTC_STUDENT","id":"..."}). Never change it.</summary>
    public string StudentCode { get; set; } = "";
    public string Name { get; set; } = "";
    public string? NameAr { get; set; }
    public Gender Gender { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? BirthRegNo { get; set; }
    public string? BloodGroup { get; set; }
    public string? FatherName { get; set; }
    public string? MotherName { get; set; }
    public string? Phone { get; set; }
    public string? PresentAddress { get; set; }
    public string? PermanentAddress { get; set; }
    public string? PhotoPath { get; set; }
    public DateOnly AdmissionDate { get; set; }
    public bool IsOrphan { get; set; }
    public bool IsResidential { get; set; }
    public StudentStatus Status { get; set; } = StudentStatus.Active;
    public bool IsDeleted { get; set; }

    public List<StudentGuardian> Guardians { get; set; } = new();
    public List<Enrollment> Enrollments { get; set; } = new();
}

public class StudentGuardian
{
    public int StudentId { get; set; }
    public Student? Student { get; set; }
    public int GuardianId { get; set; }
    public Guardian? Guardian { get; set; }
    public bool IsPrimary { get; set; }
}

public class Enrollment : BaseEntity
{
    public int StudentId { get; set; }
    public Student? Student { get; set; }
    public int SessionId { get; set; }
    public AcademicSession? Session { get; set; }
    public int ClassId { get; set; }
    public SchoolClass? Class { get; set; }
    public int? SectionId { get; set; }
    public Section? Section { get; set; }
    public int Roll { get; set; }
    public decimal? MonthlyFeeOverride { get; set; }
}

public class StudentDocument : BaseEntity
{
    public int StudentId { get; set; }
    public string Title { get; set; } = "";
    public string FilePath { get; set; } = "";
    public DateTime UploadedAt { get; set; }
}

public class AdmissionApplication : BaseEntity
{
    public string ApplicationNo { get; set; } = "";
    public int SessionId { get; set; }
    public int ClassId { get; set; }
    public SchoolClass? Class { get; set; }
    public string ApplicantName { get; set; } = "";
    public Gender Gender { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? FatherName { get; set; }
    public string GuardianPhone { get; set; } = "";
    public string? Address { get; set; }
    public string? PreviousInstitution { get; set; }
    public string? PhotoPath { get; set; }
    public AdmissionSource Source { get; set; } = AdmissionSource.Office;
    public AdmissionStage Stage { get; set; } = AdmissionStage.Applied;
    public decimal? TestMarks { get; set; }
    public string? Remarks { get; set; }
    public int? StudentId { get; set; }
    public DateTime AppliedAt { get; set; }
}
