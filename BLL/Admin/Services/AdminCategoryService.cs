using BLL.Admin.Interfaces;
using BLL.Admin.Models;
using DAL.Context;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BLL.Admin.Services
{
    // Каталог категорій. Модель дозволяє будь-яку вкладеність, але панель свідомо показує лише два рівні (коренева + підкатегорія), бо саме так їх малює мобільний застосунок.
    public class AdminCategoryService : IAdminCategoryService
    {
        private readonly ApplicationDbContext _db;

        public AdminCategoryService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IReadOnlyList<AdminCategoryListItem>> GetTreeAsync(
            CategoryFilter filter,
            CancellationToken cancellationToken = default)
        {
            var query = _db.Categories.AsNoTracking();

            if (filter.IsActive.HasValue)
            {
                query = query.Where(c => c.IsActive == filter.IsActive.Value);
            }

            if (filter.HasSearch)
            {
                var term = filter.NormalizedSearch;
                query = query.Where(c => c.Name.Contains(term) || c.Description.Contains(term));
            }

            var flat = await query
                .Select(c => new AdminCategoryListItem
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    IconUrl = c.IconUrl,
                    IsActive = c.IsActive,
                    ParentCategoryId = c.ParentCategoryId,
                    ParentCategoryName = c.ParentCategory == null ? null : c.ParentCategory.Name,
                    SubCategoriesCount = c.SubCategories.Count,
                    OrdersCount = c.Orders.Count
                })
                .ToListAsync(cancellationToken);

            return Arrange(flat);
        }

        public async Task<AdminCategoryEditModel?> GetForEditAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _db.Categories
                .AsNoTracking()
                .Where(c => c.Id == id)
                .Select(c => new AdminCategoryEditModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    IconUrl = c.IconUrl,
                    ParentCategoryId = c.ParentCategoryId,
                    IsActive = c.IsActive
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<AdminCategoryListItem>> GetParentOptionsAsync(
            int? excludeId = null,
            CancellationToken cancellationToken = default)
        {
            // Батьківськими можуть бути лише кореневі категорії — це заразом виключає цикли без рекурсивної перевірки.
            var query = _db.Categories
                .AsNoTracking()
                .Where(c => c.ParentCategoryId == null);

            if (excludeId.HasValue)
            {
                query = query.Where(c => c.Id != excludeId.Value);
            }

            return await query
                .OrderBy(c => c.Name)
                .Select(c => new AdminCategoryListItem
                {
                    Id = c.Id,
                    Name = c.Name,
                    IsActive = c.IsActive
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<AdminOperationResult> CreateAsync(
            AdminCategoryEditModel model,
            CancellationToken cancellationToken = default)
        {
            var validation = await ValidateAsync(model, cancellationToken);
            if (validation != null)
            {
                return validation;
            }

            var category = new Category
            {
                Name = model.Name.Trim(),
                Description = (model.Description ?? string.Empty).Trim(),
                IconUrl = string.IsNullOrWhiteSpace(model.IconUrl) ? null : model.IconUrl.Trim(),
                ParentCategoryId = model.ParentCategoryId,
                IsActive = model.IsActive
            };

            _db.Categories.Add(category);
            await _db.SaveChangesAsync(cancellationToken);

            return AdminOperationResult.Ok($"Category \"{category.Name}\" created.");
        }

        public async Task<AdminOperationResult> UpdateAsync(
            AdminCategoryEditModel model,
            CancellationToken cancellationToken = default)
        {
            var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == model.Id, cancellationToken);
            if (category == null)
            {
                return AdminOperationResult.Fail("Category not found.");
            }

            if (model.ParentCategoryId == model.Id)
            {
                return AdminOperationResult.Fail("A category cannot be its own parent.");
            }

            var hasChildren = await _db.Categories.AnyAsync(c => c.ParentCategoryId == model.Id, cancellationToken);
            if (hasChildren && model.ParentCategoryId.HasValue)
            {
                return AdminOperationResult.Fail(
                    "This category has sub-categories, so it must stay at the top level.");
            }

            var validation = await ValidateAsync(model, cancellationToken);
            if (validation != null)
            {
                return validation;
            }

            category.Name = model.Name.Trim();
            category.Description = (model.Description ?? string.Empty).Trim();
            category.IconUrl = string.IsNullOrWhiteSpace(model.IconUrl) ? null : model.IconUrl.Trim();
            category.ParentCategoryId = model.ParentCategoryId;
            category.IsActive = model.IsActive;

            await _db.SaveChangesAsync(cancellationToken);
            return AdminOperationResult.Ok($"Category \"{category.Name}\" updated.");
        }

        public async Task<AdminOperationResult> ToggleActiveAsync(int id, CancellationToken cancellationToken = default)
        {
            var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
            if (category == null)
            {
                return AdminOperationResult.Fail("Category not found.");
            }

            category.IsActive = !category.IsActive;

            // Приховування батьківської категорії ховає і дочірні, інакше вони лишилися б видимими в застосунку.
            if (!category.IsActive)
            {
                var children = await _db.Categories
                    .Where(c => c.ParentCategoryId == id)
                    .ToListAsync(cancellationToken);

                foreach (var child in children)
                {
                    child.IsActive = false;
                }
            }

            await _db.SaveChangesAsync(cancellationToken);

            return AdminOperationResult.Ok(category.IsActive
                ? $"Category \"{category.Name}\" is now visible."
                : $"Category \"{category.Name}\" is now hidden.");
        }

        public async Task<AdminOperationResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
            if (category == null)
            {
                return AdminOperationResult.Fail("Category not found.");
            }

            var ordersCount = await _db.Orders.CountAsync(o => o.CategoryId == id, cancellationToken);
            if (ordersCount > 0)
            {
                return AdminOperationResult.Fail(
                    $"Category is used by {ordersCount} order(s). Hide it instead of deleting.");
            }

            var childrenCount = await _db.Categories.CountAsync(c => c.ParentCategoryId == id, cancellationToken);
            if (childrenCount > 0)
            {
                return AdminOperationResult.Fail(
                    $"Category has {childrenCount} sub-category(-ies). Delete or move them first.");
            }

            _db.Categories.Remove(category);
            await _db.SaveChangesAsync(cancellationToken);

            return AdminOperationResult.Ok($"Category \"{category.Name}\" deleted.");
        }

        // -----------------------------------------------------------------

        private async Task<AdminOperationResult?> ValidateAsync(
            AdminCategoryEditModel model,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                return AdminOperationResult.Fail("Name is required.");
            }

            var name = model.Name.Trim();

            var duplicate = await _db.Categories.AnyAsync(
                c => c.Id != model.Id && c.Name == name && c.ParentCategoryId == model.ParentCategoryId,
                cancellationToken);

            if (duplicate)
            {
                return AdminOperationResult.Fail($"A category named \"{name}\" already exists at this level.");
            }

            if (model.ParentCategoryId.HasValue)
            {
                var parent = await _db.Categories
                    .FirstOrDefaultAsync(c => c.Id == model.ParentCategoryId.Value, cancellationToken);

                if (parent == null)
                {
                    return AdminOperationResult.Fail("Selected parent category does not exist.");
                }

                if (parent.ParentCategoryId.HasValue)
                {
                    return AdminOperationResult.Fail("Only two levels of categories are supported.");
                }
            }

            return null;
        }

        /// <summary>Сортує кореневі категорії за абеткою і ставить кожну дочірню одразу після її батьківської.</summary>
        private static List<AdminCategoryListItem> Arrange(List<AdminCategoryListItem> flat)
        {
            var result = new List<AdminCategoryListItem>(flat.Count);

            var roots = flat
                .Where(c => c.ParentCategoryId == null)
                .OrderBy(c => c.Name)
                .ToList();

            foreach (var root in roots)
            {
                root.Depth = 0;
                result.Add(root);

                foreach (var child in flat.Where(c => c.ParentCategoryId == root.Id).OrderBy(c => c.Name))
                {
                    child.Depth = 1;
                    result.Add(child);
                }
            }

            // Підкатегорії, чия батьківська відсіялася фільтром, усе одно мають десь з'явитися.
            foreach (var orphan in flat.Except(result).OrderBy(c => c.Name))
            {
                orphan.Depth = 0;
                result.Add(orphan);
            }

            return result;
        }
    }
}
