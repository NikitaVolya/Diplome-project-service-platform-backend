using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace AdminPanel.Services;

public sealed class AppUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
{
    public const string FirstNameClaim = "first_name";

    public const string ShortNameClaim = "short_name";

    public const string AvatarClaim = "avatar";

    /// <summary>Є лише тоді, коли користувач обрав темну тему кабінету.</summary>
    public const string ThemeClaim = "theme";

    public AppUserClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<IdentityOptions> options)
        : base(userManager, roleManager, options)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        if (!string.IsNullOrWhiteSpace(user.FirstName))
        {
            identity.AddClaim(new Claim(FirstNameClaim, user.FirstName));

            var initial = string.IsNullOrWhiteSpace(user.LastName) ? string.Empty : $" {user.LastName[0]}.";
            identity.AddClaim(new Claim(ShortNameClaim, user.FirstName + initial));
        }

        if (!string.IsNullOrWhiteSpace(user.AvatarUrl))
        {
            identity.AddClaim(new Claim(AvatarClaim, user.AvatarUrl));
        }

        if (user.Theme == "dark")
        {
            identity.AddClaim(new Claim(ThemeClaim, "dark"));
        }

        return identity;
    }
}
