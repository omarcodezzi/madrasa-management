using Madrasa.Application.Accounts;
using Madrasa.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Madrasa.Api.Controllers;

[ApiController]
[Route("api/accounts")]
[Authorize(Policy = Permissions.AccountsManage)]
public class AccountsController(AccountsService accounts) : ControllerBase
{
    [HttpGet("funds")] public Task<List<FundBalance>> Funds(CancellationToken ct) => accounts.FundsAsync(ct);
    [HttpGet("categories")] public Task<List<CategoryDto>> Categories(CancellationToken ct) => accounts.CategoriesAsync(ct);
    [HttpGet("ledger")] public Task<LedgerPage> Ledger([FromQuery] LedgerQuery q, CancellationToken ct) => accounts.LedgerAsync(q, ct);
    [HttpPost("entries")] public Task<LedgerRow> Add(EntryInput input, CancellationToken ct) => accounts.AddAsync(input, ct);
    [HttpDelete("entries/{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct) { await accounts.DeleteAsync(id, ct); return NoContent(); }
    [HttpGet("donors")] public Task<List<DonorDto>> Donors([FromQuery] string? q, CancellationToken ct) => accounts.DonorsAsync(q, ct);
    [HttpGet("summary")] public Task<AccountsSummary> Summary([FromQuery] string? month, CancellationToken ct) => accounts.SummaryAsync(month, ct);
}
