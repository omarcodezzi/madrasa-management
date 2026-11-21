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

        if (env.IsDevelopment() && !await db.Students.AnyAsync()) await SeedSampleAsync(db, s);
    }

    private static async Task SeedSampleAsync(AppDbContext db, IServiceProvider sp)
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
        db.Notices.Add(new Notice { Title = "অভিভাবক সমাবেশ", Date = DateOnly.FromDateTime(DateTime.Today.AddDays(-12)),
            Body = "আগামী শুক্রবার বাদ জুমা মাদরাসা প্রাঙ্গণে অভিভাবক সমাবেশ অনুষ্ঠিত হবে।" });
        await db.SaveChangesAsync();

        // Guardians: the father, with the student's phone
        foreach (var st in await db.Students.ToListAsync())
            db.StudentGuardians.Add(new StudentGuardian { StudentId = st.Id, IsPrimary = true,
                Guardian = new Guardian { Name = st.FatherName ?? "অভিভাবক", Relation = "পিতা", Phone = st.Phone ?? "01711000000" } });
        await db.SaveChangesAsync();

        // Attendance for the last 20 school days (deterministic, about 88% present)
        var clock = sp.GetRequiredService<Madrasa.Application.Common.IClock>();
        var ids = await db.Students.Select(x => x.Id).ToListAsync();
        var rnd = new Random(11);
        var today = clock.Today;
        for (int k = 0; k <= 26; k++)
        {
            var d = today.AddDays(-k);
            if (d.DayOfWeek == DayOfWeek.Friday) continue;
            foreach (var id in ids)
            {
                var r = rnd.NextDouble();
                if (k == 0 && r > 0.72) continue;   // today's roll call is still in progress
                var status = r < 0.82 ? AttendanceStatus.Present : r < 0.88 ? AttendanceStatus.Late : r < 0.95 ? AttendanceStatus.Absent : AttendanceStatus.Leave;
                db.StudentAttendance.Add(new StudentAttendance { StudentId = id, Date = d, Status = status,
                    CheckIn = status is AttendanceStatus.Present or AttendanceStatus.Late ? new TimeOnly(7, 15 + rnd.Next(40)) : null });
            }
        }
        await db.SaveChangesAsync();

        // Fees: most students paid last month, about two thirds paid this month
        var fees = sp.GetRequiredService<Madrasa.Application.Fees.FeeService>();
        await fees.EnsureMonthlyInvoicesAsync(Madrasa.Application.Common.Months.Of(today), default);
        int n = 0;
        foreach (var id in ids)
        {
            var dues = await fees.StudentDuesAsync(id, default);
            var thisMonth = Madrasa.Application.Common.Months.Of(today);
            var pay = dues.Items.Where(x => (n % 9 != 4 && string.Compare(x.Month, thisMonth) < 0) || (x.Month == thisMonth && n % 3 != 1)).ToList();
            // One receipt per month, dated inside that month, like a real counter
            foreach (var g in pay.GroupBy(x => x.Month))
            {
                var first = Madrasa.Application.Common.Months.First(g.Key!);
                var date = first.AddDays(4 + n % 10);
                if (date > today) date = today;
                await fees.CollectAsync(new Madrasa.Application.Fees.CollectRequest(id, g.Select(x => new Madrasa.Application.Fees.CollectLine(x.InvoiceId, null, null, x.Due)).ToList(),
                    n % 4 == 0 ? PaymentMethod.BKash : PaymentMethod.Cash, n % 4 == 0 ? $"TRX{n:000}{first.Month:00}" : null, date), default);
            }
            n++;
        }
        // A few expenses so the income/expense chart has both sides
        var general = await db.Funds.Where(f => f.Code == "GENERAL").Select(f => f.Id).FirstAsync();
        for (int m = 0; m < 6; m++)
        {
            var d = new DateOnly(today.Year, today.Month, 1).AddMonths(-m).AddDays(4);
            if (d > today) continue;
            db.LedgerEntries.Add(new LedgerEntry { Date = d, FundId = general, CategoryId = 3, Direction = Direction.Out, Amount = 42000 + m * 500, Description = "শিক্ষক-কর্মচারী বেতন", CreatedAt = DateTime.UtcNow });
            db.LedgerEntries.Add(new LedgerEntry { Date = d.AddDays(6), FundId = general, CategoryId = 5, Direction = Direction.Out, Amount = 3500 + m * 120, Description = "বিদ্যুৎ বিল", CreatedAt = DateTime.UtcNow });
            db.LedgerEntries.Add(new LedgerEntry { Date = d.AddDays(2), FundId = general, CategoryId = 2, Direction = Direction.In, Amount = 25000 + m * 3000, Description = "সাধারণ দান", CreatedAt = DateTime.UtcNow });
        }
        await db.SaveChangesAsync();
    }
}
