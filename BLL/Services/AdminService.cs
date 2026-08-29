using BLL.Services.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Identity;


namespace BLL.Services
{
    public class AdminService : IAdminService
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminService(
            UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IEnumerable<ApplicationUser>> GetAllAsync()
        {
            var users = await _userManager.GetUsersInRoleAsync(
                nameof(UserRole.Admin));

            return users;
        }

        public async Task<ApplicationUser?> GetByIdAsync(
            string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user is null)
                return null;

            var isAdmin = await _userManager.IsInRoleAsync(user, nameof(UserRole.Admin));

            if (!isAdmin)
                return null;

            return user;
        }

        public async Task<ApplicationUser> AddAsync(
            string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user is null)
                throw new KeyNotFoundException($"User with ID '{userId}' was not found.");

            var isAdmin = await _userManager.IsInRoleAsync(user, nameof(UserRole.Admin));

            if (isAdmin)
                throw new InvalidOperationException("User is already an administrator.");

            var result = await _userManager.AddToRoleAsync(
                user,
                nameof(UserRole.Admin));

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(string.Join( "; ", result.Errors.Select(e => e.Description)));
            }

            return user;
        }

        public async Task<bool> RemoveAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user is null)
                return false;

            bool isAdmin = await _userManager.IsInRoleAsync(user, nameof(UserRole.Admin));

            if (!isAdmin)
                return false;

            bool isSuperAdmin = await _userManager.IsInRoleAsync(user, nameof(UserRole.SuperAdmin));
            if (isSuperAdmin)
                throw new InvalidOperationException("Cannot remove SuperAdmin role from a user.");

            var result = await _userManager.RemoveFromRoleAsync(user, nameof(UserRole.Admin));

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    string.Join(
                        "; ",
                        result.Errors.Select(e => e.Description)));
            }

            return true;
        }
    }
}
