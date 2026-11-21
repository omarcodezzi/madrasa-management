using Madrasa.Application.Attendance;
using Madrasa.Application.Common;
using Madrasa.Application.Exams;
using Madrasa.Application.Fees;
using Madrasa.Application.Hifz;
using Madrasa.Application.Notices;
using Madrasa.Application.Settings;
using Madrasa.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Madrasa.Application.Portal;

public record PortalChild(int StudentId, string StudentCode, string Name, string? PhotoPath, string? ClassName, int? Roll, bool IsHifz);
public record PortalHome(string Kind, IReadOnlyList<PortalChild> Children, IReadOnlyList<NoticeDto> Notices);
public record PortalStudent(PortalChild Student, StudentMonth Attendance, StudentDues Dues, IReadOnlyList<StudentExamResult> Results, HifzStudentDetail? Hifz);

/// <summary>Read-only view for a guardian (their children) or a student (themself). Only published results are shown.</summary>
public class PortalService(IAppDbContext db, IUserAdmin userAdmin, AttendanceService attendance, FeeService fees, ExamService exams,
    HifzService hifz, NoticeService notices)
{
    public async Task<PortalHome> HomeAsync(string userId, CancellationToken ct)
    {
        var (kind, ids) = await LinkedAsync(userId, ct);
        var sessionId = await db.CurrentSessionIdAsync(ct);
        var children = await db.Students.AsNoTracking().Where(s => ids.Contains(s.Id))
            .Select(s => new { s.Id, s.StudentCode, s.Name, s.PhotoPath,
                E = s.Enrollments.Where(e => e.SessionId == sessionId).Select(e => new { e.Class!.NameBn, e.Roll, e.Class.Department!.Kind }).FirstOrDefault() })
            .ToListAsync(ct);
        var audience = kind == "guardian" ? NoticeAudience.Guardians : NoticeAudience.Students;
        var list = (await notices.ListAsync(false, 50, ct)).Where(n => n.Audience == NoticeAudience.All || n.Audience == audience).Take(10)
            .Select(n => n with { CreatedBy = null, SmsQueued = 0 }).ToList();
        return new PortalHome(kind, children.Select(c => new PortalChild(c.Id, c.StudentCode, c.Name, c.PhotoPath, c.E?.NameBn, c.E?.Roll, c.E?.Kind == DepartmentKind.Hifz)).ToList(), list);
    }

    public async Task<PortalStudent> StudentAsync(string userId, int studentId, string? month, CancellationToken ct)
    {
        var home = await HomeAsync(userId, ct);
        var child = home.Children.FirstOrDefault(c => c.StudentId == studentId) ?? throw new AppException("এই শিক্ষার্থীর তথ্য দেখার অনুমতি নেই", 403);
        var results = (await exams.StudentResultsAsync(studentId, null, ct)).Where(r => r.IsPublished).ToList();
        var hz = child.IsHifz ? await hifz.StudentAsync(studentId, ct) : null;
        return new PortalStudent(child, await attendance.StudentMonthAsync(studentId, month, ct), await fees.StudentDuesAsync(studentId, ct), results, hz);
    }

    private async Task<(string Kind, List<int> Ids)> LinkedAsync(string userId, CancellationToken ct)
    {
        var links = await userAdmin.LinksAsync(userId, ct);
        if (links?.GuardianId is int g)
            return ("guardian", await db.StudentGuardians.Where(x => x.GuardianId == g && x.Student!.Status != StudentStatus.Left).Select(x => x.StudentId).ToListAsync(ct));
        if (links?.StudentId is int s) return ("student", [s]);
        throw new AppException("আপনার অ্যাকাউন্টের সাথে কোনো শিক্ষার্থী বা অভিভাবক যুক্ত নেই। অফিসে যোগাযোগ করুন।", 403);
    }
}
