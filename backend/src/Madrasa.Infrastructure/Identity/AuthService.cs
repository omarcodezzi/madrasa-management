using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Madrasa.Application.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Madrasa.Infrastructure.Identity;

public class JwtOptions
{
    public string Issuer { get; set; } = "madrasa";
    public string Audience { get; set; } = "madrasa-web";
    public string Key { get; set; } = "";
    public int ExpiryMinutes { get; set; } = 480;
}

public class AuthService(UserManager<AppUser> users, RoleManager<IdentityRole> roles, IOptions<JwtOptions> jwt) : IAuthService
{
    public const string StampClaim = "stamp";

    public async Task<LoginResponse?> LoginAsync(LoginRequest req, CancellationToken ct = default)
    {
        var user = await users.FindByNameAsync(req.UserName);
        if (user is null || !user.IsActive || await users.IsLockedOutAsync(user)) return null;
        if (!await users.CheckPasswordAsync(user, req.Password))
        {
            await users.AccessFailedAsync(user);   // locks after 5 wrong tries (see DependencyInjection)
            return null;
        }
        await users.ResetAccessFailedCountAsync(user);
        return await TokenAsync(user);
    }

    public async Task<LoginResponse?> IssueAsync(string userId, CancellationToken ct = default)
    {
        var u = await users.FindByIdAsync(userId);
        return u is { IsActive: true } ? await TokenAsync(u) : null;
    }

    private async Task<LoginResponse> TokenAsync(AppUser user)
    {
        var info = await BuildAsync(user);
        var o = jwt.Value;
        var expires = DateTime.UtcNow.AddMinutes(o.ExpiryMinutes);
        // "stamp" ties the token to the account's security stamp: a password reset, deactivation or role change ends old sessions
        var claims = new List<Claim> { new(JwtRegisteredClaimNames.Sub, user.Id), new(ClaimTypes.Name, user.UserName!), new(StampClaim, user.SecurityStamp ?? "") };
        claims.AddRange(info.Roles.Select(r => new Claim(ClaimTypes.Role, r)));
        claims.AddRange(info.Permissions.Select(p => new Claim("perm", p)));
        var token = new JwtSecurityToken(o.Issuer, o.Audience, claims, expires: expires,
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(o.Key)), SecurityAlgorithms.HmacSha256));
        return new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), expires, info);
    }

    public async Task<AuthUser?> GetUserAsync(string userId, CancellationToken ct = default)
    {
        var u = await users.FindByIdAsync(userId);
        return u is null ? null : await BuildAsync(u);
    }

    public async Task<string?> ChangePasswordAsync(string userId, ChangePasswordRequest req, CancellationToken ct = default)
    {
        var u = await users.FindByIdAsync(userId);
        if (u is null) return "ব্যবহারকারী পাওয়া যায়নি";
        if (string.IsNullOrWhiteSpace(req.NewPassword) || req.NewPassword.Length < 8) return "নতুন পাসওয়ার্ড অন্তত ৮ অক্ষরের হতে হবে";
        if (req.NewPassword == req.CurrentPassword) return "নতুন পাসওয়ার্ড আগেরটির থেকে আলাদা হতে হবে";
        var r = await users.ChangePasswordAsync(u, req.CurrentPassword, req.NewPassword);
        if (r.Succeeded) return null;
        return r.Errors.Any(e => e.Code == "PasswordMismatch") ? "বর্তমান পাসওয়ার্ড সঠিক নয়" : "পাসওয়ার্ড পরিবর্তন করা যায়নি";
    }

    private async Task<AuthUser> BuildAsync(AppUser u)
    {
        var roleNames = (await users.GetRolesAsync(u)).ToArray();
        var perms = new HashSet<string>();
        foreach (var rn in roleNames)
        {
            var role = await roles.FindByNameAsync(rn);
            if (role is null) continue;
            foreach (var c in await roles.GetClaimsAsync(role)) if (c.Type == "perm") perms.Add(c.Value);
        }
        return new AuthUser(u.Id, u.UserName!, u.FullName, roleNames, perms.ToArray(), u.PreferredLanguage);
    }
}
