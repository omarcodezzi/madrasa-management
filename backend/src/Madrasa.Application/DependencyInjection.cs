using Madrasa.Application.Academic;
using Madrasa.Application.Accounts;
using Madrasa.Application.Admissions;
using Madrasa.Application.Hostel;
using Madrasa.Application.Library;
using Madrasa.Application.Notices;
using Madrasa.Application.Portal;
using Madrasa.Application.Reports;
using Madrasa.Application.Settings;
using Madrasa.Application.Staffing;
using Madrasa.Application.Attendance;
using Madrasa.Application.Exams;
using Madrasa.Application.Hifz;
using Madrasa.Application.Dashboard;
using Madrasa.Application.Fees;
using Madrasa.Application.Students;
using Microsoft.Extensions.DependencyInjection;

namespace Madrasa.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection s) => s
        .AddScoped<StudentService>()
        .AddScoped<AttendanceService>()
        .AddScoped<FeeService>()
        .AddScoped<DashboardService>()
        .AddScoped<AcademicService>()
        .AddScoped<HifzService>()
        .AddScoped<ExamService>()
        .AddScoped<AccountsService>()
        .AddScoped<StaffService>()
        .AddScoped<HostelService>()
        .AddScoped<LibraryService>()
        .AddScoped<NoticeService>()
        .AddScoped<ReportService>()
        .AddScoped<SettingsService>()
        .AddScoped<PortalService>()
        .AddScoped<AdmissionService>();
}
