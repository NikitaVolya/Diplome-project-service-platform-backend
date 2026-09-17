using DAL.Context;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BLL.Public
{
    public interface IPublicCatalogService
    {
        Task<IReadOnlyList<PublicCategoryItem>> GetPopularCategoriesAsync(
            int take = 11,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<PublicServiceItem>> GetPopularServicesAsync(
            int take = 4,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Дані для публічної головної сторінки. Читає ті самі таблиці, що й адмінпанель, але лише те,
    /// що можна показувати незареєстрованому відвідувачу.
    /// Якщо база порожня, повертає той самий набір, що й у макеті, — сторінка ніколи не виглядає зламаною.
    /// </summary>
    public class PublicCatalogService : IPublicCatalogService
    {
        private static readonly string[] FallbackCategories =
        {
            "Дім і ремонт", "Прибирання", "Дизайн та творчість", "Авто і транспорт",
            "IT та технології", "Освіта та репетиторство", "Краса та догляд",
            "Здоров'я та спорт", "Діти та догляд", "Тварини", "Фото та відео"
        };

        private static readonly PublicServiceItem[] FallbackServices =
        {
            new() { Name = "Сантехнік", FromPrice = 600, Rating = 4.9, ReviewsCount = 32, Image = "/img/landing/service-1.jpg" },
            new() { Name = "Прибирання квартири", FromPrice = 700, Rating = 5.0, ReviewsCount = 18, Image = "/img/landing/service-2.jpg" },
            new() { Name = "Дизайн логотипу", FromPrice = 800, Rating = 4.8, ReviewsCount = 24, Image = "/img/landing/service-3.jpg" },
            new() { Name = "Електрик", FromPrice = 600, Rating = 4.9, ReviewsCount = 27, Image = "/img/landing/service-4.jpg" }
        };

        /// <summary>Відповідність «слово в назві категорії» → іконка Bootstrap Icons.</summary>
        private static readonly (string Keyword, string Icon)[] IconRules =
        {
            ("ремонт", "bi-house-gear"), ("дім", "bi-house-gear"), ("home", "bi-house-gear"),
            ("прибир", "bi-stars"), ("clean", "bi-stars"),
            ("дизайн", "bi-palette"), ("творч", "bi-palette"), ("design", "bi-palette"),
            ("авто", "bi-car-front"), ("транспорт", "bi-truck"), ("delivery", "bi-truck"), ("достав", "bi-truck"),
            ("it", "bi-cpu"), ("технолог", "bi-cpu"), ("комп", "bi-pc-display"), ("computer", "bi-pc-display"),
            ("освіт", "bi-mortarboard"), ("репетит", "bi-mortarboard"), ("tutor", "bi-mortarboard"),
            ("краса", "bi-scissors"), ("догляд", "bi-heart"), ("beauty", "bi-scissors"),
            ("здоров", "bi-heart-pulse"), ("спорт", "bi-bicycle"),
            ("діти", "bi-balloon"), ("тварин", "bi-heart"),
            ("фото", "bi-camera"), ("відео", "bi-camera-reels"),
            ("поді", "bi-calendar-event"), ("event", "bi-calendar-event")
        };

        private readonly ApplicationDbContext _db;
        private readonly ILogger<PublicCatalogService> _logger;

        public PublicCatalogService(ApplicationDbContext db, ILogger<PublicCatalogService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<IReadOnlyList<PublicCategoryItem>> GetPopularCategoriesAsync(
            int take = 11,
            CancellationToken cancellationToken = default)
        {
            take = Math.Clamp(take, 4, 24);

            List<PublicCategoryItem> categories;

            try
            {
                // Кількість замовлень за кореневою категорією: замовлення зазвичай прив'язані
                // до підкатегорії, тому піднімаємо їх до батьківської.
                var counts = await _db.Orders
                    .AsNoTracking()
                    .GroupBy(o => o.Category.ParentCategoryId ?? o.CategoryId)
                    .Select(g => new { RootId = g.Key, Count = g.Count() })
                    .ToListAsync(cancellationToken);

                var roots = await _db.Categories
                    .AsNoTracking()
                    .Where(c => c.IsActive && c.ParentCategoryId == null)
                    .Select(c => new PublicCategoryItem { Id = c.Id, Name = c.Name })
                    .ToListAsync(cancellationToken);

                // Сортування робимо в пам'яті: категорій десятки, а запит лишається простим.
                categories = roots
                    .OrderByDescending(c => counts.FirstOrDefault(x => x.RootId == c.Id)?.Count ?? 0)
                    .ThenBy(c => c.Name)
                    .Take(take)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Не вдалося прочитати категорії для головної сторінки");
                categories = new List<PublicCategoryItem>();
            }

            if (categories.Count == 0)
            {
                categories = FallbackCategories
                    .Select(name => new PublicCategoryItem { Name = name })
                    .ToList();
            }

            foreach (var category in categories)
            {
                category.Icon = PickIcon(category.Name);
            }

            return categories;
        }

        public async Task<IReadOnlyList<PublicServiceItem>> GetPopularServicesAsync(
            int take = 4,
            CancellationToken cancellationToken = default)
        {
            take = Math.Clamp(take, 1, 12);

            List<CategoryStats> stats;
            List<CategoryRating> ratings;

            try
            {
                stats = await _db.Orders
                    .AsNoTracking()
                    .Where(o => o.Status == OrderStatus.Completed)
                    .GroupBy(o => new { o.CategoryId, o.Category.Name })
                    .Select(g => new CategoryStats
                    {
                        CategoryId = g.Key.CategoryId,
                        Name = g.Key.Name,
                        OrdersCount = g.Count(),
                        MinPrice = g.Min(o => o.Price)
                    })
                    .OrderByDescending(x => x.OrdersCount)
                    .Take(take)
                    .ToListAsync(cancellationToken);

                if (stats.Count == 0)
                {
                    return FallbackServices;
                }

                var categoryIds = stats.Select(s => s.CategoryId).ToList();

                ratings = await _db.Reviews
                    .AsNoTracking()
                    .Where(r => categoryIds.Contains(r.Order.CategoryId))
                    .GroupBy(r => r.Order.CategoryId)
                    .Select(g => new CategoryRating
                    {
                        CategoryId = g.Key,
                        Average = g.Average(r => (double)r.Rating),
                        Count = g.Count()
                    })
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Не вдалося прочитати популярні послуги для головної сторінки");
                return FallbackServices;
            }

            var result = new List<PublicServiceItem>(stats.Count);

            for (var i = 0; i < stats.Count; i++)
            {
                var row = stats[i];
                var rating = ratings.FirstOrDefault(r => r.CategoryId == row.CategoryId);

                result.Add(new PublicServiceItem
                {
                    CategoryId = row.CategoryId,
                    Name = row.Name,
                    FromPrice = Math.Round(row.MinPrice, 0),
                    Rating = rating == null ? 0 : Math.Round(rating.Average, 1),
                    ReviewsCount = rating?.Count ?? 0,
                    // Зображень у макеті чотири, тому вони чергуються по колу.
                    Image = $"/img/landing/service-{(i % 4) + 1}.jpg"
                });
            }

            return result;
        }

        private class CategoryStats
        {
            public int CategoryId { get; set; }
            public string Name { get; set; } = string.Empty;
            public int OrdersCount { get; set; }
            public decimal MinPrice { get; set; }
        }

        private class CategoryRating
        {
            public int CategoryId { get; set; }
            public double Average { get; set; }
            public int Count { get; set; }
        }

        private static string PickIcon(string name)
        {
            var lower = name.ToLowerInvariant();

            foreach (var (keyword, icon) in IconRules)
            {
                if (lower.Contains(keyword))
                {
                    return icon;
                }
            }

            return "bi-grid-1x2";
        }
    }
}
