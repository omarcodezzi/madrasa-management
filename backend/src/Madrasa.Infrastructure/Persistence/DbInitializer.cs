using System.Security.Claims;
using Madrasa.Application.Auth;
using Madrasa.Application.Common;
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
        // Default permissions are granted once per (role, permission). Settings can then take one away and it stays away
        // across restarts; a permission added in a later release is still granted to its default roles.
        var seeded = await db.AppSettings.FirstOrDefaultAsync(x => x.Key == "perm.seeded");
        var done = (seeded?.Value ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        foreach (var (name, perms) in Permissions.Defaults)
        {
            var role = await roles.FindByNameAsync(name);
            if (role is null) { role = new IdentityRole(name); await roles.CreateAsync(role); }
            var have = (await roles.GetClaimsAsync(role)).Where(c => c.Type == "perm").Select(c => c.Value).ToHashSet();
            foreach (var p in perms.Where(p => !have.Contains(p) && (name == Roles.SuperAdmin || !done.Contains($"{name}:{p}"))))
                await roles.AddClaimAsync(role, new Claim("perm", p));
            foreach (var p in perms) done.Add($"{name}:{p}");
        }
        if (seeded is null) db.AppSettings.Add(new AppSetting { Key = "perm.seeded", Value = string.Join(',', done) });
        else seeded.Value = string.Join(',', done);
        await db.SaveChangesAsync();

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

        // Demo data in development only; Seed:Samples=false starts with an empty madrasa
        var samples = env.IsDevelopment() && !string.Equals(cfg["Seed:Samples"], "false", StringComparison.OrdinalIgnoreCase);
        if (samples && !await db.Students.AnyAsync()) await SeedSampleAsync(db, s);
        if (samples && !await db.Subjects.AnyAsync() && await db.Students.AnyAsync()) await SeedAcademicSampleAsync(db, s);
        if (samples && !await db.Books.AnyAsync() && await db.Staff.AnyAsync()) await SeedOfficeSampleAsync(db, s);
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
            db.LedgerEntries.Add(new LedgerEntry { Date = d.AddDays(6), FundId = general, CategoryId = 5, Direction = Direction.Out, Amount = 3500 + m * 120, Description = "বিদ্যুৎ বিল", CreatedAt = DateTime.UtcNow });
            db.LedgerEntries.Add(new LedgerEntry { Date = d.AddDays(2), FundId = general, CategoryId = 2, Direction = Direction.In, Amount = 25000 + m * 3000, Description = "সাধারণ দান", CreatedAt = DateTime.UtcNow });
        }
        await db.SaveChangesAsync();
    }

    /// <summary>Development sample for phase 5: ustads, kitabs per class, routine, hifz register and two published exams.</summary>
    private static async Task SeedAcademicSampleAsync(AppDbContext db, IServiceProvider sp)
    {
        var academic = sp.GetRequiredService<Madrasa.Application.Academic.AcademicService>();
        var exams = sp.GetRequiredService<Madrasa.Application.Exams.ExamService>();
        var hifz = sp.GetRequiredService<Madrasa.Application.Hifz.HifzService>();
        var clock = sp.GetRequiredService<Madrasa.Application.Common.IClock>();

        string[][] staff =
        {
            new[] { "মাওলানা আব্দুল হালিম", "মুহতামিম ও শাইখুল হাদীস" }, new[] { "মুফতি নুরুল ইসলাম", "মুহাদ্দিস" },
            new[] { "মাওলানা ইউসুফ আলী", "সিনিয়র উস্তাদ" }, new[] { "মাওলানা আনিসুর রহমান", "উস্তাদ" },
            new[] { "হাফেজ মাওলানা কামরুল হাসান", "হিফজ বিভাগীয় প্রধান" }, new[] { "ক্বারী আব্দুস সালাম", "নাজেরা উস্তাদ" },
            new[] { "মাওলানা জাকির হোসেন", "নূরানী উস্তাদ" },
        };
        for (int i = 0; i < staff.Length; i++)
            db.Staff.Add(new Staff { StaffCode = $"T-{i + 1:000}", Name = staff[i][0], Designation = staff[i][1], Phone = $"01811-00{i + 10}0{i}",
                JoinDate = new DateOnly(2015 + i, 1, 1), BasicSalary = 12000 + (staff.Length - i) * 1500, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        var t = await db.Staff.OrderBy(x => x.StaffCode).Select(x => x.Id).ToListAsync();

        var classes = await db.Classes.OrderBy(c => c.SortOrder).Select(c => new { c.Id, c.NameBn }).ToListAsync();
        // class name → (kitab, arabic, teacher index)
        var plan = new Dictionary<string, (string, string?, int)[]>
        {
            ["নূরানী প্রথম"] = new[] { ("নূরানী কায়দা", (string?)"القاعدة النورانية", 6), ("বাংলা", null, 6), ("গণিত", null, 6) },
            ["নাজেরা"] = new[] { ("কুরআন নাজেরা", (string?)"القرآن نظرا", 5), ("তাজবীদ", "التجويد", 5), ("মাসায়েল", "المسائل", 5) },
            ["হিফজ"] = new[] { ("হিফজুল কুরআন", (string?)"حفظ القرآن", 4), ("তাজবীদ", "التجويد", 4) },
            ["ইবতেদায়ী"] = new[] { ("আরবি সাহিত্য", (string?)"الأدب العربي", 3), ("নাহু-সরফ", "النحو والصرف", 3), ("বাংলা", null, 3), ("গণিত", null, 3) },
            ["মিশকাত"] = new[] { ("মিশকাতুল মাসাবীহ", (string?)"مشكاة المصابيح", 2), ("তাফসীরে জালালাইন", "تفسير الجلالين", 2), ("হিদায়া", "الهداية", 2), ("শরহে আকাইদ", "شرح العقائد", 2) },
            ["দাওরায়ে হাদীস"] = new[] { ("সহীহ বুখারী", (string?)"صحيح البخاري", 0), ("সহীহ মুসলিম", "صحيح مسلم", 1), ("জামে তিরমিযী", "جامع الترمذي", 0), ("সুনানে আবু দাউদ", "سنن أبي داود", 1) },
        };
        var periods = (await academic.PeriodsAsync(default)).ToList();
        foreach (var c in classes)
        {
            if (!plan.TryGetValue(c.NameBn, out var kitabs)) continue;
            var ids = new List<int>();
            foreach (var (name, ar, ti) in kitabs)
                ids.Add((await academic.SaveClassSubjectAsync(c.Id, null, new(null, name, ar, t[ti], 100, 33), default)).Id);
            var routine = await academic.RoutineAsync(c.Id, default);
            var slots = new List<Madrasa.Application.Academic.SlotInput>();
            foreach (var day in routine.Days)
                for (int p = 0; p < Math.Min(periods.Count, ids.Count + 2); p++)
                    slots.Add(new(day, periods[p].PeriodNo, ids[(p + day) % ids.Count], null));
            await academic.SaveRoutineAsync(c.Id, new(periods, slots), default);
        }

        // Hifz register: the last 6 weeks of sabak for each hifz student
        var today = clock.Today;
        var hifzStudents = await db.Enrollments.Where(e => e.Class!.Department!.Kind == DepartmentKind.Hifz).OrderBy(e => e.Roll).Select(e => e.StudentId).ToListAsync();
        var rnd = new Random(7);
        int[] done = { 12, 5, 20, 8 };
        for (int k = 0; k < hifzStudents.Count; k++)
        {
            var sid = hifzStudents[k];
            var completed = done[k % done.Length];
            for (byte j = 1; j <= completed; j++)
                db.HifzJuzProgress.Add(new HifzJuzProgress { StudentId = sid, JuzNo = j, Status = JuzStatus.Completed, CompletedOn = today.AddDays(-30 * (completed - j) - 50) });
            await db.SaveChangesAsync();
            var juz = (byte)(completed + 1);
            short page = 1;
            for (int back = 42; back >= 0; back--)
            {
                var d = today.AddDays(-back);
                if (d.DayOfWeek == DayOfWeek.Friday) continue;
                if (back == 0 && k == 1) continue;   // one student has not given today's sabak yet
                var n = (short)(rnd.Next(10) < 7 ? 1 : 2);
                if (page + n - 1 > Madrasa.Application.Hifz.HifzService.PagesPerJuz)
                {
                    await hifz.SetJuzAsync(sid, new((byte)juz, "Completed", d.AddDays(-1)), default);
                    juz++; page = 1;
                }
                var mistakes = rnd.Next(0, 6);
                await hifz.SaveLogAsync(sid, new(d, juz, page, (short)(page + n - 1), null,
                    Bn($"{(juz > 1 ? juz - 1 : 1)} পারার শেষ অর্ধেক"), Bn($"{rnd.Next(1, Math.Max(2, (int)juz))} পারা পুরো"), mistakes,
                    (byte)Math.Clamp(5 - mistakes / 2, 1, 5), mistakes > 3 ? "আরও মনোযোগ দিয়ে ইয়াদ করতে হবে" : null), default);
                page += n;
            }
        }

        // Exams: two finished and published, the annual one still ahead
        var first = await exams.SaveAsync(null, new("প্রথম সাময়িক পরীক্ষা", new DateOnly(today.Year, 4, 12)), default);
        var second = await exams.SaveAsync(null, new("দ্বিতীয় সাময়িক পরীক্ষা", new DateOnly(today.Year, 8, 16)), default);
        await exams.SaveAsync(null, new("বার্ষিক পরীক্ষা", new DateOnly(today.Year, 12, 15)), default);
        var mr = new Random(3);
        foreach (var ex in new[] { first, second })
        {
            foreach (var c in classes)
            {
                var grid = await exams.GridAsync(ex.Id, c.Id, default);
                var entries = new List<Madrasa.Application.Exams.MarkEntryInput>();
                foreach (var row in grid.Rows)
                {
                    var ability = 40 + (row.StudentId * 53 % 50);
                    foreach (var s in grid.Subjects)
                    {
                        var m = Math.Clamp(ability + mr.Next(-12, 13), 20, 99);
                        entries.Add(new(row.StudentId, s.Id, m, false));
                    }
                }
                await exams.SaveMarksAsync(ex.Id, new(c.Id, entries), default);
            }
            await exams.PublishAsync(ex.Id, true, default);
        }
    }

    /// <summary>Development sample for phase 6: donations and salaries, staff attendance, hostel, library, events, applications, portal users.</summary>
    private static async Task SeedOfficeSampleAsync(AppDbContext db, IServiceProvider sp)
    {
        var clock = sp.GetRequiredService<Madrasa.Application.Common.IClock>();
        var accounts = sp.GetRequiredService<Madrasa.Application.Accounts.AccountsService>();
        var staffSvc = sp.GetRequiredService<Madrasa.Application.Staffing.StaffService>();
        var hostel = sp.GetRequiredService<Madrasa.Application.Hostel.HostelService>();
        var library = sp.GetRequiredService<Madrasa.Application.Library.LibraryService>();
        var users = sp.GetRequiredService<UserManager<AppUser>>();
        var today = clock.Today;
        var funds = await db.Funds.ToDictionaryAsync(f => f.Code, f => f.Id);
        string[][] donors = [["হাজী আব্দুল মালেক", "01712345601", "লাকসাম বাজার"], ["আলহাজ্ব নুরুল ইসলাম", "01712345602", "কুমিল্লা শহর"],
            ["মোঃ সিরাজুল হক", "01712345603", "চাঁদপুর"], ["প্রবাসী ভাইদের পক্ষে", "01712345604", "দুবাই"]];

        // Donations and a few expenses for the last three months, then two months of salary from the general fund
        for (int m = 2; m >= 0; m--)
        {
            var first = new DateOnly(today.Year, today.Month, 1).AddMonths(-m);
            var day = (int k) => { var d = first.AddDays(k); return d > today ? today : d; };
            await accounts.AddAsync(new(day(1), funds["GENERAL"], 2, 150000 + m * 5000, "মাসিক সাধারণ দান", donors[0][0], donors[0][1], donors[0][2]), default);
            await accounts.AddAsync(new(day(3), funds["ZAKAT"], 2, 40000 + m * 2500, "যাকাতের টাকা (গরিব ছাত্রদের জন্য)", donors[1][0], donors[1][1], donors[1][2]), default);
            await accounts.AddAsync(new(day(5), funds["LILLAH"], 2, 18000, "লিল্লাহ বোর্ডিংয়ের জন্য", donors[3][0], donors[3][1], donors[3][2]), default);
            if (m < 2) await accounts.AddAsync(new(day(7), funds["SADAQAH"], 2, 7500, "সদকা", donors[2][0], donors[2][1], donors[2][2]), default);
            await accounts.AddAsync(new(day(8), funds["LILLAH"], 4, 14500 + m * 300, "বোর্ডিংয়ের চাল, ডাল ও তেল", null, null, null), default);
            await accounts.AddAsync(new(day(9), funds["ZAKAT"], 7, 12000, "গরিব ছাত্রদের বই ও পোশাক", null, null, null), default);
            if (m == 1) await accounts.AddAsync(new(day(12), funds["GENERAL"], 6, 23000, "ছাদের পানি নিরোধক মেরামত", null, null, null), default);
        }
        foreach (var monthsAgo in new[] { 2, 1 })
        {
            var month = Madrasa.Application.Common.Months.Of(today.AddMonths(-monthsAgo));
            var paidOn = new DateOnly(today.Year, today.Month, 1).AddMonths(-monthsAgo + 1).AddDays(1);
            if (paidOn > today) paidOn = today;
            foreach (var st in await db.Staff.Where(x => x.IsActive && x.BasicSalary != null).OrderBy(x => x.Id).ToListAsync())
                await staffSvc.PayAsync(new(st.Id, month, st.Id % 3 == 0 ? 1000 : 0, st.Id % 4 == 0 ? 500 : 0, funds["GENERAL"], paidOn), default);
        }

        // Staff attendance for the last two weeks
        var staffIds = await db.Staff.Where(x => x.IsActive).Select(x => x.Id).ToListAsync();
        foreach (var d in await db.WorkDaysAsync(today.AddDays(-14), today, default))
            foreach (var sid in staffIds)
            {
                var r = (sid * 7 + d.DayNumber) % 23;
                db.StaffAttendance.Add(new StaffAttendance { StaffId = sid, Date = d,
                    Status = r == 0 ? AttendanceStatus.Absent : r == 1 ? AttendanceStatus.Leave : r == 2 ? AttendanceStatus.Late : AttendanceStatus.Present,
                    CheckIn = r is 0 or 1 ? null : new TimeOnly(r == 2 ? 8 : 7, 30 + r % 25) });
            }
        await db.SaveChangesAsync();

        // Hostel: four rooms for boys, two for girls; seat the hifz students and a few more
        var rooms = new[] { ("ভবন ক", "101", 6, Gender.Male), ("ভবন ক", "102", 6, Gender.Male), ("ভবন ক", "103", 4, Gender.Male),
            ("ভবন খ", "201", 8, Gender.Male), ("ছাত্রী ভবন", "১", 6, Gender.Female), ("ছাত্রী ভবন", "২", 4, Gender.Female) };
        foreach (var (b, no, seats, g) in rooms) await hostel.SaveRoomAsync(null, new(b, no, seats, g), default);
        var roomIds = await db.HostelRooms.OrderBy(r => r.Id).Select(r => new { r.Id, r.ForGender }).ToListAsync();
        var students = await db.Students.Where(x => x.Status == StudentStatus.Active).OrderBy(x => x.Id).Select(x => new { x.Id, x.Gender }).ToListAsync();
        int bi = 0, gi = 0;
        foreach (var st in students.Where((_, i) => i % 3 != 2))
        {
            var pool = roomIds.Where(r => r.ForGender == st.Gender).ToList();
            var room = st.Gender == Gender.Male ? pool[bi++ % pool.Count] : pool[gi++ % pool.Count];
            await hostel.AllocateAsync(new(st.Id, room.Id, null, today.AddDays(-60)), default);
        }
        var seated = await db.HostelAllocations.Where(a => a.ToDate == null).OrderBy(a => a.Id).Select(a => a.StudentId).ToListAsync();
        if (seated.Count >= 3)
        {
            await hostel.AddLeaveAsync(new(seated[0], today.AddDays(-2), today.AddDays(2), "বাড়িতে বিয়ের অনুষ্ঠান", "পিতা"), default);
            await hostel.AddLeaveAsync(new(seated[1], today.AddDays(-6), today.AddDays(-1), "অসুস্থতা", "বড় ভাই"), default);
            db.HostelLeaves.Add(new HostelLeave { StudentId = seated[2], FromDate = today.AddDays(-20), ToDate = today.AddDays(-17), Reason = "ঈদের ছুটি", TakenBy = "পিতা", ReturnedOn = today.AddDays(-17) });
            await db.SaveChangesAsync();
        }

        // Library
        var books = new (string, string?, string?, BookLanguage, string, string, int)[]
        {
            ("সহীহ বুখারী (১ম খণ্ড)", "صحيح البخاري", "ইমাম বুখারী (রহ.)", BookLanguage.Arabic, "হাদীস", "A-01", 4),
            ("সহীহ মুসলিম", "صحيح مسلم", "ইমাম মুসলিম (রহ.)", BookLanguage.Arabic, "হাদীস", "A-02", 3),
            ("মিশকাতুল মাসাবীহ", "مشكاة المصابيح", "খতীব তাবরেযী (রহ.)", BookLanguage.Arabic, "হাদীস", "A-03", 5),
            ("হিদায়া (১ম খণ্ড)", "الهداية", "আল্লামা মারগীনানী (রহ.)", BookLanguage.Arabic, "ফিকহ", "B-01", 3),
            ("কুদুরী", "مختصر القدوري", "ইমাম কুদুরী (রহ.)", BookLanguage.Arabic, "ফিকহ", "B-02", 6),
            ("তাফসীরে জালালাইন", "تفسير الجلالين", "জালালুদ্দীন মহল্লী ও সুয়ূতী", BookLanguage.Arabic, "তাফসীর", "C-01", 4),
            ("নাহবেমীর", "نحو مير", "মীর সাইয়্যেদ শরীফ", BookLanguage.Urdu, "নাহু-সরফ", "D-01", 8),
            ("মীযানুস সরফ", "ميزان الصرف", null, BookLanguage.Urdu, "নাহু-সরফ", "D-02", 8),
            ("রিয়াদুস সালেহীন", "رياض الصالحين", "ইমাম নববী (রহ.)", BookLanguage.Bangla, "হাদীস", "E-01", 5),
            ("আর-রাহীকুল মাখতূম", "الرحيق المختوم", "সফিউর রহমান মুবারকপুরী", BookLanguage.Bangla, "সীরাত", "E-02", 3),
            ("ফাযায়েলে আমাল", null, "মাওলানা যাকারিয়া (রহ.)", BookLanguage.Bangla, "আখলাক", "E-03", 4),
            ("তাজবীদ শিক্ষা", null, "ক্বারী বেলায়েত হুসাইন", BookLanguage.Bangla, "তাজবীদ", "F-01", 10),
            ("আল-কিরাআতুর রাশিদা", "القراءة الراشدة", "আবুল হাসান আলী নদভী", BookLanguage.Arabic, "আরবি সাহিত্য", "G-01", 6),
            ("Elementary English Grammar", null, null, BookLanguage.English, "ইংরেজি", "H-01", 5),
        };
        foreach (var (t, ar, au, lang, cat, shelf, n) in books) await library.SaveBookAsync(null, new(t, ar, au, lang, cat, shelf, n), default);
        var bookIds = await db.Books.OrderBy(b => b.Id).Select(b => b.Id).ToListAsync();
        for (int i = 0; i < 8 && i < students.Count; i++)
            db.BookIssues.Add(new BookIssue { BookId = bookIds[i % bookIds.Count], StudentId = students[i].Id, IssuedOn = today.AddDays(-(i * 3 + 2)),
                DueOn = today.AddDays(-(i * 3 + 2) + 14), ReturnedOn = i == 7 ? today.AddDays(-1) : null, Fine = 0 });
        db.BookIssues.Add(new BookIssue { BookId = bookIds[0], StaffId = staffIds[0], IssuedOn = today.AddDays(-10), DueOn = today.AddDays(20) });
        await db.SaveChangesAsync();

        // Events
        var nextMonth = new DateOnly(today.Year, today.Month, 1).AddMonths(1);
        db.Events.AddRange(
            new SchoolEvent { Title = "বার্ষিক পরীক্ষা", StartDate = nextMonth.AddDays(4), EndDate = nextMonth.AddDays(14), Kind = EventKind.Exam },
            new SchoolEvent { Title = "অভিভাবক সমাবেশ", StartDate = today.AddDays(9), Kind = EventKind.Meeting },
            new SchoolEvent { Title = "শীতকালীন ছুটি", StartDate = nextMonth.AddDays(20), EndDate = nextMonth.AddDays(27), Kind = EventKind.Holiday },
            new SchoolEvent { Title = "হিফজ সমাপনী ও দস্তারবন্দী", StartDate = today.AddDays(3), Kind = EventKind.General });
        db.Notices.Add(new Notice { Title = "ভর্তি চলছে", Body = "নতুন শিক্ষাবর্ষে নূরানী, নাজেরা, হিফজ ও কিতাব বিভাগে ভর্তি চলছে। অনলাইনে আবেদন করা যাবে।", Date = today.AddDays(-3), Audience = NoticeAudience.All, IsPublic = true });
        await db.SaveChangesAsync();

        // Admission applications at every stage
        var cls = await db.Classes.OrderBy(c => c.SortOrder).Select(c => c.Id).ToListAsync();
        var apps = new (string, Gender, string, AdmissionStage, AdmissionSource, decimal?)[]
        {
            ("আবু বকর সিদ্দিক", Gender.Male, "মোঃ আব্দুল হাই", AdmissionStage.Applied, AdmissionSource.Online, null),
            ("রুকাইয়া জান্নাত", Gender.Female, "মোঃ শফিকুল ইসলাম", AdmissionStage.Applied, AdmissionSource.Online, null),
            ("ইব্রাহীম খলীল", Gender.Male, "মাওলানা আবু তাহের", AdmissionStage.Test, AdmissionSource.Office, null),
            ("উসমান গনি", Gender.Male, "মোঃ কামাল হোসেন", AdmissionStage.Approved, AdmissionSource.Online, 78),
            ("হামযা আল-আমিন", Gender.Male, "মোঃ আলমগীর", AdmissionStage.Rejected, AdmissionSource.Online, 31),
        };
        var sessionId = await db.AcademicSessions.Where(x => x.IsCurrent).Select(x => x.Id).FirstAsync();
        for (int i = 0; i < apps.Length; i++)
        {
            var (name, g, father, stage, src, marks) = apps[i];
            var a = new AdmissionApplication { SessionId = sessionId, ClassId = cls[i % cls.Count], ApplicantName = name, Gender = g, FatherName = father,
                GuardianPhone = $"0191200{i + 1:0000}", Address = "লাকসাম, কুমিল্লা", Source = src, Stage = stage, TestMarks = marks,
                DateOfBirth = today.AddYears(-(8 + i)), AppliedAt = DateTime.UtcNow.AddDays(-(10 - i * 2)), ApplicationNo = $"tmp-{i}",
                Remarks = stage == AdmissionStage.Rejected ? "ভর্তি পরীক্ষায় উত্তীর্ণ হয়নি" : null };
            db.AdmissionApplications.Add(a);
            await db.SaveChangesAsync();
            a.ApplicationNo = $"A-{today.Year}-{a.Id:0000}";
        }
        await db.SaveChangesAsync();

        // Portal sign-ins: the first student's guardian, and a student
        var firstSt = await db.StudentGuardians.Where(x => x.IsPrimary).OrderBy(x => x.StudentId).FirstAsync();
        if (await users.FindByNameAsync("guardian") is null)
        {
            var gu = new AppUser { UserName = "guardian", FullName = "অভিভাবক (নমুনা)", GuardianId = firstSt.GuardianId };
            await users.CreateAsync(gu, "Guardian123");
            await users.AddToRoleAsync(gu, Roles.Guardian);
        }
        var accountant = new AppUser { UserName = "hisab", FullName = "হিসাবরক্ষক (নমুনা)" };
        if (await users.FindByNameAsync("hisab") is null) { await users.CreateAsync(accountant, "Hisab12345"); await users.AddToRoleAsync(accountant, Roles.Accountant); }
    }

    private static string Bn(string s) => string.Concat(s.Select(c => c is >= '0' and <= '9' ? (char)('০' + (c - '0')) : c));
}
