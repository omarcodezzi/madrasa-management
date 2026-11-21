using Madrasa.Application.Auth;
using Madrasa.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Madrasa.Api.Controllers;

/// <summary>Read endpoints used by global search and the design-system demo. Full CRUD comes in Phase 4.</summary>
[ApiController]
[Route("api/students")]
[Authorize(Policy = Permissions.StudentsView)]
public class StudentsController(IAppDbContext db) : ControllerBase
{
    public record StudentRow(int Id, string StudentCode, string Name, string Gender, string? ClassName, int? Roll, string? Phone, string Status);
    public record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);

    /// <summary>Search by name, ID, roll, phone or guardian phone (UX rule 8.2).</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<StudentRow>>> List([FromQuery] string? q, [FromQuery] int? classId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 200);
        var sessionId = await db.AcademicSessions.Where(s => s.IsCurrent).Select(s => s.Id).FirstOrDefaultAsync(ct);
        var query = db.Students.AsNoTracking();
        if (classId is not null) query = query.Where(s => s.Enrollments.Any(e => e.SessionId == sessionId && e.ClassId == classId));
        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            int.TryParse(q, out var roll);
            query = query.Where(s => s.Name.Contains(q) || s.StudentCode.Contains(q) || (s.Phone != null && s.Phone.Contains(q))
                || s.Guardians.Any(g => g.Guardian!.Phone.Contains(q))
                || (roll > 0 && s.Enrollments.Any(e => e.SessionId == sessionId && e.Roll == roll)));
        }
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(s => s.StudentCode).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(s => new
            {
                s.Id, s.StudentCode, s.Name, s.Gender, s.Phone, s.Status,
                E = s.Enrollments.Where(e => e.SessionId == sessionId).Select(e => new { e.Class!.NameBn, e.Roll }).FirstOrDefault()
            }).ToListAsync(ct);
        return new PagedResult<StudentRow>(items.Select(s => new StudentRow(s.Id, s.StudentCode, s.Name, s.Gender.ToString(),
            s.E?.NameBn, s.E?.Roll, s.Phone, s.Status.ToString())).ToList(), total, page, pageSize);
    }
}
