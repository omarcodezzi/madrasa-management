namespace Madrasa.Application.Auth;

public record LoginRequest(string UserName, string Password);
public record AuthUser(string Id, string UserName, string FullName, string[] Roles, string[] Permissions, string Language);
public record LoginResponse(string AccessToken, DateTime ExpiresAt, AuthUser User);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest req, CancellationToken ct = default);
    Task<AuthUser?> GetUserAsync(string userId, CancellationToken ct = default);
    /// <summary>Returns null on success, otherwise a Bangla error message.</summary>
    Task<string?> ChangePasswordAsync(string userId, ChangePasswordRequest req, CancellationToken ct = default);
    /// <summary>A fresh token, e.g. after a password change invalidated the old one.</summary>
    Task<LoginResponse?> IssueAsync(string userId, CancellationToken ct = default);
}

/// <summary>Permission keys. The Angular app hides menu items using these.</summary>
public static class Permissions
{
    public const string DashboardView = "dashboard.view";
    public const string StudentsView = "students.view";
    public const string StudentsEdit = "students.edit";
    public const string AdmissionManage = "admission.manage";
    public const string AttendanceTake = "attendance.take";
    public const string HifzEdit = "hifz.edit";
    public const string ExamsMarks = "exams.marks";
    public const string ExamsPublish = "exams.publish";
    public const string FeesCollect = "fees.collect";
    public const string AccountsManage = "accounts.manage";
    public const string StaffManage = "staff.manage";
    public const string HostelManage = "hostel.manage";
    public const string LibraryManage = "library.manage";
    public const string NoticesManage = "notices.manage";
    public const string ReportsView = "reports.view";
    public const string SettingsManage = "settings.manage";
    public const string PortalView = "portal.view";

    public static readonly string[] All =
    {
        DashboardView, StudentsView, StudentsEdit, AdmissionManage, AttendanceTake, HifzEdit, ExamsMarks, ExamsPublish,
        FeesCollect, AccountsManage, StaffManage, HostelManage, LibraryManage, NoticesManage, ReportsView, SettingsManage, PortalView
    };

    /// <summary>Default role → permission map, seeded on first run and editable in Settings later.</summary>
    public static readonly Dictionary<string, string[]> Defaults = new()
    {
        ["SuperAdmin"] = All,
        ["Muhtamim"] = All.Where(p => p != PortalView).ToArray(),
        ["Office"] = new[] { DashboardView, StudentsView, StudentsEdit, AdmissionManage, AttendanceTake, NoticesManage, ReportsView, FeesCollect },
        ["Teacher"] = new[] { DashboardView, StudentsView, AttendanceTake, HifzEdit, ExamsMarks, NoticesManage },
        ["Accountant"] = new[] { DashboardView, StudentsView, FeesCollect, AccountsManage, ReportsView },
        ["HostelSupervisor"] = new[] { DashboardView, StudentsView, HostelManage, AttendanceTake },
        ["Librarian"] = new[] { DashboardView, LibraryManage },
        ["Student"] = new[] { PortalView },
        ["Guardian"] = new[] { PortalView },
    };
}
