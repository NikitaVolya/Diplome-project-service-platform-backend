using BLL.Services.Interfaces;
using DAL.UnitOfWork.Interfaces;
using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CategoryService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<Category>> GetMainCategoriesAsync()
        {
            return await _unitOfWork.Categories.GetMainCategoriesWithSubcategoriesAsync();
        }

        public async Task<IEnumerable<Category>> GetActiveCategoriesAsync()
        {
            return await _unitOfWork.Categories.GetActiveCategoriesAsync();
        }

        public async Task<Category?> GetByIdAsync(int id)
        {
            return await _unitOfWork.Categories.GetByIdAsync(id);
        }

        public async Task<Category?> GetWithSubcategoriesAsync(int id)
        {
            return await _unitOfWork.Categories.GetWithSubcategoriesByIdAsync(id);
        }

        private async Task ValidateParentCategoryAsync(
            int? parentCategoryId,
            int currentCategoryId)
        {
            var visited = new HashSet<int>();

            while (parentCategoryId.HasValue)
            {
                int id = parentCategoryId.Value;

                if (currentCategoryId == id)
                {
                    throw new ArgumentException(
                        "A category cannot be its own ancestor.");
                }

                if (!visited.Add(id))
                {
                    throw new InvalidOperationException(
                        "Circular category hierarchy detected.");
                }

                var parent = await _unitOfWork.Categories.GetByIdAsync(id);

                if (parent == null)
                {
                    throw new ArgumentException(
                        $"Parent category with id {id} does not exist.");
                }

                parentCategoryId = parent.ParentCategoryId;
            }
        }

        public async Task<Category> CreateCategoryAsync(Category category)
        {
            if (string.IsNullOrWhiteSpace(category.Name))
            {
                throw new ArgumentException(
                    "Category name cannot be empty.");
            }


            await ValidateParentCategoryAsync(category.ParentCategoryId, category.Id);

            await _unitOfWork.Categories.AddAsync(category);
            await _unitOfWork.SaveChangesAsync();

            return category;
        }

        public async Task UpdateCategoryAsync(Category category)
        {
            if (string.IsNullOrWhiteSpace(category.Name))
            {
                throw new ArgumentException("Category name cannot be empty.");
            }

            var existingCategory = await _unitOfWork.Categories.GetByIdAsync(category.Id);
            if (existingCategory == null)
            {
                throw new ArgumentException("Category does not exist.");
            }

            existingCategory.Name = category.Name;
            existingCategory.ParentCategoryId = category.ParentCategoryId;
            existingCategory.IsActive = category.IsActive;

            await _unitOfWork.Categories.UpdateAsync(existingCategory);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteCategoryAsync(int id)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(id);
            if (category == null)
            {
                throw new ArgumentException("Category does not exist.");
            }

            await _unitOfWork.Categories.DeleteAsync(id);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task ToggleActiveStatusAsync(int id)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(id);
            if (category == null)
            {
                throw new ArgumentException("Category does not exist.");
            }

            category.IsActive = !category.IsActive;
            await _unitOfWork.Categories.UpdateAsync(category);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}