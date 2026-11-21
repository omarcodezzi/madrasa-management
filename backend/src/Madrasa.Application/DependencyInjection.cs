using Madrasa.Application.Attendance;
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
        .AddScoped<DashboardService>();
}
