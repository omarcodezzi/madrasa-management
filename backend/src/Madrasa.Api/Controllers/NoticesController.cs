using Madrasa.Application.Auth;
using Madrasa.Application.Notices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Madrasa.Api.Controllers;

/// <summary>Everyone signed in can read the board; writing needs notices.manage.</summary>
[ApiController]
[Route("api/notices")]
[Authorize]
public class NoticesController(NoticeService notices) : ControllerBase
{
    [HttpGet] public Task<List<NoticeDto>> List(CancellationToken ct) => notices.ListAsync(false, 100, ct);
    [HttpPost, Authorize(Policy = Permissions.NoticesManage)]
    public Task<NoticeSaved> Create(NoticeInput input, CancellationToken ct) => notices.SaveAsync(null, input, ct);
    [HttpPut("{id:int}"), Authorize(Policy = Permissions.NoticesManage)]
    public Task<NoticeSaved> Update(int id, NoticeInput input, CancellationToken ct) => notices.SaveAsync(id, input, ct);
    [HttpDelete("{id:int}"), Authorize(Policy = Permissions.NoticesManage)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct) { await notices.DeleteAsync(id, ct); return NoContent(); }
    [HttpGet("messages"), Authorize(Policy = Permissions.NoticesManage)]
    public Task<List<MessageRow>> Messages(CancellationToken ct) => notices.MessagesAsync(ct);

    [HttpGet("events")]
    public Task<List<EventDto>> Events([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct) => notices.EventsAsync(from, to, ct);
    [HttpPost("events"), Authorize(Policy = Permissions.NoticesManage)]
    public Task<EventDto> CreateEvent(EventInput input, CancellationToken ct) => notices.SaveEventAsync(null, input, ct);
    [HttpPut("events/{id:int}"), Authorize(Policy = Permissions.NoticesManage)]
    public Task<EventDto> UpdateEvent(int id, EventInput input, CancellationToken ct) => notices.SaveEventAsync(id, input, ct);
    [HttpDelete("events/{id:int}"), Authorize(Policy = Permissions.NoticesManage)]
    public async Task<IActionResult> DeleteEvent(int id, CancellationToken ct) { await notices.DeleteEventAsync(id, ct); return NoContent(); }
}
