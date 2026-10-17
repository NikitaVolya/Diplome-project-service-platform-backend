using Domain.Models;

namespace BLL.Services.Interfaces
{
    public interface ICategoryService
    {
        Task<IEnumerable<Category>> GetMainCategoriesAsync();
        Task<IEnumerable<Category>> GetActiveCategoriesAsync();
        Task<Category?> GetByIdAsync(int id);
        Task<Category?> GetWithSubcategoriesAsync(int id);

        Task<Category> CreateCategoryAsync(Category category);
        Task UpdateCategoryAsync(Category category);
        Task DeleteCategoryAsync(int id);
        Task ToggleActiveStatusAsync(int id);
    }
}