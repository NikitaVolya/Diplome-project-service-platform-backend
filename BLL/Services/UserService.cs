using BLL.Services.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;


namespace BLL.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;


        public UserService(
            UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }


        public async Task<ApplicationUser?> GetByIdAsync(string id)
        {
            return await _userManager.FindByIdAsync(id);
        }


        public async Task<ApplicationUser?> GetByEmailAsync(string email)
        {
            return await _userManager.FindByEmailAsync(email);
        }


        public async Task<IEnumerable<ApplicationUser>> GetAllAsync()
        {
            return await _userManager.Users.ToListAsync();
        }


        public async Task<ApplicationUser> UpdateAsync(
            string id,
            string firstName,
            string lastName,
            string? avatarUrl)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
                throw new Exception("User not found");


            user.FirstName = firstName;
            user.LastName = lastName;
            user.AvatarUrl = avatarUrl;
            user.UpdatedAt = DateTime.UtcNow;


            await _userManager.UpdateAsync(user);

            return user;
        }


        public async Task<bool> DeleteAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
                return false;


            user.IsDeleted = true;

            await _userManager.UpdateAsync(user);

            return true;
        }
    }
}
