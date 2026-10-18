using Domain.Enums;
using Microsoft.AspNetCore.Identity;


namespace DAL.Seed
{

    public static class RolesSeader
    {
        public static async Task SeedRolesAsync(this RoleManager<IdentityRole> roleManager)
        {
            var roles = Enum.GetNames(typeof(UserRole));

            foreach (var role in Enum.GetNames<UserRole>())
            {
                if (await roleManager.RoleExistsAsync(role))
                    continue;

                var result = await roleManager.CreateAsync(
                    new IdentityRole
                    {
                        Name = role,
                        NormalizedName = role.ToUpperInvariant()
                    });

                if (!result.Succeeded)
                {
                    throw new Exception(
                        $"Failed to create role '{role}': " +
                        string.Join(
                            ", ",
                            result.Errors.Select(e => e.Description)));
                }
            }
        }
    }
}
