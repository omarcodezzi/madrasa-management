using Microsoft.AspNetCore.Identity;

namespace Madrasa.Infrastructure.Identity;

public static class PasswordReset
{
    /// <summary>
    /// Sets a new password without a reset token (no token providers are registered). The password is validated
    /// first so a rejected one never leaves the account without a password. Also unlocks the account and
    /// renews its security stamp, which signs out every existing session.
    /// </summary>
    public static async Task<IdentityResult> SetPasswordAsync(this UserManager<AppUser> users, AppUser u, string password)
    {
        var errors = new List<IdentityError>();
        foreach (var v in users.PasswordValidators)
        {
            var r = await v.ValidateAsync(users, u, password);
            if (!r.Succeeded) errors.AddRange(r.Errors);
        }
        if (errors.Count > 0) return IdentityResult.Failed([.. errors]);
        if (await users.HasPasswordAsync(u))
        {
            var removed = await users.RemovePasswordAsync(u);
            if (!removed.Succeeded) return removed;
        }
        var added = await users.AddPasswordAsync(u, password);
        if (!added.Succeeded) return added;
        await users.SetLockoutEndDateAsync(u, null);
        await users.ResetAccessFailedCountAsync(u);
        return IdentityResult.Success;
    }
}
