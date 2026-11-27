using HrDashboard.Data;
using Microsoft.AspNetCore.Identity;

namespace HrDashboard.Security;

/// <summary>Explicit local development command; never exposed as an HTTP endpoint.</summary>
public sealed class DevelopmentAdminProvisioner(UserManager<ApplicationUser> users, RoleManager<IdentityRole> roles, IHostEnvironment environment)
{
    public async Task ProvisionAsync(string email, string password)
    {
        if (!environment.IsDevelopment()) throw new InvalidOperationException("Local administrator setup is available only in Development.");
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email)) throw new ArgumentException("Enter a valid email address.", nameof(email));
        var user = await users.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
            Require(await users.CreateAsync(user, password));
        }
        else
        {
            if (!await users.CheckPasswordAsync(user, password))
                throw new InvalidOperationException("The existing account password does not match. No access was granted.");
            if (!user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                Require(await users.UpdateAsync(user));
            }
        }
        if (!await roles.RoleExistsAsync("HRAdmin")) Require(await roles.CreateAsync(new IdentityRole("HRAdmin")));
        if (!await users.IsInRoleAsync(user, "HRAdmin")) Require(await users.AddToRoleAsync(user, "HRAdmin"));
    }

    private static void Require(IdentityResult result)
    {
        if (!result.Succeeded) throw new InvalidOperationException(string.Join(" ", result.Errors.Select(e => e.Description)));
    }
}
