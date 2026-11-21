using Madrasa.Application.Auth;
using Madrasa.Application.Hostel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Madrasa.Api.Controllers;

public record ReturnRequest(DateOnly? On);

[ApiController]
[Route("api/hostel")]
[Authorize(Policy = Permissions.HostelManage)]
public class HostelController(HostelService hostel) : ControllerBase
{
    [HttpGet] public Task<HostelSummary> Summary(CancellationToken ct) => hostel.SummaryAsync(ct);
    [HttpPost("rooms")] public Task<RoomDto> CreateRoom(RoomInput input, CancellationToken ct) => hostel.SaveRoomAsync(null, input, ct);
    [HttpPut("rooms/{id:int}")] public Task<RoomDto> UpdateRoom(int id, RoomInput input, CancellationToken ct) => hostel.SaveRoomAsync(id, input, ct);
    [HttpDelete("rooms/{id:int}")]
    public async Task<IActionResult> DeleteRoom(int id, CancellationToken ct) { await hostel.DeleteRoomAsync(id, ct); return NoContent(); }
    [HttpGet("candidates")] public Task<List<Resident>> Candidates([FromQuery] string? q, CancellationToken ct) => hostel.CandidatesAsync(q, ct);
    [HttpGet("residents")] public Task<List<Resident>> Residents(CancellationToken ct) => hostel.ResidentsAsync(ct);
    [HttpPost("allocations")]
    public async Task<IActionResult> Allocate(AllocateInput input, CancellationToken ct) { await hostel.AllocateAsync(input, ct); return NoContent(); }
    [HttpDelete("allocations/{id:int}")]
    public async Task<IActionResult> Vacate(int id, CancellationToken ct) { await hostel.VacateAsync(id, ct); return NoContent(); }
    [HttpGet("leaves")] public Task<List<LeaveRow>> Leaves([FromQuery] bool open, CancellationToken ct) => hostel.LeavesAsync(open, ct);
    [HttpPost("leaves")]
    public async Task<IActionResult> AddLeave(LeaveInput input, CancellationToken ct) { await hostel.AddLeaveAsync(input, ct); return NoContent(); }
    [HttpPost("leaves/{id:int}/return")]
    public async Task<IActionResult> Return(int id, ReturnRequest req, CancellationToken ct) { await hostel.ReturnAsync(id, req.On, ct); return NoContent(); }
}
