using Madrasa.Application.Auth;
using Madrasa.Application.Common;
using Madrasa.Application.Portal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Madrasa.Api.Controllers;

[ApiController]
[Route("api/portal")]
[Authorize(Policy = Permissions.PortalView)]
public class PortalController(PortalService portal, ICurrentUser me) : ControllerBase
{
    [HttpGet] public Task<PortalHome> Home(CancellationToken ct) => portal.HomeAsync(me.UserId!, ct);
    [HttpGet("students/{id:int}")]
    public Task<PortalStudent> Student(int id, [FromQuery] string? month, CancellationToken ct) => portal.StudentAsync(me.UserId!, id, month, ct);
}
