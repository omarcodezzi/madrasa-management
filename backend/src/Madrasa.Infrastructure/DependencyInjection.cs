using Madrasa.Application.Auth;
using Madrasa.Application.Common;
using Madrasa.Infrastructure.Identity;
using Madrasa.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Madrasa.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddDbContext<AppDbContext>(o => o.UseSqlServer(cfg.GetConnectionString("Default"),
            sql => sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddIdentityCore<AppUser>(o =>
            {
                o.Password.RequiredLength = 8;
                o.Password.RequireNonAlphanumeric = false;
                o.User.RequireUniqueEmail = false;
                o.Lockout.MaxFailedAccessAttempts = 5;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>();

        services.Configure<JwtOptions>(cfg.GetSection("Jwt"));
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }
}
