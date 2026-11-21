using Madrasa.Application.Attendance;
using Madrasa.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Madrasa.Api.Controllers;

[ApiController]
[Route("api/attendance")]
[Authorize(Policy = Permissions.AttendanceTake)]
public class AttendanceController(AttendanceService attendance) : ControllerBase
{
    [HttpGet("day")]
    public Task<AttendanceDay> Day([FromQuery] DateOnly? date, [FromQuery] int? classId, CancellationToken ct) => attendance.DayAsync(date, classId, ct);

    [HttpPost("mark")]
    public async Task<IActionResult> Mark(MarkRequest req, CancellationToken ct) => Ok(new { changed = await attendance.MarkAsync(req, ct) });

    /// <summary>ID card QR payload {"type":"RMTC_STUDENT","id":"..."} or a typed student code.</summary>
    [HttpPost("scan")]
    public Task<ScanResult> Scan(ScanRequest req, CancellationToken ct) => attendance.ScanAsync(req, ct);
}
