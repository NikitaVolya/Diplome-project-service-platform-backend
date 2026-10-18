using DAL.Context;
using Domain.Entities;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BLL.Public
{
    public enum ExecOrderTab
    {
        All,
        Fresh,
        InWork,
        Completed,
        Cancelled
    }

    public enum ExecReviewTab
    {
        Received,
        Given,
        ToRate
    }

    public interface IExecutorCabinetService
    {
        Task<ExecSummary> GetSummaryAsync(string ownerId, CancellationToken cancellationToken = default);

        Task<ExecCounters> GetCountersAsync(string ownerId, CancellationToken cancellationToken = default);

        Task<ExecOrdersPage> GetOrdersAsync(
            string ownerId, ExecOrderTab tab, string query, int page, int perPage,
            CancellationToken cancellationToken = default);

        Task<ExecServicesPage> GetServicesAsync(
            string ownerId, string query, int? categoryId, int page, int perPage,
            CancellationToken cancellationToken = default);

        Task<ExecPortfolio> GetPortfolioAsync(
            string ownerId, string group, int page, int perPage,
            CancellationToken cancellationToken = default);

        Task<ExecCalendar> GetCalendarAsync(
            string ownerId, DateTime week, string query, CancellationToken cancellationToken = default);

        Task<ExecReviews> GetReviewsAsync(
            string ownerId, ExecReviewTab tab, string sort, int page, int perPage,
            CancellationToken cancellationToken = default);

        Task<bool> AnswerOrderAsync(string ownerId, int orderId, bool accept, CancellationToken cancellationToken = default);

        Task<bool> ToggleServiceAsync(string ownerId, int serviceId, CancellationToken cancellationToken = default);

        Task<bool> DeleteServiceAsync(string ownerId, int serviceId, CancellationToken cancellationToken = default);

        Task<ExecutorService?> GetServiceAsync(string ownerId, int serviceId, CancellationToken cancellationToken = default);

        Task SaveServiceAsync(string ownerId, ExecutorService service, CancellationToken cancellationToken = default);

        Task<bool> AddEventAsync(string ownerId, CalendarEvent item, CancellationToken cancellationToken = default);

        Task<bool> AddWorkAsync(string ownerId, PortfolioWork work, CancellationToken cancellationToken = default);

        Task<bool> DeleteWorkAsync(string ownerId, int workId, CancellationToken cancellationToken = default);

        Task<List<(int Id, string Name, string Parent)>> GetLeafCategoriesAsync(CancellationToken cancellationToken = default);
    }

    public sealed class ExecutorCabinetService(ApplicationDbContext db, ILogger<ExecutorCabinetService> logger)
        : IExecutorCabinetService
    {
        private const int OrdersOnHome = 3;
        private const int ServicesOnHome = 4;
        private const int WorksOnHome = 4;


        public async Task<ExecSummary> GetSummaryAsync(string ownerId, CancellationToken cancellationToken = default)
        {
            var summary = new ExecSummary
            {
                Counters = await GetCountersAsync(ownerId, cancellationToken)
            };

            var fresh = await GetOrdersAsync(ownerId, ExecOrderTab.Fresh, string.Empty, 1, OrdersOnHome, cancellationToken);
            summary.Orders = fresh.Items;

            var services = await GetServicesAsync(ownerId, string.Empty, null, 1, ServicesOnHome, cancellationToken);
            summary.Services = services.Items;

            var portfolio = await GetPortfolioAsync(ownerId, string.Empty, 1, WorksOnHome, cancellationToken);
            summary.Works = portfolio.Items;

            var since = DateTime.UtcNow.AddDays(-30);

            summary.Month = new ExecMonth
            {
                Views = await db.Users.Where(u => u.Id == ownerId).Select(u => u.ProfileViews).FirstOrDefaultAsync(cancellationToken),
                Orders = await db.Orders.CountAsync(o => o.ExecutorId == ownerId && o.CreatedAt >= since, cancellationToken),
                Earned = await db.Payments
                    .Where(p => p.Order.ExecutorId == ownerId && p.Status == PaymentStatus.Completed && p.CreatedAt >= since)
                    .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m
            };

            summary.Completeness = await CompletenessAsync(ownerId, cancellationToken);

            return summary;
        }

        public async Task<ExecCounters> GetCountersAsync(string ownerId, CancellationToken cancellationToken = default)
        {
            var rows = await db.Orders
                .Where(o => o.ExecutorId == ownerId)
                .GroupBy(o => o.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            return new ExecCounters
            {
                Fresh = rows.Where(r => r.Status == OrderStatus.Pending).Sum(r => r.Count),
                Active = rows.Where(r => r.Status == OrderStatus.InProgress).Sum(r => r.Count),
                Completed = rows.Where(r => r.Status == OrderStatus.Completed).Sum(r => r.Count),
                Cancelled = rows.Where(r => r.Status == OrderStatus.Cancelled).Sum(r => r.Count),
                Earned = await db.Payments
                    .Where(p => p.Order.ExecutorId == ownerId && p.Status == PaymentStatus.Completed)
                    .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m
            };
        }

        private async Task<int> CompletenessAsync(string ownerId, CancellationToken cancellationToken)
        {
            var user = await db.Users
                .Where(u => u.Id == ownerId)
                .Select(u => new
                {
                    u.FirstName, u.LastName, u.Email, u.PhoneNumber,
                    u.City, u.About, u.Headline, u.AvatarUrl, u.Experience, u.SpecializationCategoryId
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (user == null)
            {
                return 0;
            }

            var hasServices = await db.ExecutorServices.AnyAsync(s => s.OwnerId == ownerId, cancellationToken);
            var hasWorks = await db.PortfolioWorks.AnyAsync(w => w.OwnerId == ownerId, cancellationToken);

            bool[] filled =
            {
                !string.IsNullOrWhiteSpace(user.FirstName),
                !string.IsNullOrWhiteSpace(user.LastName),
                !string.IsNullOrWhiteSpace(user.Email),
                !string.IsNullOrWhiteSpace(user.PhoneNumber),
                !string.IsNullOrWhiteSpace(user.City),
                !string.IsNullOrWhiteSpace(user.About),
                !string.IsNullOrWhiteSpace(user.Headline),
                !string.IsNullOrWhiteSpace(user.Experience),
                user.SpecializationCategoryId.HasValue,
                hasServices,
                hasWorks
            };

            return (int)Math.Round(filled.Count(f => f) * 100.0 / filled.Length);
        }

        public async Task<ExecOrdersPage> GetOrdersAsync(
            string ownerId, ExecOrderTab tab, string query, int page, int perPage,
            CancellationToken cancellationToken = default)
        {
            perPage = Math.Clamp(perPage, 1, 50);

            var orders = db.Orders.Where(o => o.ExecutorId == ownerId);

            orders = tab switch
            {
                ExecOrderTab.Fresh => orders.Where(o => o.Status == OrderStatus.Pending),
                ExecOrderTab.InWork => orders.Where(o => o.Status == OrderStatus.InProgress),
                ExecOrderTab.Completed => orders.Where(o => o.Status == OrderStatus.Completed),
                ExecOrderTab.Cancelled => orders.Where(o => o.Status == OrderStatus.Cancelled),
                _ => orders
            };

            if (!string.IsNullOrWhiteSpace(query))
            {
                var text = query.Trim();
                orders = orders.Where(o => o.Title.Contains(text)
                                           || o.Category.Name.Contains(text)
                                           || o.Customer.FirstName.Contains(text)
                                           || o.Customer.LastName.Contains(text));
            }

            var total = await orders.CountAsync(cancellationToken);
            var pages = Math.Max(1, (int)Math.Ceiling(total / (double)perPage));
            page = Math.Clamp(page, 1, pages);

            var rows = await orders
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * perPage)
                .Take(perPage)
                .Select(o => new
                {
                    o.Id,
                    o.Title,
                    Category = o.Category.Name,
                    Parent = o.Category.ParentCategory != null ? o.Category.ParentCategory.Name : string.Empty,
                    o.Status,
                    o.Price,
                    o.CreatedAt,
                    o.ExecutionAt,
                    o.Address,
                    o.CustomerId,
                    CustomerName = o.Customer.FirstName + " " + o.Customer.LastName
                })
                .ToListAsync(cancellationToken);

            return new ExecOrdersPage
            {
                Page = page,
                Pages = pages,
                Total = total,
                Items = rows
                    .Select(r => new OrderRow(r.Id, r.Title, r.Category, r.Parent, r.Status,
                                              r.Price, r.CreatedAt, r.ExecutionAt, r.Address,
                                              r.CustomerId, r.CustomerName))
                    .Select(Map)
                    .ToList()
            };
        }

        public async Task<bool> AnswerOrderAsync(
            string ownerId, int orderId, bool accept, CancellationToken cancellationToken = default)
        {
            var order = await db.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId && o.ExecutorId == ownerId, cancellationToken);

            if (order == null || order.Status != OrderStatus.Pending)
            {
                return false;
            }

            if (accept)
            {
                order.Status = OrderStatus.InProgress;
            }
            else
            {
                order.ExecutorId = null;

                var bid = await db.Applications
                    .FirstOrDefaultAsync(a => a.OrderId == orderId && a.ExecutorId == ownerId, cancellationToken);

                if (bid != null)
                {
                    bid.Status = ApplicationStatus.Rejected;
                }
            }

            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Кабінет виконавця: замовлення {OrderId} {Answer} виконавцем {UserId}",
                orderId, accept ? "прийнято" : "відхилено", ownerId);

            return true;
        }
        public async Task<ExecServicesPage> GetServicesAsync(
            string ownerId, string query, int? categoryId, int page, int perPage,
            CancellationToken cancellationToken = default)
        {
            perPage = Math.Clamp(perPage, 1, 50);

            var all = db.ExecutorServices.Where(s => s.OwnerId == ownerId);

            var states = await all
                .GroupBy(s => s.State)
                .Select(g => new { State = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var services = all;

            if (!string.IsNullOrWhiteSpace(query))
            {
                var text = query.Trim();
                services = services.Where(s => s.Title.Contains(text) || s.Category.Name.Contains(text));
            }

            if (categoryId is > 0)
            {
                services = services.Where(s => s.CategoryId == categoryId || s.Category.ParentCategoryId == categoryId);
            }

            var total = await services.CountAsync(cancellationToken);
            var pages = Math.Max(1, (int)Math.Ceiling(total / (double)perPage));
            page = Math.Clamp(page, 1, pages);

            var rows = await services
                .OrderBy(s => s.State == ServiceState.Active ? 0 : s.State == ServiceState.Paused ? 1 : 2)
                .ThenByDescending(s => s.CreatedAt)
                .Skip((page - 1) * perPage)
                .Take(perPage)
                .Select(s => new
                {
                    s.Id,
                    s.Title,
                    s.CategoryId,
                    Category = s.Category.Name,
                    Parent = s.Category.ParentCategory != null ? s.Category.ParentCategory.Name : string.Empty,
                    s.FromPrice,
                    s.State,
                    s.CreatedAt,
                    s.Image
                })
                .ToListAsync(cancellationToken);

            var ids = rows.Select(r => r.CategoryId).Distinct().ToList();

            var counts = await db.Orders
                .Where(o => o.ExecutorId == ownerId && ids.Contains(o.CategoryId))
                .GroupBy(o => o.CategoryId)
                .Select(g => new { CategoryId = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var byCategory = counts.ToDictionary(c => c.CategoryId, c => c.Count);

            var rating = await db.Reviews
                .Where(r => r.TargetUserId == ownerId)
                .GroupBy(r => r.Order.CategoryId)
                .Select(g => new { CategoryId = g.Key, Rating = g.Average(r => (double)r.Rating), Count = g.Count() })
                .ToListAsync(cancellationToken);

            var marks = rating.ToDictionary(r => r.CategoryId, r => (Math.Round(r.Rating, 1), r.Count));

            return new ExecServicesPage
            {
                Page = page,
                Pages = pages,
                Active = states.Where(s => s.State == ServiceState.Active).Sum(s => s.Count),
                Paused = states.Where(s => s.State == ServiceState.Paused).Sum(s => s.Count),
                Drafts = states.Where(s => s.State == ServiceState.Draft).Sum(s => s.Count),
                OrdersTotal = await db.Orders.CountAsync(o => o.ExecutorId == ownerId, cancellationToken),
                Items = rows.Select(r => new ExecServiceCard
                {
                    Id = r.Id,
                    Title = r.Title,
                    Category = r.Category,
                    Parent = r.Parent,
                    FromPrice = r.FromPrice,
                    State = r.State,
                    CreatedAt = r.CreatedAt,
                    Orders = byCategory.TryGetValue(r.CategoryId, out var count) ? count : 0,
                    Rating = marks.TryGetValue(r.CategoryId, out var mark) ? mark.Item1 : 0,
                    Reviews = marks.TryGetValue(r.CategoryId, out var reviews) ? reviews.Item2 : 0,
                    Image = r.Image ?? string.Empty
                }).ToList()
            };
        }

        public async Task<ExecutorService?> GetServiceAsync(
            string ownerId, int serviceId, CancellationToken cancellationToken = default) =>
            await db.ExecutorServices
                .FirstOrDefaultAsync(s => s.Id == serviceId && s.OwnerId == ownerId, cancellationToken);

        public async Task SaveServiceAsync(
            string ownerId, ExecutorService service, CancellationToken cancellationToken = default)
        {
            if (service.Id == 0)
            {
                service.OwnerId = ownerId;
                service.CreatedAt = DateTime.UtcNow;
                db.ExecutorServices.Add(service);
            }
            else
            {
                service.UpdatedAt = DateTime.UtcNow;
            }

            await db.SaveChangesAsync(cancellationToken);
        }

        public async Task<bool> ToggleServiceAsync(
            string ownerId, int serviceId, CancellationToken cancellationToken = default)
        {
            var service = await GetServiceAsync(ownerId, serviceId, cancellationToken);

            if (service == null)
            {
                return false;
            }

            service.State = service.State == ServiceState.Active ? ServiceState.Paused : ServiceState.Active;
            service.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> DeleteServiceAsync(
            string ownerId, int serviceId, CancellationToken cancellationToken = default)
        {
            var service = await GetServiceAsync(ownerId, serviceId, cancellationToken);

            if (service == null)
            {
                return false;
            }

            db.ExecutorServices.Remove(service);
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<ExecPortfolio> GetPortfolioAsync(
            string ownerId, string group, int page, int perPage,
            CancellationToken cancellationToken = default)
        {
            perPage = Math.Clamp(perPage, 1, 60);

            var groups = await db.PortfolioWorks
                .Where(w => w.OwnerId == ownerId)
                .Select(w => w.Group)
                .Distinct()
                .ToListAsync(cancellationToken);

            groups = groups
                .OrderBy(PortfolioSections.Place)
                .ThenBy(g => g)
                .ToList();

            var works = db.PortfolioWorks.Where(w => w.OwnerId == ownerId);

            if (!string.IsNullOrWhiteSpace(group))
            {
                works = works.Where(w => w.Group == group);
            }

            var total = await works.CountAsync(cancellationToken);
            var pages = Math.Max(1, (int)Math.Ceiling(total / (double)perPage));
            page = Math.Clamp(page, 1, pages);

            var items = await works
                .OrderBy(w => w.Sort)
                .ThenByDescending(w => w.CreatedAt)
                .Skip((page - 1) * perPage)
                .Take(perPage)
                .Select(w => new ExecWork
                {
                    Id = w.Id,
                    Title = w.Title,
                    Group = w.Group,
                    Link = w.Link,
                    Image = w.Image ?? string.Empty
                })
                .ToListAsync(cancellationToken);

            return new ExecPortfolio { Groups = groups, Items = items, Page = page, Pages = pages };
        }

        public async Task<bool> AddWorkAsync(
            string ownerId, PortfolioWork work, CancellationToken cancellationToken = default)
        {
            work.OwnerId = ownerId;
            work.CreatedAt = DateTime.UtcNow;

            work.Sort = PortfolioSections.Rank(work.Group, 99);

            db.PortfolioWorks.Add(work);
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> DeleteWorkAsync(
            string ownerId, int workId, CancellationToken cancellationToken = default)
        {
            var work = await db.PortfolioWorks
                .FirstOrDefaultAsync(w => w.Id == workId && w.OwnerId == ownerId, cancellationToken);

            if (work == null)
            {
                return false;
            }

            db.PortfolioWorks.Remove(work);
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<ExecCalendar> GetCalendarAsync(
            string ownerId, DateTime week, string query, CancellationToken cancellationToken = default)
        {
            var monday = week.Date.AddDays(-(((int)week.DayOfWeek + 6) % 7));
            var next = monday.AddDays(7);

            var saved = await db.CalendarEvents
                .Where(e => e.OwnerId == ownerId && e.StartAt >= monday && e.StartAt < next)
                .Select(e => new ExecEvent
                {
                    Id = e.Id,
                    Title = e.Title,
                    Kind = e.Kind,
                    StartAt = e.StartAt,
                    EndAt = e.EndAt,
                    PartnerName = e.PartnerName,
                    OrderId = e.OrderId
                })
                .ToListAsync(cancellationToken);

            var deadlines = await db.Orders
                .Where(o => o.ExecutorId == ownerId
                            && o.Status != OrderStatus.Cancelled
                            && o.ExecutionAt != null
                            && o.ExecutionAt >= monday && o.ExecutionAt < next)
                .Select(o => new ExecEvent
                {
                    Id = 0,
                    OrderId = o.Id,
                    Title = o.Title,
                    Kind = EventKind.Deadline,
                    StartAt = o.ExecutionAt!.Value,
                    EndAt = o.ExecutionAt!.Value.AddHours(1),
                    PartnerName = o.Customer.FirstName + " " + o.Customer.LastName,
                    FromOrder = true
                })
                .ToListAsync(cancellationToken);

            var all = saved.Concat(deadlines).ToList();

            if (!string.IsNullOrWhiteSpace(query))
            {
                var text = query.Trim();
                all = all.Where(e => e.Title.Contains(text, StringComparison.OrdinalIgnoreCase)
                                     || (e.PartnerName ?? string.Empty).Contains(text, StringComparison.OrdinalIgnoreCase))
                         .ToList();
            }

            foreach (var item in all)
            {
                item.Day = (int)(item.StartAt.Date - monday).TotalDays;
                item.Hour = item.StartAt.Hour;
                item.Span = Math.Max(1, (int)Math.Ceiling((item.EndAt - item.StartAt).TotalHours));

                if (!string.IsNullOrWhiteSpace(item.PartnerName))
                {
                    var parts = item.PartnerName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    item.PartnerName = parts.Length < 2 ? parts[0] : $"{parts[0]} {parts[1][0]}.";
                }
            }

            var now = DateTime.UtcNow;

            return new ExecCalendar
            {
                Monday = monday,
                Events = all.OrderBy(e => e.StartAt).ToList(),
                Upcoming = all.Where(e => e.EndAt >= now).OrderBy(e => e.StartAt).Take(4).ToList()
            };
        }

        public async Task<bool> AddEventAsync(
            string ownerId, CalendarEvent item, CancellationToken cancellationToken = default)
        {
            if (item.EndAt <= item.StartAt)
            {
                item.EndAt = item.StartAt.AddHours(1);
            }

            item.OwnerId = ownerId;
            item.CreatedAt = DateTime.UtcNow;

            db.CalendarEvents.Add(item);
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        public async Task<ExecReviews> GetReviewsAsync(
            string ownerId, ExecReviewTab tab, string sort, int page, int perPage,
            CancellationToken cancellationToken = default)
        {
            perPage = Math.Clamp(perPage, 1, 50);

            var received = db.Reviews.Where(r => r.TargetUserId == ownerId);

            var stats = await received
                .GroupBy(r => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Average = g.Average(r => (double)r.Rating),
                    Good = g.Count(r => r.Rating >= 4)
                })
                .FirstOrDefaultAsync(cancellationToken);

            var result = new ExecReviews
            {
                Total = stats?.Total ?? 0,
                Average = stats == null ? 0 : Math.Round(stats.Average, 1),
                Recommend = stats is { Total: > 0 } ? (int)Math.Round(stats.Good * 100.0 / stats.Total) : 0
            };

            if (tab == ExecReviewTab.ToRate)
            {
                var waiting = await db.Orders
                    .Where(o => o.ExecutorId == ownerId
                                && o.Status == OrderStatus.Completed
                                && !db.Reviews.Any(r => r.OrderId == o.Id && r.AuthorId == ownerId))
                    .OrderByDescending(o => o.ExecutionAt)
                    .Take(perPage)
                    .Select(o => new
                    {
                        o.Id,
                        o.Title,
                        Category = o.Category.Name,
                        Parent = o.Category.ParentCategory != null ? o.Category.ParentCategory.Name : string.Empty,
                        o.Status,
                        o.Price,
                        o.CreatedAt,
                        o.ExecutionAt,
                        o.Address,
                        o.CustomerId,
                        CustomerName = o.Customer.FirstName + " " + o.Customer.LastName
                    })
                    .ToListAsync(cancellationToken);

                result.Awaiting = waiting
                    .Select(r => new OrderRow(r.Id, r.Title, r.Category, r.Parent, r.Status,
                                              r.Price, r.CreatedAt, r.ExecutionAt, r.Address,
                                              r.CustomerId, r.CustomerName))
                    .Select(Map)
                    .ToList();
                return result;
            }

            var reviews = tab == ExecReviewTab.Given
                ? db.Reviews.Where(r => r.AuthorId == ownerId)
                : received;

            reviews = sort switch
            {
                "old" => reviews.OrderBy(r => r.CreatedAt),
                "best" => reviews.OrderByDescending(r => r.Rating).ThenByDescending(r => r.CreatedAt),
                "worst" => reviews.OrderBy(r => r.Rating).ThenByDescending(r => r.CreatedAt),
                _ => reviews.OrderByDescending(r => r.CreatedAt)
            };

            var total = await reviews.CountAsync(cancellationToken);
            var pages = Math.Max(1, (int)Math.Ceiling(total / (double)perPage));
            page = Math.Clamp(page, 1, pages);

            var rows = await reviews
                .Skip((page - 1) * perPage)
                .Take(perPage)
                .Select(r => new
                {
                    r.Id,
                    r.OrderId,
                    OrderTitle = r.Order.Title,
                    Category = r.Order.Category.Name,
                    Parent = r.Order.Category.ParentCategory != null ? r.Order.Category.ParentCategory.Name : string.Empty,
                    PersonId = tab == ExecReviewTab.Given ? r.TargetUserId : r.AuthorId,
                    PersonName = tab == ExecReviewTab.Given
                        ? r.TargetUser.FirstName + " " + r.TargetUser.LastName
                        : r.Author.FirstName + " " + r.Author.LastName,
                    r.Rating,
                    r.Comment,
                    r.CreatedAt
                })
                .ToListAsync(cancellationToken);

            result.Page = page;
            result.Pages = pages;
            result.Items = rows.Select(r => new ExecReview
            {
                Id = r.Id,
                OrderId = r.OrderId,
                OrderTitle = r.Category,
                Parent = r.Parent,
                Category = r.Category,
                PersonId = r.PersonId,
                PersonName = Short(r.PersonName),
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            }).ToList();
            result.WithPhoto = rows.Count(r => !string.IsNullOrEmpty(r.Parent));

            return result;
        }


        public async Task<List<(int Id, string Name, string Parent)>> GetLeafCategoriesAsync(
            CancellationToken cancellationToken = default)
        {
            var rows = await db.Categories
                .Where(c => c.IsActive && c.ParentCategoryId != null)
                .OrderBy(c => c.ParentCategory!.Name)
                .ThenBy(c => c.Name)
                .Select(c => new { c.Id, c.Name, Parent = c.ParentCategory!.Name })
                .ToListAsync(cancellationToken);

            return rows.Select(r => (r.Id, r.Name, r.Parent)).ToList();
        }

        private sealed record OrderRow(
            int Id, string Title, string Category, string Parent, OrderStatus Status,
            decimal Price, DateTime CreatedAt, DateTime? ExecutionAt, string? Address,
            string CustomerId, string CustomerName);

        private static ExecOrder Map(OrderRow row) => new()
        {
            Id = row.Id,
            Title = row.Title,
            Category = row.Category,
            Parent = row.Parent,
            Status = row.Status,
            OnReview = row.Status == OrderStatus.InProgress
                       && row.ExecutionAt != null
                       && row.ExecutionAt < DateTime.UtcNow,
            Price = row.Price,
            CreatedAt = row.CreatedAt,
            ExecutionAt = row.ExecutionAt,
            Address = row.Address ?? string.Empty,
            CustomerId = row.CustomerId,
            CustomerName = Short(row.CustomerName)
        };

        private static string Short(string? full)
        {
            if (string.IsNullOrWhiteSpace(full))
            {
                return string.Empty;
            }

            var parts = full.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            return parts.Length < 2 ? parts[0] : $"{parts[0]} {parts[1][0]}.";
        }
    }
}
