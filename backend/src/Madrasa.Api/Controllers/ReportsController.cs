using Madrasa.Application.Auth;
using Madrasa.Application.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Madrasa.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Policy = Permissions.ReportsView)]
public class ReportsController(ReportService reports) : ControllerBase
{
    [HttpGet("{key}")]
    public Task<ReportTable> Run(string key, [FromQuery] ReportQuery q, CancellationToken ct) => reports.RunAsync(key, q, ct);
}
