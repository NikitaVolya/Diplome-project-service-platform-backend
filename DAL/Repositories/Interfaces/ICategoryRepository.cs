using Domain.Models;

namespace DAL.Repositories.Interfaces
{
    public interface ICategoryRepository : IGenericRepository<Category>
    {
        Task<IEnumerable<Category>> GetMainCategoriesWithSubcategoriesAsync();

        Task<IEnumerable<Category>> GetActiveCategoriesAsync();

        Task<Category?> GetWithSubcategoriesByIdAsync(int id);
    }
}