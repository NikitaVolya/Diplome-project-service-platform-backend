using System.Linq.Expressions;
using DAL.Context;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BLL.Public
{
    public enum OrderBoardSort
    {
        Newest,
        Oldest,
        PriceHigh,
        PriceLow,
        Deadline,
        FewBids
    }

    /// <summary>Фільтри сторінки «Знайти замовлення». Порожнє поле — фільтр не застосовується.</summary>
    public sealed class OrderBoardQuery
    {
        public string? Text { get; set; }

        public int? CategoryId { get; set; }

        public string? City { get; set; }

        /// <summary>Радіус у кілометрах від центру міста; null — «у межах міста».</summary>
        public int? Radius { get; set; }

        public bool OneTime { get; set; }

        public bool Regular { get; set; }

        public bool Urgent { get; set; }

        public decimal? PriceFrom { get; set; }

        public decimal? PriceTo { get; set; }

        /// <summary>«today», «3», «7», «30» (днів) або «none» — без терміну.</summary>
        public string? Due { get; set; }

        /// <summary>«onsite», «remote» або порожньо — будь-який формат.</summary>
        public string? Place { get; set; }

        public OrderBoardSort Sort { get; set; }

        public int Page { get; set; } = 1;

        public int PerPage { get; set; } = 5;
    }

    public sealed class OrderBoardItem
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string Parent { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? ExecutionAt { get; set; }

        public string Address { get; set; } = string.Empty;

        public int Bids { get; set; }

        public bool IsRemote { get; set; }

        public bool IsUrgent { get; set; }

        public bool IsRegular { get; set; }

        public bool IsMine { get; set; }

        public bool HasMyBid { get; set; }

        public bool IsFavorite { get; set; }

        /// <summary>Знімок для картки — підставляє веб-шар за категорією.</summary>
        public string? Image { get; set; }
    }

    public sealed class OrderBoardPage
    {
        public List<OrderBoardItem> Items { get; set; } = new();

        public int Total { get; set; }

        public int Page { get; set; } = 1;

        public int Pages { get; set; } = 1;
    }

    public sealed record OrderBoardCategory(int Id, string Name, int? ParentId);

    public enum BidResult
    {
        Sent,
        NotFound,
        OwnOrder,
        AlreadySent,
        Invalid
    }

    public interface IOrderBoardService
    {
        Task<OrderBoardPage> SearchAsync(OrderBoardQuery query, string? userId, CancellationToken cancellationToken = default);

        Task<List<OrderBoardCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default);

        Task<BidResult> PlaceBidAsync(
            string executorId, int orderId, decimal price, int? days, string? comment,
            CancellationToken cancellationToken = default);

        /// <summary>Додає замовлення в обране або прибирає звідти. null — замовлення немає.</summary>
        Task<bool?> ToggleFavoriteAsync(string userId, int orderId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Відкриті замовлення для виконавців: ще без виконавця, у статусі «очікує».
    /// Окремих полів «регулярне» чи «формат роботи» в замовленні немає, тому їх визначаємо
    /// з того, що є: онлайн-замовлення мають адресу «Онлайн», регулярні — згадку про
    /// періодичність в описі, термінові — строк до двох днів.
    /// </summary>
    public sealed class OrderBoardService(ApplicationDbContext db, ILogger<OrderBoardService> logger) : IOrderBoardService
    {
        public const string RemoteAddress = "Онлайн";

        private const int UrgentDays = 2;

        private static readonly string[] RegularWords =
        {
            "регуляр", "щотиж", "щоміс", "щодня", "щоден", "постійн", "раз на тиждень", "раз на місяць"
        };

        /// <summary>Центри міст для пошуку в радіусі — координати замовлень порівнюємо з ними.</summary>
        public static readonly IReadOnlyDictionary<string, (double Lat, double Lng)> Cities =
            new Dictionary<string, (double, double)>(StringComparer.OrdinalIgnoreCase)
            {
                ["Київ"] = (50.4501, 30.5234),
                ["Львів"] = (49.8397, 24.0297),
                ["Харків"] = (49.9935, 36.2304),
                ["Одеса"] = (46.4825, 30.7233),
                ["Дніпро"] = (48.4647, 35.0462),
                ["Запоріжжя"] = (47.8388, 35.1396),
                ["Вінниця"] = (49.2331, 28.4682),
                ["Полтава"] = (49.5883, 34.5514),
                ["Чернігів"] = (51.4982, 31.2893),
                ["Черкаси"] = (49.4444, 32.0598),
                ["Житомир"] = (50.2547, 28.6587),
                ["Суми"] = (50.9077, 34.7981),
                ["Хмельницький"] = (49.4230, 26.9871),
                ["Чернівці"] = (48.2915, 25.9403),
                ["Рівне"] = (50.6199, 26.2516),
                ["Тернопіль"] = (49.5535, 25.5948),
                ["Івано-Франківськ"] = (48.9226, 24.7111),
                ["Луцьк"] = (50.7472, 25.3254),
                ["Ужгород"] = (48.6208, 22.2879),
                ["Миколаїв"] = (46.9750, 31.9946),
                ["Херсон"] = (46.6354, 32.6169),
                ["Кропивницький"] = (48.5079, 32.2623)
            };

        public static bool IsRegularText(string? title, string? description)
        {
            var text = $"{title} {description}";
            return RegularWords.Any(w => text.Contains(w, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<OrderBoardPage> SearchAsync(
            OrderBoardQuery query, string? userId, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var urgentUntil = now.Date.AddDays(UrgentDays + 1);
            var perPage = Math.Clamp(query.PerPage, 1, 50);

            var orders = db.Orders.AsNoTracking()
                .Where(o => o.Status == OrderStatus.Pending && o.ExecutorId == null);

            if (!string.IsNullOrWhiteSpace(query.Text))
            {
                var text = query.Text.Trim();
                orders = orders.Where(o => o.Title.Contains(text)
                                           || o.Description.Contains(text)
                                           || o.Category.Name.Contains(text));
            }

            if (query.CategoryId.HasValue)
            {
                var id = query.CategoryId.Value;
                orders = orders.Where(o => o.CategoryId == id || o.Category.ParentCategoryId == id);
            }

            if (!string.IsNullOrWhiteSpace(query.City))
            {
                orders = orders.Where(NearCity(query.City.Trim(), query.Radius));
            }

            if (query.OneTime || query.Regular || query.Urgent)
            {
                var regular = IsRegular();
                Expression<Func<Order, bool>> type = o => false;

                if (query.OneTime) type = Or(type, Not(regular));
                if (query.Regular) type = Or(type, regular);
                if (query.Urgent) type = Or(type, o => o.ExecutionAt != null && o.ExecutionAt < urgentUntil);

                orders = orders.Where(type);
            }

            if (query.PriceFrom.HasValue)
            {
                var from = query.PriceFrom.Value;
                orders = orders.Where(o => o.Price >= from);
            }

            if (query.PriceTo.HasValue)
            {
                var to = query.PriceTo.Value;
                orders = orders.Where(o => o.Price <= to);
            }

            orders = query.Due switch
            {
                "today" => orders.Where(o => o.ExecutionAt != null && o.ExecutionAt < now.Date.AddDays(2)),
                "3" => orders.Where(o => o.ExecutionAt != null && o.ExecutionAt < now.Date.AddDays(4)),
                "7" => orders.Where(o => o.ExecutionAt != null && o.ExecutionAt < now.Date.AddDays(8)),
                "30" => orders.Where(o => o.ExecutionAt != null && o.ExecutionAt < now.Date.AddDays(31)),
                "none" => orders.Where(o => o.ExecutionAt == null),
                _ => orders
            };

            orders = query.Place switch
            {
                "remote" => orders.Where(o => o.Address == RemoteAddress),
                "onsite" => orders.Where(o => o.Address != RemoteAddress),
                _ => orders
            };

            orders = query.Sort switch
            {
                OrderBoardSort.Oldest => orders.OrderBy(o => o.CreatedAt),
                OrderBoardSort.PriceHigh => orders.OrderByDescending(o => o.Price).ThenByDescending(o => o.CreatedAt),
                OrderBoardSort.PriceLow => orders.OrderBy(o => o.Price).ThenByDescending(o => o.CreatedAt),
                OrderBoardSort.Deadline => orders.OrderBy(o => o.ExecutionAt == null).ThenBy(o => o.ExecutionAt),
                OrderBoardSort.FewBids => orders.OrderBy(o => o.Applications.Count).ThenByDescending(o => o.CreatedAt),
                _ => orders.OrderByDescending(o => o.CreatedAt)
            };

            var total = await orders.CountAsync(cancellationToken);
            var pages = Math.Max(1, (int)Math.Ceiling(total / (double)perPage));
            var page = Math.Clamp(query.Page, 1, pages);

            var me = userId ?? string.Empty;

            var items = await orders
                .Skip((page - 1) * perPage)
                .Take(perPage)
                .Select(o => new OrderBoardItem
                {
                    Id = o.Id,
                    Title = o.Title,
                    Description = o.Description,
                    Category = o.Category.Name,
                    Parent = o.Category.ParentCategory != null ? o.Category.ParentCategory.Name : string.Empty,
                    Price = o.Price,
                    CreatedAt = o.CreatedAt,
                    ExecutionAt = o.ExecutionAt,
                    Address = o.Address,
                    Bids = o.Applications.Count,
                    IsRemote = o.Address == RemoteAddress,
                    IsUrgent = o.ExecutionAt != null && o.ExecutionAt < urgentUntil,
                    IsMine = o.CustomerId == me,
                    HasMyBid = o.Applications.Any(a => a.ExecutorId == me),
                    IsFavorite = db.Favorites.Any(f => f.UserId == me && f.TargetOrderId == o.Id)
                })
                .ToListAsync(cancellationToken);

            foreach (var item in items)
            {
                item.IsRegular = IsRegularText(item.Title, item.Description);
            }

            return new OrderBoardPage { Items = items, Total = total, Page = page, Pages = pages };
        }

        public async Task<List<OrderBoardCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
            await db.Categories.AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .Select(c => new OrderBoardCategory(c.Id, c.Name, c.ParentCategoryId))
                .ToListAsync(cancellationToken);

        public async Task<BidResult> PlaceBidAsync(
            string executorId, int orderId, decimal price, int? days, string? comment,
            CancellationToken cancellationToken = default)
        {
            if (price < 0 || price > 1_000_000 || days is < 1 or > 365)
            {
                return BidResult.Invalid;
            }

            var order = await db.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId
                                          && o.Status == OrderStatus.Pending
                                          && o.ExecutorId == null, cancellationToken);

            if (order == null)
            {
                return BidResult.NotFound;
            }

            if (order.CustomerId == executorId)
            {
                return BidResult.OwnOrder;
            }

            if (await db.Applications.AnyAsync(a => a.OrderId == orderId && a.ExecutorId == executorId, cancellationToken))
            {
                return BidResult.AlreadySent;
            }

            db.Applications.Add(new Application
            {
                OrderId = orderId,
                ExecutorId = executorId,
                ProposedPrice = price,
                Days = days,
                Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
                CreatedAt = DateTime.UtcNow,
                Status = ApplicationStatus.Pending
            });

            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Пошук замовлень: виконавець {UserId} подав пропозицію до замовлення {OrderId}",
                executorId, orderId);

            return BidResult.Sent;
        }

        public async Task<bool?> ToggleFavoriteAsync(string userId, int orderId, CancellationToken cancellationToken = default)
        {
            if (!await db.Orders.AnyAsync(o => o.Id == orderId, cancellationToken))
            {
                return null;
            }

            var favorite = await db.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.TargetOrderId == orderId, cancellationToken);

            if (favorite != null)
            {
                db.Favorites.Remove(favorite);
            }
            else
            {
                db.Favorites.Add(new Favorite { UserId = userId, TargetOrderId = orderId });
            }

            await db.SaveChangesAsync(cancellationToken);
            return favorite == null;
        }

        // ── Вирази для фільтрів ────────────────────────────────────────────────

        private static Expression<Func<Order, bool>> IsRegular()
        {
            Expression<Func<Order, bool>> result = o => false;

            foreach (var word in RegularWords)
            {
                var w = word;
                result = Or(result, o => o.Title.Contains(w) || o.Description.Contains(w));
            }

            return result;
        }

        /// <summary>
        /// Замовлення в межах радіуса від центру міста (за координатами) або з цим містом в адресі —
        /// у старих замовлень координат може не бути.
        /// </summary>
        private static Expression<Func<Order, bool>> NearCity(string city, int? radius)
        {
            if (!Cities.TryGetValue(city, out var center))
            {
                return o => o.Address.Contains(city);
            }

            var km = radius ?? 20;
            var dLat = km / 111.0;
            var dLng = km / (111.0 * Math.Cos(center.Lat * Math.PI / 180));
            var (minLat, maxLat) = (center.Lat - dLat, center.Lat + dLat);
            var (minLng, maxLng) = (center.Lng - dLng, center.Lng + dLng);

            if (radius.HasValue)
            {
                return o => (o.Latitude != 0 || o.Longitude != 0)
                    ? o.Latitude >= minLat && o.Latitude <= maxLat && o.Longitude >= minLng && o.Longitude <= maxLng
                    : o.Address.Contains(city);
            }

            return o => o.Address.Contains(city)
                        || (o.Latitude >= minLat && o.Latitude <= maxLat && o.Longitude >= minLng && o.Longitude <= maxLng);
        }

        private static Expression<Func<Order, bool>> Or(Expression<Func<Order, bool>> left, Expression<Func<Order, bool>> right)
        {
            var parameter = left.Parameters[0];
            var body = Expression.OrElse(left.Body, new Rebind(right.Parameters[0], parameter).Visit(right.Body)!);
            return Expression.Lambda<Func<Order, bool>>(body, parameter);
        }

        private static Expression<Func<Order, bool>> Not(Expression<Func<Order, bool>> expression) =>
            Expression.Lambda<Func<Order, bool>>(Expression.Not(expression.Body), expression.Parameters);

        private sealed class Rebind(ParameterExpression from, ParameterExpression to) : ExpressionVisitor
        {
            protected override Expression VisitParameter(ParameterExpression node) => node == from ? to : node;
        }
    }
}
