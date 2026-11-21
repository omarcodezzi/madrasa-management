using System.Linq.Expressions;
using Madrasa.Application.Common;
using Madrasa.Domain.Common;
using Madrasa.Domain.Entities;
using Madrasa.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Madrasa.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUser currentUser)
    : IdentityDbContext<AppUser>(options), IAppDbContext
{
    public DbSet<MadrasaProfile> MadrasaProfiles => Set<MadrasaProfile>();
    public DbSet<AcademicSession> AcademicSessions => Set<AcademicSession>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<SchoolClass> Classes => Set<SchoolClass>();
    public DbSet<Section> Sections => Set<Section>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<ClassSubject> ClassSubjects => Set<ClassSubject>();
    public DbSet<RoutineSlot> RoutineSlots => Set<RoutineSlot>();
    public DbSet<Staff> Staff => Set<Staff>();
    public DbSet<StaffAttendance> StaffAttendance => Set<StaffAttendance>();
    public DbSet<Guardian> Guardians => Set<Guardian>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<StudentGuardian> StudentGuardians => Set<StudentGuardian>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<StudentDocument> StudentDocuments => Set<StudentDocument>();
    public DbSet<AdmissionApplication> AdmissionApplications => Set<AdmissionApplication>();
    public DbSet<StudentAttendance> StudentAttendance => Set<StudentAttendance>();
    public DbSet<Holiday> Holidays => Set<Holiday>();
    public DbSet<HifzJuzProgress> HifzJuzProgress => Set<HifzJuzProgress>();
    public DbSet<HifzDailyLog> HifzDailyLogs => Set<HifzDailyLog>();
    public DbSet<Exam> Exams => Set<Exam>();
    public DbSet<ExamMark> ExamMarks => Set<ExamMark>();
    public DbSet<GradeRule> GradeRules => Set<GradeRule>();
    public DbSet<ExamResult> ExamResults => Set<ExamResult>();
    public DbSet<Fund> Funds => Set<Fund>();
    public DbSet<FeeHead> FeeHeads => Set<FeeHead>();
    public DbSet<StudentDiscount> StudentDiscounts => Set<StudentDiscount>();
    public DbSet<FeeInvoice> FeeInvoices => Set<FeeInvoice>();
    public DbSet<FeeReceipt> FeeReceipts => Set<FeeReceipt>();
    public DbSet<FeeReceiptLine> FeeReceiptLines => Set<FeeReceiptLine>();
    public DbSet<SalaryPayment> SalaryPayments => Set<SalaryPayment>();
    public DbSet<Donor> Donors => Set<Donor>();
    public DbSet<LedgerCategory> LedgerCategories => Set<LedgerCategory>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
    public DbSet<HostelRoom> HostelRooms => Set<HostelRoom>();
    public DbSet<HostelAllocation> HostelAllocations => Set<HostelAllocation>();
    public DbSet<HostelLeave> HostelLeaves => Set<HostelLeave>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<BookIssue> BookIssues => Set<BookIssue>();
    public DbSet<Notice> Notices => Set<Notice>();
    public DbSet<SchoolEvent> Events => Set<SchoolEvent>();
    public DbSet<MessageLog> MessageLogs => Set<MessageLog>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // Defaults for every entity: money precision, Bangla-safe strings
        foreach (var p in b.Model.GetEntityTypes().SelectMany(t => t.GetProperties()))
        {
            if (p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)) p.SetPrecision(12); 
            if (p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)) p.SetScale(2);
        }

        // Soft-delete filters
        foreach (var t in b.Model.GetEntityTypes().Where(t => typeof(ISoftDelete).IsAssignableFrom(t.ClrType)))
        {
            var param = Expression.Parameter(t.ClrType, "e");
            var body = Expression.Equal(Expression.Property(param, nameof(ISoftDelete.IsDeleted)), Expression.Constant(false));
            b.Entity(t.ClrType).HasQueryFilter(Expression.Lambda(body, param));
        }

        b.Entity<MadrasaProfile>().ToTable("MadrasaProfile");
        b.Entity<AppSetting>().HasKey(x => x.Key);
        b.Entity<SchoolClass>().ToTable("Classes");
        b.Entity<SchoolEvent>().ToTable("Events");
        b.Entity<Staff>().ToTable("Staff").HasIndex(x => x.StaffCode).IsUnique();
        b.Entity<StaffAttendance>().ToTable("StaffAttendance").HasIndex(x => new { x.StaffId, x.Date }).IsUnique();

        b.Entity<ClassSubject>().HasIndex(x => new { x.SessionId, x.ClassId, x.SubjectId }).IsUnique();
        b.Entity<ClassSubject>().HasOne(x => x.Class).WithMany().OnDelete(DeleteBehavior.Restrict);
        b.Entity<RoutineSlot>().HasOne(x => x.ClassSubject).WithMany().OnDelete(DeleteBehavior.Restrict);
        b.Entity<RoutineSlot>().HasOne(x => x.Section).WithMany().OnDelete(DeleteBehavior.Restrict);

        b.Entity<Student>(e =>
        {
            e.HasIndex(x => x.StudentCode).IsUnique();
            e.HasIndex(x => x.Name);
            e.HasIndex(x => x.Phone);
            e.Property(x => x.StudentCode).HasMaxLength(30);
            e.Property(x => x.Name).HasMaxLength(150);
            e.Property(x => x.Phone).HasMaxLength(30);
        });
        b.Entity<Guardian>().HasIndex(x => x.Phone);
        b.Entity<Guardian>().Property(x => x.Phone).HasMaxLength(30);
        b.Entity<StudentGuardian>().HasKey(x => new { x.StudentId, x.GuardianId });

        b.Entity<Enrollment>(e =>
        {
            e.HasIndex(x => new { x.StudentId, x.SessionId }).IsUnique();
            e.HasIndex(x => new { x.SessionId, x.ClassId, x.SectionId, x.Roll }).IsUnique();
            e.HasOne(x => x.Class).WithMany().OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Section).WithMany().OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Session).WithMany().OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<AdmissionApplication>().HasIndex(x => x.ApplicationNo).IsUnique();
        b.Entity<AdmissionApplication>().HasOne(x => x.Class).WithMany().OnDelete(DeleteBehavior.Restrict);

        b.Entity<StudentAttendance>().ToTable("StudentAttendance").HasIndex(x => new { x.StudentId, x.Date }).IsUnique();
        b.Entity<StudentAttendance>().HasIndex(x => x.Date);
        b.Entity<Holiday>().HasIndex(x => x.Date).IsUnique();

        b.Entity<HifzJuzProgress>().ToTable("HifzJuzProgress").HasKey(x => new { x.StudentId, x.JuzNo });
        b.Entity<HifzDailyLog>().HasIndex(x => new { x.StudentId, x.Date }).IsUnique();

        b.Entity<ExamMark>().HasIndex(x => new { x.ExamId, x.StudentId, x.ClassSubjectId }).IsUnique();
        b.Entity<ExamMark>().HasOne(x => x.ClassSubject).WithMany().OnDelete(DeleteBehavior.Restrict);
        b.Entity<ExamResult>().HasKey(x => new { x.ExamId, x.StudentId });

        b.Entity<Fund>().HasIndex(x => x.Code).IsUnique();
        b.Entity<FeeInvoice>().HasIndex(x => new { x.StudentId, x.FeeHeadId, x.Month }).IsUnique();
        b.Entity<FeeReceipt>().HasIndex(x => x.ReceiptNo).IsUnique();
        b.Entity<FeeReceipt>().HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.ReceiptId);
        b.Entity<FeeReceiptLine>().HasOne(x => x.FeeHead).WithMany().OnDelete(DeleteBehavior.Restrict);
        b.Entity<SalaryPayment>().HasIndex(x => new { x.StaffId, x.Month }).IsUnique();
        b.Entity<LedgerEntry>().HasIndex(x => new { x.FundId, x.Date });
        b.Entity<LedgerEntry>().HasOne(x => x.Fund).WithMany().OnDelete(DeleteBehavior.Restrict);
        b.Entity<LedgerEntry>().HasOne(x => x.Category).WithMany().OnDelete(DeleteBehavior.Restrict);

        b.Entity<HifzJuzProgress>().Property(x => x.JuzNo);
        b.Entity<HifzJuzProgress>().ToTable(t => t.HasCheckConstraint("CK_Juz", "[JuzNo] BETWEEN 1 AND 30"));
        b.Entity<LedgerEntry>().ToTable(t => t.HasCheckConstraint("CK_Ledger_Amount", "[Amount] > 0"));

        foreach (var fk in b.Model.GetEntityTypes().SelectMany(t => t.GetForeignKeys())
                     .Where(f => f.DeleteBehavior == DeleteBehavior.Cascade && f.DeclaringEntityType.ClrType.Namespace == typeof(Student).Namespace))
            fk.DeleteBehavior = DeleteBehavior.Restrict;

        SeedData.Apply(b);
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        foreach (var e in ChangeTracker.Entries<AuditableEntity>())
        {
            if (e.State == EntityState.Added) { e.Entity.CreatedAt = now; e.Entity.CreatedBy = currentUser.UserId; }
            if (e.State == EntityState.Modified) { e.Entity.UpdatedAt = now; e.Entity.UpdatedBy = currentUser.UserId; }
        }
        foreach (var e in ChangeTracker.Entries<ISoftDelete>().Where(e => e.State == EntityState.Deleted))
        {
            e.State = EntityState.Modified;
            e.Entity.IsDeleted = true;
        }
        return base.SaveChangesAsync(ct);
    }
}
