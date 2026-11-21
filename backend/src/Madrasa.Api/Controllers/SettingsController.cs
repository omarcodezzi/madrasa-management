using Madrasa.Application.Auth;
using Madrasa.Application.Common;
using Madrasa.Application.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Madrasa.Api.Controllers;

public record PasswordRequest(string Password);
public record RolePermsRequest(string[] Permissions);

[ApiController]
[Route("api/settings")]
[Authorize(Policy = Permissions.SettingsManage)]
public class SettingsController(SettingsService settings, IUserAdmin users, ICurrentUser me) : ControllerBase
{
    [HttpGet("profile")] public Task<ProfileDto> Profile(CancellationToken ct) => settings.ProfileAsync(ct);
    [HttpPut("profile")] public Task<ProfileDto> SaveProfile(ProfileInput input, CancellationToken ct) => settings.SaveProfileAsync(input, ct);

    [HttpGet("sessions")] public Task<List<SessionDto>> Sessions(CancellationToken ct) => settings.SessionsAsync(ct);
    [HttpPost("sessions")] public Task<SessionDto> CreateSession(SessionInput input, CancellationToken ct) => settings.SaveSessionAsync(null, input, ct);
    [HttpPut("sessions/{id:int}")] public Task<SessionDto> UpdateSession(int id, SessionInput input, CancellationToken ct) => settings.SaveSessionAsync(id, input, ct);
    [HttpPost("sessions/{id:int}/current")]
    public async Task<IActionResult> SetCurrent(int id, CancellationToken ct) { await settings.SetCurrentSessionAsync(id, ct); return NoContent(); }

    [HttpGet("fee-heads")] public Task<List<FeeHeadRow>> FeeHeads(CancellationToken ct) => settings.FeeHeadsAsync(ct);
    [HttpPost("fee-heads")] public Task<FeeHeadRow> CreateFeeHead(FeeHeadInput input, CancellationToken ct) => settings.SaveFeeHeadAsync(null, input, ct);
    [HttpPut("fee-heads/{id:int}")] public Task<FeeHeadRow> UpdateFeeHead(int id, FeeHeadInput input, CancellationToken ct) => settings.SaveFeeHeadAsync(id, input, ct);

    [HttpGet("grades")] public Task<List<GradeRow>> Grades(CancellationToken ct) => settings.GradesAsync(ct);
    [HttpPut("grades")] public Task<List<GradeRow>> SaveGrades(List<GradeInput> input, CancellationToken ct) => settings.SaveGradesAsync(input, ct);

    [HttpGet("holidays")] public Task<List<HolidayDto>> Holidays([FromQuery] int? year, CancellationToken ct) => settings.HolidaysAsync(year ?? DateTime.Today.Year, ct);
    [HttpPost("holidays")] public Task<HolidayDto> AddHoliday(HolidayInput input, CancellationToken ct) => settings.AddHolidayAsync(input, ct);
    [HttpDelete("holidays/{id:int}")]
    public async Task<IActionResult> DeleteHoliday(int id, CancellationToken ct) { await settings.DeleteHolidayAsync(id, ct); return NoContent(); }

    [HttpGet("general")] public Task<GeneralSettings> General(CancellationToken ct) => settings.GeneralAsync(ct);
    [HttpPut("general")] public Task<GeneralSettings> SaveGeneral(GeneralSettings input, CancellationToken ct) => settings.SaveGeneralAsync(input, ct);

    [HttpGet("users")] public Task<List<UserRow>> Users(CancellationToken ct) => users.UsersAsync(ct);
    [HttpPost("users")] public Task<UserRow> CreateUser(UserInput input, CancellationToken ct) => users.SaveUserAsync(null, input, me.UserId!, ct);
    [HttpPut("users/{id}")] public Task<UserRow> UpdateUser(string id, UserInput input, CancellationToken ct) => users.SaveUserAsync(id, input, me.UserId!, ct);
    [HttpPost("users/{id}/password")]
    public async Task<IActionResult> ResetPassword(string id, PasswordRequest req, CancellationToken ct) { await users.ResetPasswordAsync(id, req.Password, ct); return NoContent(); }

    [HttpGet("roles")] public Task<List<RoleRow>> Roles(CancellationToken ct) => users.RolesAsync(ct);
    [HttpPut("roles/{name}")] public Task<RoleRow> SetRole(string name, RolePermsRequest req, CancellationToken ct) => users.SetRolePermissionsAsync(name, req.Permissions, ct);
}
