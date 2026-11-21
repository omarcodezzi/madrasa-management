using Madrasa.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Madrasa.Application.Common;

public interface IAppDbContext
{
    /// <summary>Display name of a sign-in user (receipts, logs show who did it).</summary>
    Task<string?> UserNameAsync(string? userId, CancellationToken ct);
    DbSet<MadrasaProfile> MadrasaProfiles { get; }
    DbSet<AcademicSession> AcademicSessions { get; }
    DbSet<AppSetting> AppSettings { get; }
    DbSet<Department> Departments { get; }
    DbSet<SchoolClass> Classes { get; }
    DbSet<Section> Sections { get; }
    DbSet<Subject> Subjects { get; }
    DbSet<ClassSubject> ClassSubjects { get; }
    DbSet<RoutineSlot> RoutineSlots { get; }
    DbSet<Staff> Staff { get; }
    DbSet<StaffAttendance> StaffAttendance { get; }
    DbSet<Guardian> Guardians { get; }
    DbSet<Student> Students { get; }
    DbSet<StudentGuardian> StudentGuardians { get; }
    DbSet<Enrollment> Enrollments { get; }
    DbSet<StudentDocument> StudentDocuments { get; }
    DbSet<AdmissionApplication> AdmissionApplications { get; }
    DbSet<StudentAttendance> StudentAttendance { get; }
    DbSet<Holiday> Holidays { get; }
    DbSet<HifzJuzProgress> HifzJuzProgress { get; }
    DbSet<HifzDailyLog> HifzDailyLogs { get; }
    DbSet<Exam> Exams { get; }
    DbSet<ExamMark> ExamMarks { get; }
    DbSet<GradeRule> GradeRules { get; }
    DbSet<ExamResult> ExamResults { get; }
    DbSet<Fund> Funds { get; }
    DbSet<FeeHead> FeeHeads { get; }
    DbSet<StudentDiscount> StudentDiscounts { get; }
    DbSet<FeeInvoice> FeeInvoices { get; }
    DbSet<FeeReceipt> FeeReceipts { get; }
    DbSet<FeeReceiptLine> FeeReceiptLines { get; }
    DbSet<SalaryPayment> SalaryPayments { get; }
    DbSet<Donor> Donors { get; }
    DbSet<LedgerCategory> LedgerCategories { get; }
    DbSet<LedgerEntry> LedgerEntries { get; }
    DbSet<HostelRoom> HostelRooms { get; }
    DbSet<HostelAllocation> HostelAllocations { get; }
    DbSet<HostelLeave> HostelLeaves { get; }
    DbSet<Book> Books { get; }
    DbSet<BookIssue> BookIssues { get; }
    DbSet<Notice> Notices { get; }
    DbSet<SchoolEvent> Events { get; }
    DbSet<MessageLog> MessageLogs { get; }
    DbSet<AuditLog> AuditLogs { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

public interface ICurrentUser
{
    string? UserId { get; }
}
