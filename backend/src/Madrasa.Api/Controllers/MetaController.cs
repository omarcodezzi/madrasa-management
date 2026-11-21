using Madrasa.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Madrasa.Api.Controllers;

/// <summary>Lookups the app shell needs on load: madrasa profile, sessions, departments/classes, settings.</summary>
[ApiController]
[Route("api/meta")]
[Authorize]
public class MetaController(IAppDbContext db) : ControllerBase
{
    [HttpGet("profile"), AllowAnonymous]
    public async Task<IActionResult> Profile(CancellationToken ct) =>
        Ok(await db.MadrasaProfiles.AsNoTracking().FirstOrDefaultAsync(ct));

    [HttpGet("sessions")]
    public async Task<IActionResult> Sessions(CancellationToken ct) =>
        Ok(await db.AcademicSessions.AsNoTracking().OrderByDescending(x => x.StartDate).ToListAsync(ct));

    [HttpGet("classes")]
    public async Task<IActionResult> Classes(CancellationToken ct) =>
        Ok(await db.Departments.AsNoTracking().OrderBy(d => d.SortOrder)
            .Select(d => new { d.Id, d.NameBn, d.NameAr, d.Kind,
                Classes = d.Classes.OrderBy(c => c.SortOrder).Select(c => new { c.Id, c.NameBn, c.NameAr, c.MonthlyFee }) })
            .ToListAsync(ct));

    [HttpGet("settings")]
    public async Task<IActionResult> Settings(CancellationToken ct) =>
        Ok(await db.AppSettings.AsNoTracking().ToDictionaryAsync(x => x.Key, x => x.Value, ct));
}
