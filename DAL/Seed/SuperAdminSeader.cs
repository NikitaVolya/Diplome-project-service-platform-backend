using DAL.Configurations;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;


namespace DAL.Seed
{
    public static class SuperAdminSeader
    {
        private static async Task AddRoleIfNoteExists(UserManager<ApplicationUser> userManager, ApplicationUser user, string role)
        {
            if (!await userManager.IsInRoleAsync(user, role))
            {
                var result = await userManager.AddToRoleAsync(user, role);
                if (!result.Succeeded)
                {
                    throw new Exception(
                        string.Join("; ", result.Errors.Select(e => e.Description)));
                }
            }
        }

        public static async Task SeedSuperAdminAsync(this UserManager<ApplicationUser> userManager, IOptions<AdminSeedOptions> options)
        {
            var settings = options.Value;

            var user = await userManager.FindByEmailAsync(settings.Email);

            if (user is null)
            {
                user = new ApplicationUser
                {
                    UserName = settings.Email,
                    Email = settings.Email,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(
                    user,
                    settings.Password);

                if (!result.Succeeded)
                {
                    throw new Exception(
                        string.Join("; ", result.Errors.Select(e => e.Description)));
                }
            }

            await AddRoleIfNoteExists(userManager, user, UserRole.Admin.ToRoleName());
            await AddRoleIfNoteExists(userManager, user, UserRole.SuperAdmin.ToRoleName());
        }
    }
}
