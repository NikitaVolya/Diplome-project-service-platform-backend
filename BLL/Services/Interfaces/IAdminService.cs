using Domain.Entities;


namespace BLL.Services.Interfaces
{
    public interface IAdminService
    {
        Task<IEnumerable<ApplicationUser>> GetAllAsync();

        Task<ApplicationUser?> GetByIdAsync(string userId);

        Task<ApplicationUser> AddAsync(string userId);

        Task<bool> RemoveAsync(string userId);
    }
}
