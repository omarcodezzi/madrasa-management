using Madrasa.Application.Common;
using Madrasa.Domain.Entities;
using Madrasa.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Madrasa.Application.Settings;

public record ProfileDto(string NameBn, string? NameEn, string? NameAr, string? Address, string? Phone, string? Email, string? LogoPath, int? EstablishedYear, string StudentIdPrefix);
public record ProfileInput(string NameBn, string? NameEn, string? NameAr, string? Address, string? Phone, string? Email, int? EstablishedYear, string StudentIdPrefix, string? Logo);
public record SessionDto(int Id, string Name, DateOnly StartDate, DateOnly EndDate, bool IsCurrent, int Students);
public record SessionInput(string Name, DateOnly StartDate, DateOnly EndDate);
public record FeeHeadRow(int Id, string NameBn, FeeFrequency Frequency, decimal DefaultAmount, bool IsActive, bool InUse);
public record FeeHeadInput(string NameBn, FeeFrequency Frequency, decimal DefaultAmount, bool IsActive);
public record GradeRow(int Id, string NameBn, string? NameAr, decimal MinAverage, int SortOrder);
public record GradeInput(string NameBn, string? NameAr, decimal MinAverage);
public record HolidayDto(int Id, DateOnly Date, string Title);
public record HolidayInput(DateOnly Date, string Title);
public record GeneralSettings(int WeeklyHoliday, decimal LibraryFinePerDay, bool OnlineAdmission);

/// <summary>Madrasa profile, sessions, fee heads, grade rules, holidays and app settings.</summary>
public class SettingsService(IAppDbContext db, IFileStorage files)
{
    public async Task<ProfileDto> ProfileAsync(CancellationToken ct)
    {
        var p = await db.MadrasaProfiles.AsNoTracking().FirstOrDefaultAsync(ct) ?? new MadrasaProfile();
        return new ProfileDto(p.NameBn, p.NameEn, p.NameAr, p.Address, p.Phone, p.Email, p.LogoPath, p.EstablishedYear, p.StudentIdPrefix);
    }

    public async Task<ProfileDto> SaveProfileAsync(ProfileInput input, CancellationToken ct)
    {
        var err = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(input.NameBn)) err["nameBn"] = "মাদরাসার নাম লিখুন";
        if (!string.IsNullOrWhiteSpace(input.Phone) && Digits.PhoneDigits(input.Phone).Length < 6) err["phone"] = "ফোন নম্বর ঠিক নেই";
        if (input.EstablishedYear is < 1800 or > 2100) err["establishedYear"] = "সাল ঠিক নেই";
        var prefix = (Digits.ToEnglish(input.StudentIdPrefix) ?? "").Trim().ToUpperInvariant();
        if (prefix.Length is < 1 or > 6 || !prefix.All(char.IsAsciiLetterOrDigit)) err["studentIdPrefix"] = "১ থেকে ৬টি ইংরেজি অক্ষর বা সংখ্যা";
        if (err.Count > 0) throw AppException.Invalid(err);
        var p = await db.MadrasaProfiles.FirstOrDefaultAsync(ct);
        if (p is null) { p = new MadrasaProfile(); db.MadrasaProfiles.Add(p); }
        p.NameBn = input.NameBn.Trim(); p.NameEn = Blank(input.NameEn); p.NameAr = Blank(input.NameAr); p.Address = Blank(input.Address);
        p.Phone = Blank(Digits.ToEnglish(input.Phone)); p.Email = Blank(input.Email); p.EstablishedYear = input.EstablishedYear; p.StudentIdPrefix = prefix;
        if (!string.IsNullOrEmpty(input.Logo)) p.LogoPath = await Photos.SaveAsync(files, "branding", "logo", input.Logo, ct);
        await db.SaveChangesAsync(ct);
        return await ProfileAsync(ct);
    }

    // ---- sessions ----
    public Task<List<SessionDto>> SessionsAsync(CancellationToken ct) =>
        db.AcademicSessions.AsNoTracking().OrderByDescending(s => s.StartDate)
            .Select(s => new SessionDto(s.Id, s.Name, s.StartDate, s.EndDate, s.IsCurrent, db.Enrollments.Count(e => e.SessionId == s.Id))).ToListAsync(ct);

    public async Task<SessionDto> SaveSessionAsync(int? id, SessionInput input, CancellationToken ct)
    {
        var err = new Dictionary<string, string>();
        var name = (Digits.ToEnglish(input.Name) ?? "").Trim();
        if (name.Length == 0) err["name"] = "শিক্ষাবর্ষের নাম লিখুন, যেমন 2026 বা 1447-48";
        if (input.EndDate <= input.StartDate) err["endDate"] = "শেষ তারিখ শুরুর পরে হতে হবে";
        if (name.Length > 0 && await db.AcademicSessions.AnyAsync(s => s.Id != id && s.Name == name, ct)) err["name"] = "এই নামে শিক্ষাবর্ষ আগেই আছে";
        if (err.Count > 0) throw AppException.Invalid(err);
        AcademicSession s;
        if (id is int sid) s = await db.AcademicSessions.FirstOrDefaultAsync(x => x.Id == sid, ct) ?? throw AppException.NotFound("শিক্ষাবর্ষ");
        else { s = new AcademicSession(); db.AcademicSessions.Add(s); }
        s.Name = name; s.StartDate = input.StartDate; s.EndDate = input.EndDate;
        await db.SaveChangesAsync(ct);
        return (await SessionsAsync(ct)).First(x => x.Id == s.Id);
    }

    /// <summary>Makes a session current. Students are not moved: promotion is a separate step.</summary>
    public async Task SetCurrentSessionAsync(int id, CancellationToken ct)
    {
        if (!await db.AcademicSessions.AnyAsync(s => s.Id == id, ct)) throw AppException.NotFound("শিক্ষাবর্ষ");
        foreach (var s in await db.AcademicSessions.ToListAsync(ct)) s.IsCurrent = s.Id == id;
        await db.SaveChangesAsync(ct);
    }

    // ---- fee heads ----
    public Task<List<FeeHeadRow>> FeeHeadsAsync(CancellationToken ct) =>
        db.FeeHeads.AsNoTracking().OrderBy(h => h.Id)
            .Select(h => new FeeHeadRow(h.Id, h.NameBn, h.Frequency, h.DefaultAmount, h.IsActive, db.FeeInvoices.Any(i => i.FeeHeadId == h.Id))).ToListAsync(ct);

    public async Task<FeeHeadRow> SaveFeeHeadAsync(int? id, FeeHeadInput input, CancellationToken ct)
    {
        var err = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(input.NameBn)) err["nameBn"] = "ফি-এর নাম লিখুন";
        if (!Enum.IsDefined(input.Frequency)) err["frequency"] = "কতবার নেওয়া হয়, বাছাই করুন";
        if (input.DefaultAmount is < 0 or > 1_000_000) err["defaultAmount"] = "পরিমাণ ঠিক নেই";
        FeeHead? h = null;
        if (id is int hid)
        {
            h = await db.FeeHeads.FirstOrDefaultAsync(x => x.Id == hid, ct) ?? throw AppException.NotFound("ফি খাত");
            if (h.Frequency != input.Frequency && await db.FeeInvoices.AnyAsync(i => i.FeeHeadId == hid, ct)) err["frequency"] = "এই খাতে আদায় হয়ে গেছে, ধরন বদলানো যাবে না";
            if (hid == 2 && !input.IsActive) err["isActive"] = "মাসিক বেতন খাত বন্ধ করা যাবে না";
        }
        if (err.Count > 0) throw AppException.Invalid(err);
        if (h is null) { h = new FeeHead(); db.FeeHeads.Add(h); }
        h.NameBn = input.NameBn.Trim(); h.Frequency = input.Frequency; h.DefaultAmount = input.DefaultAmount; h.IsActive = input.IsActive;
        await db.SaveChangesAsync(ct);
        return (await FeeHeadsAsync(ct)).First(x => x.Id == h.Id);
    }

    // ---- grade rules ----
    public Task<List<GradeRow>> GradesAsync(CancellationToken ct) =>
        db.GradeRules.AsNoTracking().OrderByDescending(g => g.MinAverage).Select(g => new GradeRow(g.Id, g.NameBn, g.NameAr, g.MinAverage, g.SortOrder)).ToListAsync(ct);

    /// <summary>Replaces the whole grade table so the bands can be checked together.</summary>
    public async Task<List<GradeRow>> SaveGradesAsync(IReadOnlyList<GradeInput> input, CancellationToken ct)
    {
        var err = new Dictionary<string, string>();
        if (input.Count is < 2 or > 10) err["grades"] = "২ থেকে ১০টি বিভাগ রাখুন";
        for (var i = 0; i < input.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(input[i].NameBn)) err[$"grades[{i}].nameBn"] = "নাম লিখুন";
            if (input[i].MinAverage is < 0 or > 100) err[$"grades[{i}].minAverage"] = "০ থেকে ১০০";
        }
        if (input.Select(g => g.MinAverage).Distinct().Count() != input.Count) err["grades"] = "দুটি বিভাগের সর্বনিম্ন নম্বর একই হতে পারে না";
        if (err.Count > 0) throw AppException.Invalid(err);
        var existing = await db.GradeRules.OrderByDescending(g => g.MinAverage).ToListAsync(ct);
        var ordered = input.OrderByDescending(g => g.MinAverage).ToList();
        for (var i = 0; i < ordered.Count; i++)
        {
            var g = i < existing.Count ? existing[i] : new GradeRule();
            if (i >= existing.Count) db.GradeRules.Add(g);
            g.NameBn = ordered[i].NameBn.Trim(); g.NameAr = Blank(ordered[i].NameAr); g.MinAverage = ordered[i].MinAverage; g.SortOrder = i + 1;
        }
        foreach (var extra in existing.Skip(ordered.Count))
        {
            foreach (var r in await db.ExamResults.Where(r => r.GradeRuleId == extra.Id).ToListAsync(ct)) r.GradeRuleId = null;
            db.GradeRules.Remove(extra);
        }
        await db.SaveChangesAsync(ct);
        return await GradesAsync(ct);
    }

    // ---- holidays ----
    public Task<List<HolidayDto>> HolidaysAsync(int year, CancellationToken ct) =>
        db.Holidays.AsNoTracking().Where(h => h.Date.Year == year).OrderBy(h => h.Date).Select(h => new HolidayDto(h.Id, h.Date, h.Title)).ToListAsync(ct);

    public async Task<HolidayDto> AddHolidayAsync(HolidayInput input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Title)) throw AppException.Invalid(new Dictionary<string, string> { ["title"] = "ছুটির নাম লিখুন" });
        if (await db.Holidays.AnyAsync(h => h.Date == input.Date, ct)) throw AppException.Invalid(new Dictionary<string, string> { ["date"] = "এই তারিখে আগেই ছুটি আছে" });
        var h = new Holiday { Date = input.Date, Title = input.Title.Trim() };
        db.Holidays.Add(h);
        await db.SaveChangesAsync(ct);
        return new HolidayDto(h.Id, h.Date, h.Title);
    }

    public async Task DeleteHolidayAsync(int id, CancellationToken ct)
    {
        var h = await db.Holidays.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw AppException.NotFound("ছুটি");
        db.Holidays.Remove(h);
        await db.SaveChangesAsync(ct);
    }

    // ---- general ----
    public async Task<GeneralSettings> GeneralAsync(CancellationToken ct)
    {
        var s = await db.AppSettings.AsNoTracking().ToDictionaryAsync(x => x.Key, x => x.Value, ct);
        return new GeneralSettings(int.TryParse(s.GetValueOrDefault("weeklyHoliday"), out var w) ? w : 5,
            decimal.TryParse(s.GetValueOrDefault("library.finePerDay"), out var f) ? f : 2,
            s.GetValueOrDefault("admission.online") != "false");
    }

    public async Task<GeneralSettings> SaveGeneralAsync(GeneralSettings input, CancellationToken ct)
    {
        var err = new Dictionary<string, string>();
        if (input.WeeklyHoliday is < 0 or > 6) err["weeklyHoliday"] = "সাপ্তাহিক ছুটির দিন বাছাই করুন";
        if (input.LibraryFinePerDay is < 0 or > 1000) err["libraryFinePerDay"] = "জরিমানা ০ থেকে ১০০০ টাকা";
        if (err.Count > 0) throw AppException.Invalid(err);
        await SetAsync("weeklyHoliday", input.WeeklyHoliday.ToString(), ct);
        await SetAsync("library.finePerDay", input.LibraryFinePerDay.ToString("0.##"), ct);
        await SetAsync("admission.online", input.OnlineAdmission ? "true" : "false", ct);
        await db.SaveChangesAsync(ct);
        return await GeneralAsync(ct);
    }

    private async Task SetAsync(string key, string value, CancellationToken ct)
    {
        var row = await db.AppSettings.FirstOrDefaultAsync(x => x.Key == key, ct);
        if (row is null) db.AppSettings.Add(new AppSetting { Key = key, Value = value }); else row.Value = value;
    }

    private static string? Blank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
