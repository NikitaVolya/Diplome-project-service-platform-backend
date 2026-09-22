using DAL.Context;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BLL.Public;

public sealed class DirectoryCategory
{
    public int Id { get; set; }
    public int? ParentId { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int Executors { get; set; }
    public int Artwork { get; set; }
    public string Icon { get; set; } = "";
}

public sealed class DirectoryExecutor
{
    public string Name { get; set; } = "";
    public int Completed { get; set; }
    public int Reviews { get; set; }
    public double Rating { get; set; }
    public decimal FromPrice { get; set; }
    public int Artwork { get; set; }
}

public sealed class DirectoryData
{
    public List<DirectoryCategory> Categories { get; set; } = new();
    public List<DirectoryExecutor> Executors { get; set; } = new();
}

// Публічна статистика виконавців: лише завершені замовлення й активні категорії.
public sealed class ServiceDirectory(ApplicationDbContext db)
{
    public async Task<DirectoryData> ReadAsync(int? categoryId, CancellationToken cancellationToken, string? city = null)
    {
        var categories = await db.Categories.AsNoTracking().Where(c => c.IsActive)
            .OrderBy(c => c.Id).Select(c => new DirectoryCategory
            {
                Id = c.Id, ParentId = c.ParentCategoryId, Name = c.Name,
                Description = c.Description ?? ""
            }).ToListAsync(cancellationToken);
        // Не публікуємо підкатегорії вимкнених батьків.
        var visible = categories.Where(c => c.ParentId == null).Select(c => c.Id).ToHashSet();
        for (var changed = true; changed;)
        {
            changed = false;
            foreach (var c in categories)
                if (c.ParentId.HasValue && visible.Contains(c.ParentId.Value)) changed |= visible.Add(c.Id);
        }
        categories = categories.Where(c => visible.Contains(c.Id)).ToList();
        var orders = db.Orders.AsNoTracking().Where(o => o.Status == OrderStatus.Completed
            && o.ExecutorId != null && o.Executor != null && !o.Executor.IsDeleted
            && visible.Contains(o.CategoryId));
        if (!string.IsNullOrWhiteSpace(city))
            orders = orders.Where(o => o.Address != null && o.Address.Contains(city));
        var pairs = await orders.Select(o => new { o.CategoryId, o.ExecutorId }).Distinct().ToListAsync(cancellationToken);
        foreach (var c in categories)
        {
            var ids = Descendants(categories, c.Id);
            c.Executors = pairs.Where(p => ids.Contains(p.CategoryId)).Select(p => p.ExecutorId).Distinct().Count();
        }
        var selected = categoryId.HasValue && visible.Contains(categoryId.Value)
            ? categoryId.Value : categories.FirstOrDefault(c => c.ParentId == null)?.Id;
        if (selected.HasValue)
        {
            var selectedIds = Descendants(categories, selected.Value);
            orders = orders.Where(o => selectedIds.Contains(o.CategoryId));
        }
        var ranked = await orders.GroupBy(o => o.ExecutorId)
            .Select(g => new { Id = g.Key, Count = g.Count(), Price = g.Min(o => o.Price) })
            .OrderByDescending(x => x.Count).ThenBy(x => x.Id).Take(24).ToListAsync(cancellationToken);
        var userIds = ranked.Select(x => x.Id!).ToList();
        var users = await db.Users.AsNoTracking().Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FirstName, u.LastName }).ToListAsync(cancellationToken);
        var ratings = await db.Reviews.AsNoTracking().Where(r => userIds.Contains(r.TargetUserId))
            .GroupBy(r => r.TargetUserId).Select(g => new { Id = g.Key, Count = g.Count(), Rating = g.Average(r => (double)r.Rating) })
            .ToListAsync(cancellationToken);
        return new DirectoryData
        {
            Categories = categories,
            Executors = ranked.Select(r =>
            {
                var user = users.First(u => u.Id == r.Id);
                var rating = ratings.FirstOrDefault(x => x.Id == r.Id);
                return new DirectoryExecutor
                {
                    Name = (string.IsNullOrWhiteSpace(user.FirstName) ? "Виконавець" : user.FirstName)
                        + (string.IsNullOrWhiteSpace(user.LastName) ? "" : " " + user.LastName[..1] + "."),
                    Completed = r.Count, FromPrice = r.Price, Reviews = rating?.Count ?? 0, Rating = rating?.Rating ?? 0
                };
            }).ToList()
        };
    }

    private static HashSet<int> Descendants(List<DirectoryCategory> categories, int id)
    {
        var ids = new HashSet<int> { id };
        for (var changed = true; changed;)
        {
            changed = false;
            foreach (var c in categories)
                if (c.ParentId.HasValue && ids.Contains(c.ParentId.Value)) changed |= ids.Add(c.Id);
        }
        return ids;
    }
}
