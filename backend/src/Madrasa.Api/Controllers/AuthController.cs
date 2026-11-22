using Madrasa.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Madrasa.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService auth) : ControllerBase
{
    [HttpPost("login"), AllowAnonymous, EnableRateLimiting("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest req, CancellationToken ct)
    {
        var res = await auth.LoginAsync(req, ct);
        return res is null ? Problem(title: "ইউজার নাম বা পাসওয়ার্ড সঠিক নয়", statusCode: 401) : Ok(res);
    }

    [HttpGet("me"), Authorize]
    public async Task<ActionResult<AuthUser>> Me(CancellationToken ct)
    {
        var id = User.FindFirst("sub")?.Value;
        var u = id is null ? null : await auth.GetUserAsync(id, ct);
        return u is null ? Unauthorized() : Ok(u);
    }

    [HttpPost("change-password"), Authorize]
    public async Task<ActionResult<LoginResponse>> ChangePassword(ChangePasswordRequest req, CancellationToken ct)
    {
        var id = User.FindFirst("sub")?.Value;
        if (id is null) return Unauthorized();
        var err = await auth.ChangePasswordAsync(id, req, ct);
        if (err is not null) return Problem(title: err, statusCode: 400);
        // The change ends every other session; this device gets a fresh token
        return Ok(await auth.IssueAsync(id, ct));
    }
}
