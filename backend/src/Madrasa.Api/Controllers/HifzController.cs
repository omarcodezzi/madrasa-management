using Madrasa.Application.Auth;
using Madrasa.Application.Hifz;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Madrasa.Api.Controllers;

[ApiController]
[Route("api/hifz")]
/// <summary>Policies are per action: reading one student's record only needs students.view.</summary>
[Authorize]
public class HifzController(HifzService hifz) : ControllerBase
{
    [HttpGet, Authorize(Policy = Permissions.HifzEdit)]
    public Task<HifzOverview> Overview([FromQuery] DateOnly? date, CancellationToken ct) => hifz.OverviewAsync(date, ct);

    [HttpGet("students/{id:int}"), Authorize(Policy = Permissions.StudentsView)]
    public Task<HifzStudentDetail> Student(int id, CancellationToken ct) => hifz.StudentAsync(id, ct);

    [HttpPost("students/{id:int}/logs"), Authorize(Policy = Permissions.HifzEdit)]
    public Task<HifzLogDto> SaveLog(int id, HifzLogInput input, CancellationToken ct) => hifz.SaveLogAsync(id, input, ct);

    [HttpDelete("students/{id:int}/logs/{logId:long}"), Authorize(Policy = Permissions.HifzEdit)]
    public async Task<IActionResult> DeleteLog(int id, long logId, CancellationToken ct) { await hifz.DeleteLogAsync(id, logId, ct); return NoContent(); }

    [HttpPut("students/{id:int}/juz"), Authorize(Policy = Permissions.HifzEdit)]
    public async Task<IActionResult> SetJuz(int id, SetJuzRequest req, CancellationToken ct) { await hifz.SetJuzAsync(id, req, ct); return NoContent(); }
}
