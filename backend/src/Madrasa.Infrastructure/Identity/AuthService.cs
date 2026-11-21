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
    public async Task<LoginResponse?> LoginAsync(LoginRequest req, CancellationToken ct = default)
    {
        var user = await users.FindByNameAsync(req.UserName);
        if (user is null || !user.IsActive || !await users.CheckPasswordAsync(user, req.Password)) return null;
        var info = await BuildAsync(user);
        var o = jwt.Value;
        var expires = DateTime.UtcNow.AddMinutes(o.ExpiryMinutes);
        var claims = new List<Claim> { new(JwtRegisteredClaimNames.Sub, user.Id), new(ClaimTypes.Name, user.UserName!) };
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
