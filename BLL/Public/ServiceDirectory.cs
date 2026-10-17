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

    /// <summary>Шлях до світлини; порожній — картка покаже кружечок із літерою.</summary>
    public string Portrait { get; set; } = "";
}

/// <summary>Звідки набрані картки виконавців — щоб сторінка не видавала чужих за своїх.</summary>
public enum ExecutorScope
{
    /// <summary>Усі з обраної категорії.</summary>
    Own,

    /// <summary>Своїх не вистачило — добрали з інших категорій цього ж напряму.</summary>
    Direction,

    /// <summary>Не вистачило й у напрямі — добрали найкращих на платформі.</summary>
    Platform
}

public sealed class DirectoryData
{
    public List<DirectoryCategory> Categories { get; set; } = new();
    public List<DirectoryExecutor> Executors { get; set; } = new();
    public ExecutorScope ExecutorsScope { get; set; }
}

// Публічна статистика виконавців: лише завершені замовлення й активні категорії.
public sealed class ServiceDirectory(ApplicationDbContext db)
{
    /// <summary>Скільки карток видно одразу — рівно ряд макета.</summary>
    private const int Wanted = 4;

    /// <summary>Скільки віддаємо сторінці разом із тими, що ховаються під кнопкою.</summary>
    private const int Shown = 24;

    private sealed record Ranked(string Id, int Count, decimal Price);

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

        // Виконавців шукаємо трьома колами: спершу в самій категорії, потім у всьому
        // напрямі, а як і там порожньо — по всій платформі. Інакше сторінка напряму,
        // на який ще ніхто не замовляв, обривається одразу після плиток, а в макеті
        // блок є завжди. Звідки взялися картки, сторінка потім чесно підписує.
        async Task<List<Ranked>> RankAsync(HashSet<int>? ids, HashSet<string> skip)
        {
            var query = ids == null ? orders : orders.Where(o => ids.Contains(o.CategoryId));

            // Проєкція саме в анонімний тип: у власний record EF Core групування не перекладає.
            var rows = await query.GroupBy(o => o.ExecutorId)
                .Select(g => new { Id = g.Key, Count = g.Count(), Price = g.Min(o => o.Price) })
                .OrderByDescending(x => x.Count).ThenBy(x => x.Id).Take(Shown + skip.Count)
                .ToListAsync(cancellationToken);

            return rows.Where(r => r.Id != null && !skip.Contains(r.Id))
                .Select(r => new Ranked(r.Id!, r.Count, r.Price)).ToList();
        }

        var taken = new HashSet<string>(StringComparer.Ordinal);
        var ranked = await RankAsync(selected.HasValue ? Descendants(categories, selected.Value) : null, taken);
        var scope = ExecutorScope.Own;

        if (ranked.Count < Wanted && selected.HasValue)
        {
            taken.UnionWith(ranked.Select(r => r.Id));
            var root = RootOf(categories, selected.Value);
            var extra = await RankAsync(Descendants(categories, root), taken);
            if (extra.Count > 0)
            {
                ranked.AddRange(extra);
                scope = ExecutorScope.Direction;
            }
        }

        if (ranked.Count < Wanted)
        {
            taken.UnionWith(ranked.Select(r => r.Id));
            var extra = await RankAsync(null, taken);
            if (extra.Count > 0)
            {
                ranked.AddRange(extra);
                scope = ExecutorScope.Platform;
            }
        }

        ranked = ranked.Take(Shown).ToList();
        var userIds = ranked.Select(x => x.Id!).ToList();
        var users = await db.Users.AsNoTracking().Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FirstName, u.LastName }).ToListAsync(cancellationToken);
        var ratings = await db.Reviews.AsNoTracking().Where(r => userIds.Contains(r.TargetUserId))
            .GroupBy(r => r.TargetUserId).Select(g => new { Id = g.Key, Count = g.Count(), Rating = g.Average(r => (double)r.Rating) })
            .ToListAsync(cancellationToken);
        return new DirectoryData
        {
            Categories = categories,
            ExecutorsScope = ranked.Count == 0 ? ExecutorScope.Own : scope,
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

    /// <summary>Напрям, до якого належить категорія (для підкатегорії — її верхній предок).</summary>
    private static int RootOf(List<DirectoryCategory> categories, int id)
    {
        var current = categories.FirstOrDefault(c => c.Id == id);

        // Обмежуємо кроки: якщо в базі колись утвориться коло, сторінка не зависне.
        for (var step = 0; step < 16 && current?.ParentId != null; step++)
        {
            var parent = categories.FirstOrDefault(c => c.Id == current.ParentId.Value);
            if (parent == null) break;
            current = parent;
        }

        return current?.Id ?? id;
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
