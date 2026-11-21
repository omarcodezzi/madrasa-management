using Madrasa.Application.Academic;
using Madrasa.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Madrasa.Api.Controllers;

/// <summary>Classes, kitabs and routine. Anyone signed in can read; changing them needs settings permission.</summary>
[ApiController]
[Route("api/academic")]
[Authorize]
public class AcademicController(AcademicService academic) : ControllerBase
{
    [HttpGet("overview")]
    public Task<List<DepartmentOverview>> Overview(CancellationToken ct) => academic.OverviewAsync(ct);

    [HttpPost("classes"), Authorize(Policy = Permissions.SettingsManage)]
    public async Task<IActionResult> CreateClass(ClassInput input, CancellationToken ct) => Ok(new { id = await academic.SaveClassAsync(null, input, ct) });

    [HttpPut("classes/{id:int}"), Authorize(Policy = Permissions.SettingsManage)]
    public async Task<IActionResult> UpdateClass(int id, ClassInput input, CancellationToken ct) => Ok(new { id = await academic.SaveClassAsync(id, input, ct) });

    [HttpDelete("classes/{id:int}"), Authorize(Policy = Permissions.SettingsManage)]
    public async Task<IActionResult> DeleteClass(int id, CancellationToken ct) { await academic.DeleteClassAsync(id, ct); return NoContent(); }

    [HttpGet("subjects")]
    public Task<List<SubjectDto>> Subjects(CancellationToken ct) => academic.SubjectsAsync(ct);

    [HttpGet("teachers")]
    public Task<List<TeacherDto>> Teachers(CancellationToken ct) => academic.TeachersAsync(ct);

    [HttpGet("classes/{classId:int}/subjects")]
    public Task<List<ClassSubjectDto>> ClassSubjects(int classId, CancellationToken ct) => academic.ClassSubjectsAsync(classId, ct);

    [HttpPost("classes/{classId:int}/subjects"), Authorize(Policy = Permissions.SettingsManage)]
    public Task<ClassSubjectDto> AddSubject(int classId, ClassSubjectInput input, CancellationToken ct) => academic.SaveClassSubjectAsync(classId, null, input, ct);

    [HttpPut("classes/{classId:int}/subjects/{id:int}"), Authorize(Policy = Permissions.SettingsManage)]
    public Task<ClassSubjectDto> UpdateSubject(int classId, int id, ClassSubjectInput input, CancellationToken ct) => academic.SaveClassSubjectAsync(classId, id, input, ct);

    [HttpDelete("classes/{classId:int}/subjects/{id:int}"), Authorize(Policy = Permissions.SettingsManage)]
    public async Task<IActionResult> RemoveSubject(int classId, int id, CancellationToken ct) { await academic.RemoveClassSubjectAsync(classId, id, ct); return NoContent(); }

    [HttpGet("classes/{classId:int}/routine")]
    public Task<RoutineDto> Routine(int classId, CancellationToken ct) => academic.RoutineAsync(classId, ct);

    [HttpPut("classes/{classId:int}/routine"), Authorize(Policy = Permissions.SettingsManage)]
    public Task<RoutineDto> SaveRoutine(int classId, SaveRoutineRequest req, CancellationToken ct) => academic.SaveRoutineAsync(classId, req, ct);
}
