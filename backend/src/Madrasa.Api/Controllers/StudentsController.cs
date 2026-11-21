using Madrasa.Application.Attendance;
using Madrasa.Application.Auth;
using Madrasa.Application.Common;
using Madrasa.Application.Fees;
using Madrasa.Application.Students;
using Madrasa.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Madrasa.Api.Controllers;

[ApiController]
[Route("api/students")]
[Authorize(Policy = Permissions.StudentsView)]
public class StudentsController(StudentService students, AttendanceService attendance, FeeService fees) : ControllerBase
{
    /// <summary>Search by name, ID, roll, phone or guardian phone (UX rule 8.2). sort: name | -name | admitted | -admitted | code (default class+roll)</summary>
    [HttpGet]
    public Task<PagedResult<StudentRow>> List([FromQuery] string? q, [FromQuery] int? classId, [FromQuery] Gender? gender, [FromQuery] StudentStatus? status,
        [FromQuery] string? sort, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default) =>
        students.ListAsync(new StudentListQuery(q, classId, gender, status, sort, page, pageSize), ct);

    [HttpGet("{id:int}")]
    public Task<StudentDetail> Get(int id, CancellationToken ct) => students.GetAsync(id, ct);

    [HttpGet("next-codes")]
    public Task<NextCodes> NextCodes([FromQuery] int? classId, CancellationToken ct) => students.NextCodesAsync(classId, ct);

    [HttpPost, Authorize(Policy = Permissions.StudentsEdit)]
    public async Task<ActionResult<StudentDetail>> Create(StudentInput input, CancellationToken ct)
    {
        var s = await students.CreateAsync(input, ct);
        return CreatedAtAction(nameof(Get), new { id = s.Id }, s);
    }

    [HttpPut("{id:int}"), Authorize(Policy = Permissions.StudentsEdit)]
    public Task<StudentDetail> Update(int id, StudentInput input, CancellationToken ct) => students.UpdateAsync(id, input, ct);

    [HttpDelete("{id:int}"), Authorize(Policy = Permissions.StudentsEdit)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct) { await students.DeleteAsync(id, ct); return NoContent(); }

    [HttpGet("{id:int}/attendance")]
    public Task<StudentMonth> Attendance(int id, [FromQuery] string? month, CancellationToken ct) => attendance.StudentMonthAsync(id, month, ct);

    [HttpGet("{id:int}/dues")]
    public Task<StudentDues> Dues(int id, CancellationToken ct) => fees.StudentDuesAsync(id, ct);
}
