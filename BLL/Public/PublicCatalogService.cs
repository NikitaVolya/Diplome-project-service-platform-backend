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

        /// <summary>
        /// Відповідність «слово в назві категорії» → намальована дизайнерами іконка.
        /// Файли лежать у AdminPanel/wwwroot/img/icons/ (SVG, тому не розмиваються на будь-якому екрані).
        /// Порядок рядків важливий: перемагає перший збіг, тому вужчі слова стоять вище за ширші.
        /// </summary>
        private static readonly (string Keyword, string Icon)[] IconRules =
        {
            // Спершу вужчі теми, інакше «Ремонт техніки» піймалося б на слові «ремонт»,
            // а «Доставка» — на транспорті.
            ("техні", "cat-appliance"), ("побутов", "cat-appliance"), ("appliance", "cat-appliance"),
            ("вантаж", "cat-moving"), ("переїзд", "cat-moving"), ("перевез", "cat-moving"),
            ("moving", "cat-moving"), ("cargo", "cat-moving"), ("truck", "cat-moving"),
            ("достав", "cat-delivery"), ("кур", "cat-delivery"),
            ("delivery", "cat-delivery"), ("courier", "cat-delivery"),
            ("поді", "cat-events"), ("свят", "cat-events"), ("захід", "cat-events"), ("event", "cat-events"),
            ("юрид", "cat-legal"), ("фінанс", "cat-legal"), ("бухгалт", "cat-legal"),
            ("legal", "cat-legal"), ("financ", "cat-legal"), ("account", "cat-legal"), ("law", "cat-legal"),

            ("ремонт", "cat-home"), ("дім", "cat-home"), ("буді", "cat-home"),
            ("home", "cat-home"), ("repair", "cat-home"), ("hous", "cat-home"),
            ("прибир", "cat-cleaning"), ("клінінг", "cat-cleaning"), ("clean", "cat-cleaning"),
            ("дизайн", "cat-design"), ("творч", "cat-design"), ("design", "cat-design"), ("art", "cat-design"),
            ("авто", "cat-auto"), ("транспорт", "cat-auto"),
            ("auto", "cat-auto"), ("car", "cat-auto"), ("transport", "cat-auto"),
            ("розробк", "cat-it"), ("технолог", "cat-it"), ("комп", "cat-it"),
            ("it", "cat-it"), ("comput", "cat-it"), ("develop", "cat-it"), ("software", "cat-it"),
            ("освіт", "cat-education"), ("репетит", "cat-education"), ("навчан", "cat-education"),
            ("переклад", "cat-education"), ("tutor", "cat-education"), ("lesson", "cat-education"),
            ("teach", "cat-education"), ("translat", "cat-education"),
            ("краса", "cat-beauty"), ("beauty", "cat-beauty"), ("hair", "cat-beauty"),
            ("здоров", "cat-health"), ("спорт", "cat-health"), ("медиц", "cat-health"),
            ("health", "cat-health"), ("sport", "cat-health"), ("fitness", "cat-health"), ("medic", "cat-health"),
            ("діти", "cat-kids"), ("дитяч", "cat-kids"), ("няня", "cat-kids"),
            ("kid", "cat-kids"), ("child", "cat-kids"), ("babysit", "cat-kids"), ("nann", "cat-kids"),
            ("тварин", "cat-pets"), ("pet", "cat-pets"), ("animal", "cat-pets"),
            ("фото", "cat-photo"), ("відео", "cat-photo"), ("photo", "cat-photo"), ("video", "cat-photo"),
            ("догляд", "cat-beauty"), ("care", "cat-beauty")
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
                // Порядок плиток — той самий, що в макеті: у порядку створення категорій.
                // Раніше вони сортувалися за кількістю замовлень, і ряд щоразу виглядав інакше.
                categories = await _db.Categories
                    .AsNoTracking()
                    .Where(c => c.IsActive && c.ParentCategoryId == null)
                    .OrderBy(c => c.Id)
                    .Select(c => new PublicCategoryItem { Id = c.Id, Name = c.Name })
                    .ToListAsync(cancellationToken);

                categories = categories
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

        /// <summary>
        /// Підбирає файл іконки за назвою категорії. Назви категорій задає адміністратор,
        /// тому шукаємо не точний збіг, а частину слова; якщо нічого не підійшло —
        /// показуємо нейтральні три крапки, як у макеті на плитці «Більше».
        /// </summary>
        private static string PickIcon(string name)
        {
            // Слово має саме починатися з ключа, а не просто містити його: інакше коротке «it»
            // спрацювало б у слові «fitness», а «car» — у «career».
            var words = name.ToLowerInvariant()
                .Split(new[] { ' ', '\t', '-', '/', '&', ',', '.', '(', ')', '«', '»', '\'', '"' },
                       StringSplitOptions.RemoveEmptyEntries);

            foreach (var (keyword, icon) in IconRules)
            {
                if (words.Any(word => word.StartsWith(keyword, StringComparison.Ordinal)))
                {
                    return IconPath(icon);
                }
            }

            return IconPath("cat-more");
        }

        private static string IconPath(string icon) => $"/img/icons/{icon}.svg";
    }
}
