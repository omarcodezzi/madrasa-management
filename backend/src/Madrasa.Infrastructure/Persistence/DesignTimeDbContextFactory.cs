using Madrasa.Application.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Madrasa.Infrastructure.Persistence;

/// <summary>Used only by `dotnet ef` to build migrations; no database connection is opened.</summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=localhost;Database=MadrasaDb;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        return new AppDbContext(options, new NoUser());
    }

    private sealed class NoUser : ICurrentUser { public string? UserId => null; }
}
