using System.Security.Claims;
using Madrasa.Application.Auth;
using Madrasa.Application.Common;
using Madrasa.Application.Settings;
using Madrasa.Domain.Enums;
using Madrasa.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Madrasa.Infrastructure.Identity;

public class UserAdmin(UserManager<AppUser> users, RoleManager<IdentityRole> roles, AppDbContext db) : IUserAdmin
{
    public async Task<List<UserRow>> UsersAsync(CancellationToken ct)
    {
        var list = await users.Users.AsNoTracking().OrderBy(u => u.UserName).ToListAsync(ct);
        var userRoles = await db.UserRoles.Join(db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name }).ToListAsync(ct);
        var staff = await db.Staff.IgnoreQueryFilters().ToDictionaryAsync(s => s.Id, s => s.Name, ct);
        var studentIds = list.Where(u => u.StudentId != null).Select(u => u.StudentId!.Value).ToList();
        var students = await db.Students.IgnoreQueryFilters().Where(s => studentIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, s => s.Name, ct);
        var gIds = list.Where(u => u.GuardianId != null).Select(u => u.GuardianId!.Value).ToList();
        var guardians = await db.Guardians.Where(g => gIds.Contains(g.Id)).ToDictionaryAsync(g => g.Id, g => g.Name, ct);
        var now = DateTimeOffset.UtcNow;
        return list.Select(u => new UserRow(u.Id, u.UserName!, u.FullName, userRoles.Where(r => r.UserId == u.Id).Select(r => r.Name!).ToArray(), u.IsActive,
            u.LockoutEnd > now,
            u.StaffId, u.StudentId, u.GuardianId,
            u.StaffId is int s ? staff.GetValueOrDefault(s) : u.StudentId is int st ? students.GetValueOrDefault(st) : u.GuardianId is int g ? guardians.GetValueOrDefault(g) : null)).ToList();
    }

    public async Task<UserRow> SaveUserAsync(string? id, UserInput input, string currentUserId, CancellationToken ct)
    {
        var err = new Dictionary<string, string>();
        var userName = (Digits.ToEnglish(input.UserName) ?? "").Trim().ToLowerInvariant();
        if (userName.Length < 3 || !userName.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '_' or '-')) err["userName"] = "অন্তত ৩টি ইংরেজি অক্ষর বা সংখ্যা (. _ - চলবে)";
        if (string.IsNullOrWhiteSpace(input.FullName)) err["fullName"] = "পুরো নাম লিখুন";
        if (input.Roles is null || input.Roles.Length == 0) err["roles"] = "অন্তত একটি রোল দিন";
        else if (input.Roles.Any(r => !Permissions.Defaults.ContainsKey(r))) err["roles"] = "রোল ঠিক নেই";
        if (id is null && (input.Password ?? "").Length < 8) err["password"] = "পাসওয়ার্ড অন্তত ৮ অক্ষর";
        var links = new[] { input.StaffId, input.StudentId, input.GuardianId }.Count(x => x != null);
        if (links > 1) err["link"] = "একজন ইউজারকে একটিমাত্র ব্যক্তির সাথে যুক্ত করুন";
        var portal = input.Roles?.Any(r => r is Roles.Student or Roles.Guardian) == true;
        if (input.Roles?.Contains(Roles.Guardian) == true && input.GuardianId is null) err["link"] = "অভিভাবক ইউজারের জন্য অভিভাবক বাছাই করুন";
        if (input.Roles?.Contains(Roles.Student) == true && input.StudentId is null) err["link"] = "শিক্ষার্থী ইউজারের জন্য শিক্ষার্থী বাছাই করুন";
        if (portal && input.Roles!.Length > 1) err["roles"] = "শিক্ষার্থী/অভিভাবক রোলের সাথে অন্য রোল দেওয়া যাবে না";
        if (input.StaffId is int sid && !await db.Staff.AnyAsync(s => s.Id == sid, ct)) err["link"] = "স্টাফ পাওয়া যায়নি";
        if (input.StudentId is int stid && !await db.Students.AnyAsync(s => s.Id == stid, ct)) err["link"] = "শিক্ষার্থী পাওয়া যায়নি";
        if (input.GuardianId is int gid && !await db.Guardians.AnyAsync(g => g.Id == gid, ct)) err["link"] = "অভিভাবক পাওয়া যায়নি";
        var other = await users.FindByNameAsync(userName);
        if (other != null && other.Id != id) err["userName"] = "এই ইউজারনেম আগেই নেওয়া";

        AppUser? u = null;
        if (id != null)
        {
            u = await users.FindByIdAsync(id) ?? throw AppException.NotFound("ইউজার");
            var wasSuper = await users.IsInRoleAsync(u, Roles.SuperAdmin);
            var losesSuper = wasSuper && (!input.IsActive || input.Roles?.Contains(Roles.SuperAdmin) != true);
            if (losesSuper && (await users.GetUsersInRoleAsync(Roles.SuperAdmin)).Count(x => x.IsActive && x.Id != u.Id) == 0)
                err["roles"] = "অন্তত একজন সক্রিয় সুপার অ্যাডমিন থাকতে হবে";
            if (id == currentUserId && !input.IsActive) err["isActive"] = "নিজের অ্যাকাউন্ট বন্ধ করা যাবে না";
        }
        if (err.Count > 0) throw AppException.Invalid(err);

        if (u is null)
        {
            u = new AppUser { UserName = userName };
            Apply(u, input);
            var r = await users.CreateAsync(u, input.Password!);
            if (!r.Succeeded) throw AppException.Invalid(new Dictionary<string, string> { ["password"] = PasswordError(r) });
        }
        else
        {
            u.UserName = userName; Apply(u, input);
            var r = await users.UpdateAsync(u);
            if (!r.Succeeded) throw new AppException("ইউজার আপডেট করা যায়নি");
            if (!string.IsNullOrEmpty(input.Password)) await ResetPasswordAsync(u.Id, input.Password, ct);
        }
        var have = await users.GetRolesAsync(u);
        var rolesChanged = have.Except(input.Roles!).Any() || input.Roles!.Except(have).Any();
        await users.RemoveFromRolesAsync(u, have.Except(input.Roles!));
        await users.AddToRolesAsync(u, input.Roles!.Except(have));
        // Signed-in sessions carry roles and permissions, so a change (or deactivation) signs the user out everywhere
        if (id is not null && (rolesChanged || !input.IsActive)) await users.UpdateSecurityStampAsync(u);
        return (await UsersAsync(ct)).First(x => x.Id == u.Id);
    }

    private static void Apply(AppUser u, UserInput i)
    {
        u.FullName = i.FullName.Trim(); u.IsActive = i.IsActive; u.StaffId = i.StaffId; u.StudentId = i.StudentId; u.GuardianId = i.GuardianId;
        if (i.IsActive) u.LockoutEnd = null;
    }

    public async Task ResetPasswordAsync(string id, string password, CancellationToken ct)
    {
        var u = await users.FindByIdAsync(id) ?? throw AppException.NotFound("ইউজার");
        if ((password ?? "").Length < 8) throw AppException.Invalid(new Dictionary<string, string> { ["password"] = "পাসওয়ার্ড অন্তত ৮ অক্ষর" });
        var r = await users.SetPasswordAsync(u, password!);
        if (!r.Succeeded) throw AppException.Invalid(new Dictionary<string, string> { ["password"] = PasswordError(r) });
    }

    public async Task<List<RoleRow>> RolesAsync(CancellationToken ct)
    {
        var claims = await db.RoleClaims.Where(c => c.ClaimType == "perm").Join(db.Roles, c => c.RoleId, r => r.Id, (c, r) => new { r.Name, c.ClaimValue }).ToListAsync(ct);
        var counts = await db.UserRoles.Join(db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name).GroupBy(n => n).Select(g => new { g.Key, N = g.Count() }).ToListAsync(ct);
        return Permissions.Defaults.Keys.Select(n => new RoleRow(n,
            n == Roles.SuperAdmin ? Permissions.All : claims.Where(c => c.Name == n).Select(c => c.ClaimValue!).ToArray(),
            counts.FirstOrDefault(c => c.Key == n)?.N ?? 0, n == Roles.SuperAdmin)).ToList();
    }

    public async Task<RoleRow> SetRolePermissionsAsync(string roleName, string[] permissions, CancellationToken ct)
    {
        if (roleName == Roles.SuperAdmin) throw new AppException("সুপার অ্যাডমিনের অনুমতি বদলানো যায় না");
        var role = await roles.FindByNameAsync(roleName) ?? throw AppException.NotFound("রোল");
        if (permissions.Any(p => !Permissions.All.Contains(p))) throw AppException.Invalid(new Dictionary<string, string> { ["permissions"] = "অনুমতি ঠিক নেই" });
        var portal = roleName is Roles.Student or Roles.Guardian;
        if (portal && permissions.Any(p => p != Permissions.PortalView)) throw AppException.Invalid(new Dictionary<string, string> { ["permissions"] = "পোর্টাল রোল শুধু পোর্টাল দেখতে পারে" });
        var have = (await roles.GetClaimsAsync(role)).Where(c => c.Type == "perm").ToList();
        foreach (var c in have.Where(c => !permissions.Contains(c.Value))) await roles.RemoveClaimAsync(role, c);
        foreach (var p in permissions.Where(p => have.All(c => c.Value != p))) await roles.AddClaimAsync(role, new Claim("perm", p));
        // Members must sign in again to pick up the new permissions (a removed one must stop working now, not at token expiry)
        if (have.Count != permissions.Length || have.Any(c => !permissions.Contains(c.Value)))
            foreach (var m in await users.GetUsersInRoleAsync(roleName)) await users.UpdateSecurityStampAsync(m);
        return (await RolesAsync(ct)).First(r => r.Name == roleName);
    }

    public async Task<UserLinks?> LinksAsync(string userId, CancellationToken ct)
    {
        var u = await users.FindByIdAsync(userId);
        return u is null ? null : new UserLinks(u.StaffId, u.StudentId, u.GuardianId);
    }

    private static string PasswordError(IdentityResult r) =>
        r.Errors.Any(e => e.Code.StartsWith("Password")) ? "পাসওয়ার্ড অন্তত ৮ অক্ষর, বড় হাতের ও ছোট হাতের অক্ষর এবং সংখ্যা থাকতে হবে" : "পাসওয়ার্ড সেট করা যায়নি";
}
