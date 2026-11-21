namespace Madrasa.Application.Settings;

public record UserRow(string Id, string UserName, string FullName, string[] Roles, bool IsActive, bool LockedOut, int? StaffId, int? StudentId, int? GuardianId, string? LinkedName);
public record UserInput(string UserName, string FullName, string? Password, string[] Roles, bool IsActive, int? StaffId, int? StudentId, int? GuardianId);
public record RoleRow(string Name, string[] Permissions, int Users, bool Locked);
public record UserLinks(int? StaffId, int? StudentId, int? GuardianId);

/// <summary>Identity-backed user and role administration (implemented in Infrastructure).</summary>
public interface IUserAdmin
{
    Task<List<UserRow>> UsersAsync(CancellationToken ct);
    Task<UserRow> SaveUserAsync(string? id, UserInput input, string currentUserId, CancellationToken ct);
    Task ResetPasswordAsync(string id, string password, CancellationToken ct);
    Task<List<RoleRow>> RolesAsync(CancellationToken ct);
    Task<RoleRow> SetRolePermissionsAsync(string role, string[] permissions, CancellationToken ct);
    Task<UserLinks?> LinksAsync(string userId, CancellationToken ct);
}
