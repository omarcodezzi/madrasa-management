using System.Security.Claims;
using Madrasa.Application.Auth;
using Madrasa.Domain.Entities;
using Madrasa.Domain.Enums;
using Madrasa.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Madrasa.Infrastructure.Persistence;

public static class DbInitializer
{
    /// <summary>Applies migrations, seeds roles + permissions, the first admin, and (Development only) sample data.</summary>
    public static async Task InitializeAsync(IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var s = scope.ServiceProvider;
        var db = s.GetRequiredService<AppDbContext>();
        var env = s.GetRequiredService<IHostEnvironment>();
        var cfg = s.GetRequiredService<IConfiguration>();
        await db.Database.MigrateAsync();

        var roles = s.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var (name, perms) in Permissions.Defaults)
        {
            var role = await roles.FindByNameAsync(name);
            if (role is null) { role = new IdentityRole(name); await roles.CreateAsync(role); }
            var have = (await roles.GetClaimsAsync(role)).Where(c => c.Type == "perm").Select(c => c.Value).ToHashSet();
            foreach (var p in perms.Where(p => !have.Contains(p))) await roles.AddClaimAsync(role, new Claim("perm", p));
        }

        var users = s.GetRequiredService<UserManager<AppUser>>();
        var adminName = cfg["Seed:AdminUser"] ?? "admin";
        if (await users.FindByNameAsync(adminName) is null)
        {
            var pwd = cfg["Seed:AdminPassword"];
            if (string.IsNullOrWhiteSpace(pwd))
                throw new InvalidOperationException("Set Seed:AdminPassword (env Seed__AdminPassword) for the first admin account.");
            var admin = new AppUser { UserName = adminName, FullName = "সুপার অ্যাডমিন" };
            var r = await users.CreateAsync(admin, pwd);
            if (!r.Succeeded) throw new InvalidOperationException(string.Join("; ", r.Errors.Select(e => e.Description)));
            await users.AddToRoleAsync(admin, Roles.SuperAdmin);
        }

        if (env.IsDevelopment() && !await db.Students.AnyAsync()) await SeedSampleAsync(db);
    }

    private static async Task SeedSampleAsync(AppDbContext db)
    {
        var classes = new[]
        {
            new SchoolClass { DepartmentId = 1, NameBn = "নূরানী প্রথম", SortOrder = 1, MonthlyFee = 400 },
            new SchoolClass { DepartmentId = 2, NameBn = "নাজেরা", SortOrder = 2, MonthlyFee = 500 },
            new SchoolClass { DepartmentId = 3, NameBn = "হিফজ", SortOrder = 3, MonthlyFee = 800 },
            new SchoolClass { DepartmentId = 4, NameBn = "ইবতেদায়ী", SortOrder = 4, MonthlyFee = 500 },
            new SchoolClass { DepartmentId = 4, NameBn = "মিশকাত", SortOrder = 5, MonthlyFee = 600 },
            new SchoolClass { DepartmentId = 4, NameBn = "দাওরায়ে হাদীস", SortOrder = 6, MonthlyFee = 700 },
        };
        db.Classes.AddRange(classes);
        await db.SaveChangesAsync();

        string[] names = { "আব্দুল্লাহ আল মামুন", "মুহাম্মাদ হাসান", "উমর ফারুক", "তাহমিদ হোসেন", "আরিফ বিল্লাহ", "ইয়াসিন আরাফাত",
            "সাদিক রহমান", "মাহমুদুল হাসান", "আনাস ইবনে কাসেম", "জুবায়ের আহমদ", "ফাতিমা আক্তার", "আয়েশা সিদ্দিকা",
            "মারিয়াম খাতুন", "খাদিজা ইসলাম", "সুমাইয়া আক্তার", "হাফসা বিনতে রফিক", "নাফিসা তাবাসসুম", "রাকিব হাসান" };
        string[] fathers = { "আবুল কাশেম", "মোঃ রফিকুল ইসলাম", "আব্দুর রহিম", "মোঃ জসিম উদ্দিন", "নুরুল আমিন", "মাওলানা ইউসুফ" };
        for (int i = 0; i < names.Length; i++)
        {
            var st = new Student
            {
                StudentCode = $"RM-2026-{i + 1:000}", Name = names[i], Gender = i is >= 10 and < 17 ? Gender.Female : Gender.Male,
                FatherName = fathers[i % fathers.Length], Phone = $"01711-0000{10 + i}", PresentAddress = "রামারবাগ, লাকসাম, কুমিল্লা",
                AdmissionDate = new DateOnly(2026, 1, 1 + i % 8),
            };
            st.Enrollments.Add(new Enrollment { SessionId = 1, ClassId = classes[i % classes.Length].Id, Roll = i / classes.Length + 1 });
            db.Students.Add(st);
        }
        db.Notices.Add(new Notice { Title = "বার্ষিক পরীক্ষার সময়সূচি", Date = DateOnly.FromDateTime(DateTime.Today),
            Body = "আগামী ১৫ ডিসেম্বর থেকে বার্ষিক পরীক্ষা শুরু হবে। পরীক্ষার আগে সকল বকেয়া বেতন পরিশোধ করতে অনুরোধ করা হলো।" });
        await db.SaveChangesAsync();
    }
}
