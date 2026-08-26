using Domain.Entities;


namespace BLL.Services.Interfaces
{
    public interface IUserService
    {
        Task<ApplicationUser?> GetByIdAsync(string id);
        Task<ApplicationUser?> GetByEmailAsync(string email);
        Task<ApplicationUser?> GetByUserNameAsync(string username);
        Task<IEnumerable<ApplicationUser>> GetAllAsync();
        Task<ApplicationUser> UpdateAsync(
            string id,
            string firstName,
            string lastName,
            string? avatarUrl);
        Task<bool> DeleteAsync(string id);
    }
}
