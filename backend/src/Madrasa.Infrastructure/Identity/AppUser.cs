using Microsoft.AspNetCore.Identity;

namespace Madrasa.Infrastructure.Identity;

public class AppUser : IdentityUser
{
    public string FullName { get; set; } = "";
    public string? PhotoPath { get; set; }
    public string PreferredLanguage { get; set; } = "bn";
    public int? StaffId { get; set; }
    public int? GuardianId { get; set; }
    public int? StudentId { get; set; }
    public bool IsActive { get; set; } = true;
}
