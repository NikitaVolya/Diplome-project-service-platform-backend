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

        Task<IReadOnlyList<PublicServiceItem>> GetServicesByIdsAsync(
            IReadOnlyList<int> ids,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<PublicServiceItem>> GetMostOrderedServicesAsync(
            int take = 6,
            CancellationToken cancellationToken = default);
    }
    public class PublicCatalogService : IPublicCatalogService
    {
        private static readonly string[] LandingCategories =
        {
            "Дім і ремонт", "Прибирання", "Дизайн і творчість", "Автопослуги",
            "IT та розробка", "Освіта і розвиток", "Краса та догляд",
            "Спорт та здоров’я", "Діти та догляд", "Послуги для тварин", "Фото та відео"
        };

        private static readonly (string Category, string Label, string Image)[] FeaturedServices =
        {
            ("Сантехнічні роботи", "Сантехнік", "/img/landing/service-1.jpg"),
            ("Прибирання квартир", "Прибирання квартири", "/img/landing/service-2.jpg"),
            ("Логотипи та стиль", "Дизайн логотипу", "/img/landing/service-3.jpg"),
            ("Електромонтажні роботи", "Електрик", "/img/landing/service-4.jpg")
        };

        private static readonly PublicServiceItem[] FallbackServices =
        {
            new() { Name = "Сантехнік", FromPrice = 600, Rating = 4.9, ReviewsCount = 32, Image = "/img/landing/service-1.jpg" },
            new() { Name = "Прибирання квартири", FromPrice = 700, Rating = 5.0, ReviewsCount = 18, Image = "/img/landing/service-2.jpg" },
            new() { Name = "Дизайн логотипу", FromPrice = 800, Rating = 4.8, ReviewsCount = 24, Image = "/img/landing/service-3.jpg" },
            new() { Name = "Електрик", FromPrice = 600, Rating = 4.9, ReviewsCount = 27, Image = "/img/landing/service-4.jpg" }
        };
        
        private static readonly (string Keyword, string Icon)[] IconRules =
        {
            ("техні", "cat-appliance"), ("побутов", "cat-appliance"), ("appliance", "cat-appliance"),
            ("вантаж", "cat-moving"), ("переїзд", "cat-moving"), ("перевез", "cat-moving"),
            ("moving", "cat-moving"), ("cargo", "cat-moving"), ("truck", "cat-moving"),
            ("достав", "cat-delivery"), ("кур", "cat-delivery"),
            ("delivery", "cat-delivery"), ("courier", "cat-delivery"),
            ("поді", "cat-events"), ("свят", "cat-events"), ("захід", "cat-events"), ("event", "cat-events"),
            ("бізнес", "cat-business"), ("business", "cat-business"),
            ("маркетинг", "cat-marketing"), ("реклам", "cat-marketing"),
            ("marketing", "cat-marketing"), ("ad", "cat-marketing"),
            ("інше", "cat-another"), ("інш", "cat-another"), ("other", "cat-another"),
            ("бізнес", "cat-business"), ("маркетинг", "cat-marketing"), ("реклам", "cat-marketing"),
            ("юрид", "cat-legal"), ("фінанс", "cat-legal"), ("бухгалт", "cat-legal"),
            ("legal", "cat-legal"), ("financ", "cat-legal"), ("account", "cat-legal"), ("law", "cat-legal"),

            ("ремонт", "cat-home"), ("дім", "cat-home"), ("буді", "cat-home"),
            ("home", "cat-home"), ("repair", "cat-home"), ("hous", "cat-home"),
            ("прибир", "cat-cleaning"), ("клінінг", "cat-cleaning"), ("clean", "cat-cleaning"),
            ("дизайн", "cat-design"), ("творч", "cat-design"), ("design", "cat-design"), ("art", "cat-design"),
            ("авто", "cat-auto"), ("транспорт", "cat-auto"),
            ("auto", "cat-auto"), ("car", "cat-auto"), ("transport", "cat-auto"),
            ("розробк", "cat-it"), ("технолог", "cat-it"), ("комп", "cat-it"),
            ("іт", "cat-it"), ("it", "cat-it"), ("comput", "cat-it"), ("develop", "cat-it"), ("software", "cat-it"),
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
            ("догляд", "cat-beauty"), ("care", "cat-beauty"),
            ("інше", "cat-another")
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
                categories = await _db.Categories
                    .AsNoTracking()
                    .Where(c => c.IsActive && c.ParentCategoryId == null)
                    .OrderBy(c => c.Id)
                    .Select(c => new PublicCategoryItem { Id = c.Id, Name = c.Name })
                    .ToListAsync(cancellationToken);

                categories = categories
                    .OrderBy(c => LandingOrder(c.Name))
                    .ThenBy(c => c.Id)
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
                categories = LandingCategories
                    .Select(name => new PublicCategoryItem { Name = name })
                    .ToList();
            }

            foreach (var category in categories)
            {
                category.Icon = GetCategoryIcon(category.Name);
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
            List<FeaturedCategory> found;

            var featured = FeaturedServices.Select(f => f.Category).ToList();

            try
            {
                found = await _db.Categories
                    .AsNoTracking()
                    .Where(c => c.IsActive && featured.Contains(c.Name))
                    .Select(c => new FeaturedCategory { CategoryId = c.Id, Name = c.Name })
                    .ToListAsync(cancellationToken);

                if (found.Count == 0)
                {
                    return FallbackServices;
                }

                var categoryIds = found.Select(c => c.CategoryId).ToList();

                stats = await _db.Orders
                    .AsNoTracking()
                    .Where(o => o.Status == OrderStatus.Completed && categoryIds.Contains(o.CategoryId))
                    .GroupBy(o => o.CategoryId)
                    .Select(g => new CategoryStats
                    {
                        CategoryId = g.Key,
                        OrdersCount = g.Count(),
                        MinPrice = g.Min(o => o.Price)
                    })
                    .ToListAsync(cancellationToken);

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

            var result = new List<PublicServiceItem>(FeaturedServices.Length);

            foreach (var card in FeaturedServices)
            {
                var category = found.FirstOrDefault(c => SameName(c.Name, card.Category));

                if (category == null)
                {
                    continue;
                }

                var row = stats.FirstOrDefault(s => s.CategoryId == category.CategoryId);
                var rating = ratings.FirstOrDefault(r => r.CategoryId == category.CategoryId);

                result.Add(new PublicServiceItem
                {
                    CategoryId = category.CategoryId,
                    Name = card.Label,
                    FromPrice = row == null ? 0 : Math.Round(row.MinPrice, 0),
                    Rating = rating == null ? 0 : Math.Round(rating.Average, 1),
                    ReviewsCount = rating?.Count ?? 0,
                    Image = card.Image
                });

                if (result.Count == take)
                {
                    break;
                }
            }

            return result.Count > 0 ? result : FallbackServices;
        }

        private class FeaturedCategory
        {
            public int CategoryId { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        private class CategoryStats
        {
            public int CategoryId { get; set; }
            public int OrdersCount { get; set; }
            public decimal MinPrice { get; set; }
        }

        private class CategoryRating
        {
            public int CategoryId { get; set; }
            public double Average { get; set; }
            public int Count { get; set; }
        }

        public static string GetCategoryIcon(string name)
        {
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

        public async Task<IReadOnlyList<PublicServiceItem>> GetServicesByIdsAsync(
            IReadOnlyList<int> ids,
            CancellationToken cancellationToken = default)
        {
            if (ids.Count == 0)
            {
                return Array.Empty<PublicServiceItem>();
            }

            List<PublicServiceItem> items;

            try
            {
                items = await _db.Categories
                    .AsNoTracking()
                    .Where(c => ids.Contains(c.Id) && c.IsActive && c.ParentCategoryId != null)
                    .Select(c => new PublicServiceItem
                    {
                        CategoryId = c.Id,
                        Name = c.Name,
                        Parent = c.ParentCategory!.Name,
                        FromPrice = c.Orders
                            .Where(o => o.Status == OrderStatus.Completed)
                            .Select(o => (decimal?)o.Price)
                            .Min() ?? 0
                    })
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Не вдалося прочитати нещодавно переглянуті послуги");
                return Array.Empty<PublicServiceItem>();
            }

            foreach (var item in items)
            {
                item.FromPrice = Math.Round(item.FromPrice, 0);
            }

            return ids
                .Select(id => items.FirstOrDefault(i => i.CategoryId == id))
                .Where(item => item != null)
                .Select(item => item!)
                .ToList();
        }

        public async Task<IReadOnlyList<PublicServiceItem>> GetMostOrderedServicesAsync(
            int take = 6,
            CancellationToken cancellationToken = default)
        {
            take = Math.Clamp(take, 1, 24);

            try
            {
                var popular = await _db.Orders
                    .AsNoTracking()
                    .Where(o => o.Category.IsActive && o.Category.ParentCategoryId != null)
                    .GroupBy(o => o.CategoryId)
                    .Select(g => new { CategoryId = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ThenBy(x => x.CategoryId)
                    .Take(take)
                    .ToListAsync(cancellationToken);

                if (popular.Count > 0)
                {
                    return await GetServicesByIdsAsync(popular.Select(p => p.CategoryId).ToList(), cancellationToken);
                }

                var first = await _db.Categories
                    .AsNoTracking()
                    .Where(c => c.IsActive && c.ParentCategoryId != null)
                    .OrderBy(c => c.Id)
                    .Select(c => c.Id)
                    .Take(take)
                    .ToListAsync(cancellationToken);

                return await GetServicesByIdsAsync(first, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Не вдалося прочитати найпопулярніші послуги");
                return Array.Empty<PublicServiceItem>();
            }
        }

        private static int LandingOrder(string name)
        {
            var index = Array.FindIndex(LandingCategories, known => SameName(known, name));
            return index < 0 ? int.MaxValue : index;
        }

        private static bool SameName(string left, string right) =>
            string.Equals(left.Replace('\u2019', '\''), right.Replace('\u2019', '\''),
                          StringComparison.OrdinalIgnoreCase);
    }
}
