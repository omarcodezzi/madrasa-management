using Madrasa.Application.Common;
using Madrasa.Infrastructure.Identity;
using Madrasa.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Madrasa.Tests;

/// <summary>Admin password reset works without token providers and ends the user's other sessions.</summary>
public class PasswordResetTests
{
    private sealed class User : ICurrentUser { public string? UserId => null; }

    private static ServiceProvider Services()
    {
        var sc = new ServiceCollection();
        sc.AddLogging();
        sc.AddScoped<ICurrentUser, User>();
        var name = Guid.NewGuid().ToString();
        sc.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(name));
        sc.AddIdentityCore<AppUser>(o => { o.Password.RequiredLength = 8; o.Password.RequireNonAlphanumeric = false; })
            .AddRoles<IdentityRole>().AddEntityFrameworkStores<AppDbContext>();
        return sc.BuildServiceProvider();
    }

    [Fact]
    public async Task Sets_password_unlocks_and_renews_stamp()
    {
        using var sp = Services();
        using var scope = sp.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var u = new AppUser { UserName = "ustad", FullName = "উস্তাদ" };
        Assert.True((await users.CreateAsync(u, "Old12345a")).Succeeded);
        await users.SetLockoutEndDateAsync(u, DateTimeOffset.UtcNow.AddHours(1));
        var stamp = u.SecurityStamp;

        var weak = await users.SetPasswordAsync(u, "short");
        Assert.False(weak.Succeeded);
        Assert.True(await users.CheckPasswordAsync(u, "Old12345a"));   // a rejected password leaves the old one in place

        Assert.True((await users.SetPasswordAsync(u, "New12345b")).Succeeded);
        Assert.True(await users.CheckPasswordAsync(u, "New12345b"));
        Assert.False(await users.IsLockedOutAsync(u));
        Assert.NotEqual(stamp, u.SecurityStamp);
    }
}
