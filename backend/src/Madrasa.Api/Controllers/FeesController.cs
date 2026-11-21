using Madrasa.Application.Auth;
using Madrasa.Application.Common;
using Madrasa.Application.Fees;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Madrasa.Api.Controllers;

[ApiController]
[Route("api/fees")]
[Authorize(Policy = Permissions.FeesCollect)]
public class FeesController(FeeService fees) : ControllerBase
{
    [HttpGet("heads")]
    public Task<List<FeeHeadDto>> Heads(CancellationToken ct) => fees.HeadsAsync(ct);

    [HttpGet("students/{studentId:int}")]
    public Task<StudentDues> StudentDues(int studentId, CancellationToken ct) => fees.StudentDuesAsync(studentId, ct);

    [HttpPost("collect")]
    public Task<ReceiptDto> Collect(CollectRequest req, CancellationToken ct) => fees.CollectAsync(req, ct);

    [HttpGet("receipts")]
    public Task<PagedResult<ReceiptSummary>> Receipts([FromQuery] string? month, [FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int pageSize = 25,
        CancellationToken ct = default) => fees.ReceiptsAsync(month, q, page, pageSize, ct);

    [HttpGet("receipts/{receiptNo:int}")]
    public Task<ReceiptDto> Receipt(int receiptNo, CancellationToken ct) => fees.ReceiptAsync(receiptNo, ct);

    /// <summary>Voiding needs accounts permission: a counter clerk cannot cancel their own receipts.</summary>
    [HttpPost("receipts/{receiptNo:int}/void"), Authorize(Policy = Permissions.AccountsManage)]
    public Task<ReceiptDto> Void(int receiptNo, CancellationToken ct) => fees.VoidAsync(receiptNo, ct);

    [HttpGet("dues")]
    public Task<List<DueListRow>> Dues([FromQuery] string? month, [FromQuery] int? classId, CancellationToken ct) => fees.DueListAsync(month, classId, ct);

    [HttpGet("summary")]
    public Task<MonthSummary> Summary([FromQuery] string? month, CancellationToken ct) => fees.MonthSummaryAsync(month, ct);
}
