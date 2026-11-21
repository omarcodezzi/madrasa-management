using Madrasa.Application.Auth;
using Madrasa.Application.Exams;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Madrasa.Api.Controllers;

[ApiController]
[Route("api/exams")]
/// <summary>Policies are per action: reading one student's record only needs students.view.</summary>
[Authorize]
public class ExamsController(ExamService exams) : ControllerBase
{
    [HttpGet, Authorize(Policy = Permissions.ExamsMarks)]
    public Task<List<ExamDto>> List(CancellationToken ct) => exams.ListAsync(ct);

    [HttpPost, Authorize(Policy = Permissions.ExamsPublish)]
    public Task<ExamDto> Create(ExamInput input, CancellationToken ct) => exams.SaveAsync(null, input, ct);

    [HttpPut("{id:int}"), Authorize(Policy = Permissions.ExamsPublish)]
    public Task<ExamDto> Update(int id, ExamInput input, CancellationToken ct) => exams.SaveAsync(id, input, ct);

    [HttpDelete("{id:int}"), Authorize(Policy = Permissions.ExamsPublish)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct) { await exams.DeleteAsync(id, ct); return NoContent(); }

    [HttpGet("{id:int}/classes/{classId:int}/marks"), Authorize(Policy = Permissions.ExamsMarks)]
    public Task<MarkGrid> Grid(int id, int classId, CancellationToken ct) => exams.GridAsync(id, classId, ct);

    [HttpPut("{id:int}/marks"), Authorize(Policy = Permissions.ExamsMarks)]
    public async Task<IActionResult> SaveMarks(int id, SaveMarksRequest req, CancellationToken ct) => Ok(new { changed = await exams.SaveMarksAsync(id, req, ct) });

    [HttpGet("{id:int}/classes/{classId:int}/results"), Authorize(Policy = Permissions.ExamsMarks)]
    public Task<ResultSheet> Results(int id, int classId, CancellationToken ct) => exams.ResultsAsync(id, classId, ct);

    [HttpPost("{id:int}/publish"), Authorize(Policy = Permissions.ExamsPublish)]
    public Task<ExamDto> Publish(int id, [FromQuery] bool value = true, CancellationToken ct = default) => exams.PublishAsync(id, value, ct);

    [HttpGet("students/{studentId:int}"), Authorize(Policy = Permissions.StudentsView)]
    public Task<List<StudentExamResult>> StudentResults(int studentId, [FromQuery] int? examId, CancellationToken ct) => exams.StudentResultsAsync(studentId, examId, ct);
}
