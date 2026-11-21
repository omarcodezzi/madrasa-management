using Madrasa.Application.Auth;
using Madrasa.Application.Library;
using Madrasa.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Madrasa.Api.Controllers;

[ApiController]
[Route("api/library")]
[Authorize(Policy = Permissions.LibraryManage)]
public class LibraryController(LibraryService library) : ControllerBase
{
    [HttpGet] public Task<LibrarySummary> Summary(CancellationToken ct) => library.SummaryAsync(ct);
    [HttpGet("books")]
    public Task<List<BookDto>> Books([FromQuery] string? q, [FromQuery] BookLanguage? language, [FromQuery] string? category, CancellationToken ct) =>
        library.BooksAsync(q, language, category, ct);
    [HttpPost("books")] public Task<BookDto> Create(BookInput input, CancellationToken ct) => library.SaveBookAsync(null, input, ct);
    [HttpPut("books/{id:int}")] public Task<BookDto> Update(int id, BookInput input, CancellationToken ct) => library.SaveBookAsync(id, input, ct);
    [HttpDelete("books/{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct) { await library.DeleteBookAsync(id, ct); return NoContent(); }
    [HttpGet("borrowers")] public Task<List<Borrower>> Borrowers([FromQuery] string? q, CancellationToken ct) => library.BorrowersAsync(q, ct);
    [HttpGet("issues")] public Task<List<IssueRow>> Issues([FromQuery] string? filter, CancellationToken ct) => library.IssuesAsync(filter ?? "out", ct);
    [HttpPost("issues")]
    public async Task<IActionResult> Issue(IssueInput input, CancellationToken ct) { await library.IssueAsync(input, ct); return NoContent(); }
    [HttpPost("issues/{id:int}/return")] public Task<IssueRow> Return(int id, ReturnInput input, CancellationToken ct) => library.ReturnAsync(id, input, ct);
}
