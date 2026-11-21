using Madrasa.Application.Admissions;
using Madrasa.Application.Auth;
using Madrasa.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Madrasa.Api.Controllers;

/// <summary>Public website endpoints (no sign-in) and the office review of applications.</summary>
[ApiController]
[Route("api")]
public class AdmissionsController(AdmissionService admissions) : ControllerBase
{
    [HttpGet("public/info"), AllowAnonymous]
    public Task<PublicInfo> Info(CancellationToken ct) => admissions.PublicInfoAsync(ct);

    [HttpPost("public/apply"), AllowAnonymous, EnableRateLimiting("public")]
    public Task<ApplyResult> Apply(ApplyInput input, CancellationToken ct) => admissions.ApplyAsync(input, ct);

    [HttpGet("public/status"), AllowAnonymous, EnableRateLimiting("public")]
    public Task<ApplicationStatus> Status([FromQuery] string no, [FromQuery] string phone, CancellationToken ct) => admissions.StatusAsync(no, phone, ct);

    [HttpGet("admissions/applications"), Authorize(Policy = Permissions.AdmissionManage)]
    public Task<List<ApplicationRow>> List([FromQuery] AdmissionStage? stage, [FromQuery] string? q, CancellationToken ct) => admissions.ListAsync(stage, q, ct);

    [HttpGet("admissions/pipeline"), Authorize(Policy = Permissions.AdmissionManage)]
    public Task<List<PipelineCount>> Pipeline(CancellationToken ct) => admissions.PipelineAsync(ct);

    [HttpPost("admissions/applications"), Authorize(Policy = Permissions.AdmissionManage)]
    public Task<ApplicationRow> Create(ApplyInput input, CancellationToken ct) => admissions.CreateOfficeAsync(input, ct);

    [HttpPut("admissions/applications/{id:int}/stage"), Authorize(Policy = Permissions.AdmissionManage)]
    public Task<ApplicationRow> Stage(int id, StageInput input, CancellationToken ct) => admissions.SetStageAsync(id, input, ct);

    [HttpPut("admissions/applications/{id:int}/admitted"), Authorize(Policy = Permissions.AdmissionManage)]
    public Task<ApplicationRow> Admitted(int id, AdmittedInput input, CancellationToken ct) => admissions.MarkAdmittedAsync(id, input, ct);
}
