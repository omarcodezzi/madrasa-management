using Madrasa.Application.Auth;
using Madrasa.Application.Staffing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Madrasa.Api.Controllers;

public record StaffMarkRequest(DateOnly Date, IReadOnlyList<StaffMark> Marks);

[ApiController]
[Route("api/staff")]
[Authorize(Policy = Permissions.StaffManage)]
public class StaffController(StaffService staff) : ControllerBase
{
    [HttpGet] public Task<List<StaffRow>> List([FromQuery] string? q, [FromQuery] bool? active, CancellationToken ct) => staff.ListAsync(q, active, ct);
    [HttpGet("{id:int}")] public Task<StaffDetail> Get(int id, CancellationToken ct) => staff.GetAsync(id, ct);
    [HttpPost] public Task<StaffDetail> Create(StaffInput input, CancellationToken ct) => staff.SaveAsync(null, input, ct);
    [HttpPut("{id:int}")] public Task<StaffDetail> Update(int id, StaffInput input, CancellationToken ct) => staff.SaveAsync(id, input, ct);
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct) { await staff.DeleteAsync(id, ct); return NoContent(); }

    [HttpGet("attendance")] public Task<List<StaffDayRow>> Day([FromQuery] DateOnly? date, CancellationToken ct) => staff.DayAsync(date, ct);
    [HttpPost("attendance")]
    public async Task<IActionResult> Mark(StaffMarkRequest req, CancellationToken ct) => Ok(new { changed = await staff.MarkAsync(req.Date, req.Marks, ct) });

    [HttpGet("salary")] public Task<List<SalaryRow>> Salary([FromQuery] string? month, CancellationToken ct) => staff.SalarySheetAsync(month, ct);
    [HttpPost("salary")] public Task<SalaryRow> Pay(PayInput input, CancellationToken ct) => staff.PayAsync(input, ct);
    [HttpDelete("salary/{paymentId:int}")]
    public async Task<IActionResult> Cancel(int paymentId, CancellationToken ct) { await staff.CancelPaymentAsync(paymentId, ct); return NoContent(); }
}
