using DAL.Context;
using Domain.Entities;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BLL.Public
{
    public enum CabinetOrderTab
    {
        All,
        Active,
        InProgress,
        Completed,
        Cancelled
    }

    public enum CabinetSide
    {
        Customer,
        Executor
    }

    public enum CabinetThreadFilter
    {
        All,
        Unread,
        Archive
    }

    public enum CabinetPaymentType
    {
        All,
        Payment,
        Refund
    }

    public interface ICabinetService
    {
        Task<CabinetOwner> ResolveOwnerAsync(string userId, CancellationToken cancellationToken = default);

        Task<CabinetSummary> GetSummaryAsync(string ownerId, CancellationToken cancellationToken = default);

        Task<CabinetOrdersPage> GetOrdersAsync(
            string ownerId, CabinetOrderTab tab, string query, int page, int perPage,
            CancellationToken cancellationToken = default);

        Task<CabinetOrderDetails?> GetOrderAsync(string ownerId, int orderId, CancellationToken cancellationToken = default);

        Task<List<CabinetThread>> GetThreadsAsync(
            string ownerId, CabinetThreadFilter filter, string query,
            CabinetSide side = CabinetSide.Customer, CancellationToken cancellationToken = default);

        Task<CabinetChat?> GetChatAsync(
            string ownerId, int orderId,
            CabinetSide side = CabinetSide.Customer, CancellationToken cancellationToken = default);

        Task<CabinetFavorites> GetFavoritesAsync(string ownerId, string query, string group, CancellationToken cancellationToken = default);

        Task<CabinetWallet> GetWalletAsync(string ownerId, CancellationToken cancellationToken = default);

        Task<CabinetTransactionsPage> GetTransactionsAsync(
            string ownerId, CabinetPaymentType type, string query, int page, int perPage,
            CancellationToken cancellationToken = default);

        Task<List<CabinetReview>> GetReviewsAsync(string ownerId, CancellationToken cancellationToken = default);

        Task<int> GetUnreadCountAsync(
            string ownerId, CabinetSide side = CabinetSide.Customer, CancellationToken cancellationToken = default);

        Task<List<UserAddress>> GetAddressesAsync(string userId, CancellationToken cancellationToken = default);

        Task<Order> CreateOrderAsync(string userId, Order order, CancellationToken cancellationToken = default);

        Task<bool> SendMessageAsync(
            string userId, int orderId, string text,
            CabinetSide side = CabinetSide.Customer, CancellationToken cancellationToken = default);

        Task<bool> AnswerBidAsync(string userId, int bidId, bool accept, CancellationToken cancellationToken = default);
    }

    public sealed class CabinetService(ApplicationDbContext db, ILogger<CabinetService> logger) : ICabinetService
    {
        private const int OrdersOnHome = 3;

        private const int ThreadsOnHome = 3;

        public async Task<CabinetOwner> ResolveOwnerAsync(string userId, CancellationToken cancellationToken = default)
        {
            var owner = new CabinetOwner { Id = userId };

            if (string.IsNullOrEmpty(userId))
            {
                return owner;
            }

            if (await db.Orders.AnyAsync(o => o.CustomerId == userId, cancellationToken))
            {
                return owner;
            }

            var demo = await db.Orders
                .GroupBy(o => o.CustomerId)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefaultAsync(cancellationToken);

            return demo == null ? owner : new CabinetOwner { Id = demo, IsDemo = true };
        }


        public async Task<CabinetSummary> GetSummaryAsync(string ownerId, CancellationToken cancellationToken = default)
        {
            var summary = new CabinetSummary();

            var counts = await db.Orders
                .Where(o => o.CustomerId == ownerId)
                .GroupBy(o => o.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            summary.Active = counts.Where(c => c.Status is OrderStatus.Pending or OrderStatus.InProgress).Sum(c => c.Count);
            summary.Completed = counts.Where(c => c.Status == OrderStatus.Completed).Sum(c => c.Count);

            summary.AwaitingReview = await db.Orders
                .Where(o => o.CustomerId == ownerId
                            && o.Status == OrderStatus.Completed
                            && !db.Reviews.Any(r => r.OrderId == o.Id && r.AuthorId == ownerId))
                .CountAsync(cancellationToken);

            summary.Spent = await db.Payments
                .Where(p => p.UserId == ownerId && p.Status == PaymentStatus.Completed)
                .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;

            var page = await GetOrdersAsync(ownerId, CabinetOrderTab.Active, string.Empty, 1, OrdersOnHome, cancellationToken);
            summary.Orders = page.Items;

            var threads = await GetThreadsAsync(
                ownerId, CabinetThreadFilter.All, string.Empty, CabinetSide.Customer, cancellationToken);
            summary.Threads = threads.Take(ThreadsOnHome).ToList();

            return summary;
        }

        public async Task<CabinetOrdersPage> GetOrdersAsync(
            string ownerId, CabinetOrderTab tab, string query, int page, int perPage,
            CancellationToken cancellationToken = default)
        {
            perPage = Math.Clamp(perPage, 1, 50);

            var orders = db.Orders.Where(o => o.CustomerId == ownerId);

            orders = tab switch
            {
                CabinetOrderTab.Active => orders.Where(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.InProgress),
                CabinetOrderTab.InProgress => orders.Where(o => o.Status == OrderStatus.InProgress),
                CabinetOrderTab.Completed => orders.Where(o => o.Status == OrderStatus.Completed),
                CabinetOrderTab.Cancelled => orders.Where(o => o.Status == OrderStatus.Cancelled),
                _ => orders
            };

            if (!string.IsNullOrWhiteSpace(query))
            {
                var text = query.Trim();
                orders = orders.Where(o => o.Title.Contains(text) || o.Category.Name.Contains(text));
            }

            var total = await orders.CountAsync(cancellationToken);
            var pages = Math.Max(1, (int)Math.Ceiling(total / (double)perPage));
            page = Math.Clamp(page, 1, pages);

            var items = await orders
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
                    o.ExecutorId,
                    ExecutorName = o.Executor != null ? o.Executor.FirstName + " " + o.Executor.LastName : null
                })
                .ToListAsync(cancellationToken);

            return new CabinetOrdersPage
            {
                Page = page,
                Pages = pages,
                Total = total,
                Items = items.Select(o => new CabinetOrder
                {
                    Id = o.Id,
                    Title = o.Title,
                    Category = o.Category,
                    Parent = o.Parent,
                    Status = o.Status,
                    Price = o.Price,
                    CreatedAt = o.CreatedAt,
                    ExecutionAt = o.ExecutionAt,
                    ExecutorId = o.ExecutorId,
                    ExecutorName = ShortName(o.ExecutorName)
                }).ToList()
            };
        }

        public async Task<CabinetOrderDetails?> GetOrderAsync(
            string ownerId, int orderId, CancellationToken cancellationToken = default)
        {
            var row = await db.Orders
                .Where(o => o.Id == orderId && o.CustomerId == ownerId)
                .Select(o => new
                {
                    o.Id,
                    o.Title,
                    o.Description,
                    o.Address,
                    Category = o.Category.Name,
                    Parent = o.Category.ParentCategory != null ? o.Category.ParentCategory.Name : string.Empty,
                    o.Status,
                    o.Price,
                    o.CreatedAt,
                    o.ExecutionAt,
                    o.ExecutorId,
                    ExecutorName = o.Executor != null ? o.Executor.FirstName + " " + o.Executor.LastName : null
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (row == null)
            {
                return null;
            }

            var bidRows = await db.Applications
                .Where(a => a.OrderId == orderId && a.Executor != null)
                .OrderByDescending(a => a.Status == ApplicationStatus.Accepted)
                .ThenBy(a => a.CreatedAt)
                .Select(a => new
                {
                    a.Id,
                    a.ExecutorId,
                    Name = a.Executor!.FirstName + " " + a.Executor.LastName,
                    a.ProposedPrice,
                    a.Days,
                    a.Status,
                    a.CreatedAt
                })
                .ToListAsync(cancellationToken);

            var executorIds = bidRows.Select(b => b.ExecutorId!).Distinct().ToList();
            var ratings = await RatingsAsync(executorIds, cancellationToken);
            var roles = await TopServiceAsync(executorIds, cancellationToken);

            var details = new CabinetOrderDetails
            {
                Description = row.Description ?? string.Empty,
                Address = row.Address ?? string.Empty,
                Order = new CabinetOrder
                {
                    Id = row.Id,
                    Title = row.Title,
                    Category = row.Category,
                    Parent = row.Parent,
                    Status = row.Status,
                    Price = row.Price,
                    CreatedAt = row.CreatedAt,
                    ExecutionAt = row.ExecutionAt,
                    ExecutorId = row.ExecutorId,
                    ExecutorName = ShortName(row.ExecutorName)
                },
                Bids = bidRows.Select(b => new CabinetBid
                {
                    Id = b.Id,
                    ExecutorId = b.ExecutorId!,
                    Name = b.Name,
                    Role = roles.TryGetValue(b.ExecutorId!, out var role) ? role : "Виконавець",
                    Rating = ratings.TryGetValue(b.ExecutorId!, out var rating) ? rating.Rating : 0,
                    Reviews = ratings.TryGetValue(b.ExecutorId!, out var count) ? count.Count : 0,
                    Price = b.ProposedPrice,
                    Days = b.Days ?? 0,
                    Status = b.Status
                }).ToList()
            };

            details.Activity.Add(new CabinetEvent { Text = "Замовлення опубліковано", At = row.CreatedAt });

            foreach (var bid in bidRows.OrderBy(b => b.CreatedAt).Take(6))
            {
                details.Activity.Add(new CabinetEvent
                {
                    Text = "Новий відгук від {0}",
                    Arg = ShortName(bid.Name),
                    At = bid.CreatedAt
                });
            }

            if (row.Status == OrderStatus.InProgress)
            {
                details.Activity.Add(new CabinetEvent { Text = "Виконавець розпочав роботу", At = row.ExecutionAt ?? row.CreatedAt });
            }
            else if (row.Status == OrderStatus.Completed)
            {
                details.Activity.Add(new CabinetEvent { Text = "Замовлення виконано", At = row.ExecutionAt ?? row.CreatedAt });
            }
            else if (row.Status == OrderStatus.Cancelled)
            {
                details.Activity.Add(new CabinetEvent { Text = "Замовлення скасовано", At = row.ExecutionAt ?? row.CreatedAt });
            }

            return details;
        }

        public async Task<List<CabinetThread>> GetThreadsAsync(
            string ownerId, CabinetThreadFilter filter, string query,
            CabinetSide side = CabinetSide.Customer, CancellationToken cancellationToken = default)
        {
            var mine = Mine(side, ownerId);
            var messages = db.OrderMessages.Where(m => mine.Contains(m.OrderId));

            if (!string.IsNullOrWhiteSpace(query))
            {
                var text = query.Trim();
                messages = messages.Where(m => m.Order.Title.Contains(text) || m.Text.Contains(text));
            }

            var heads = await messages
                .GroupBy(m => m.OrderId)
                .Select(g => new
                {
                    OrderId = g.Key,
                    LastAt = g.Max(m => m.SentAt),
                    Unread = g.Count(m => m.SenderId != ownerId && !m.IsRead)
                })
                .ToListAsync(cancellationToken);

            if (heads.Count == 0)
            {
                return new List<CabinetThread>();
            }

            if (filter == CabinetThreadFilter.Unread)
            {
                heads = heads.Where(h => h.Unread > 0).ToList();
            }
            else if (filter == CabinetThreadFilter.Archive)
            {
                var closed = await db.Orders
                    .Where(o => mine.Contains(o.Id)
                                && (o.Status == OrderStatus.Completed || o.Status == OrderStatus.Cancelled))
                    .Select(o => o.Id)
                    .ToListAsync(cancellationToken);

                var closedSet = closed.ToHashSet();
                heads = heads.Where(h => closedSet.Contains(h.OrderId)).ToList();
            }

            var ids = heads.Select(h => h.OrderId).ToList();

            var last = await db.OrderMessages
                .Where(m => ids.Contains(m.OrderId))
                .Select(m => new { m.OrderId, m.Text, m.SentAt })
                .ToListAsync(cancellationToken);

            var lastByOrder = last
                .GroupBy(m => m.OrderId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(m => m.SentAt).First().Text);

            var orders = await db.Orders
                .Where(o => ids.Contains(o.Id))
                .Select(o => new
                {
                    o.Id,
                    o.Title,
                    PartnerId = side == CabinetSide.Customer ? o.ExecutorId : o.CustomerId,
                    PartnerName = side == CabinetSide.Customer
                        ? (o.Executor != null ? o.Executor.FirstName + " " + o.Executor.LastName : null)
                        : o.Customer.FirstName + " " + o.Customer.LastName
                })
                .ToListAsync(cancellationToken);

            var orderById = orders.ToDictionary(o => o.Id);

            return heads
                .OrderByDescending(h => h.LastAt)
                .Select(h =>
                {
                    orderById.TryGetValue(h.OrderId, out var order);

                    return new CabinetThread
                    {
                        OrderId = h.OrderId,
                        OrderTitle = order?.Title ?? string.Empty,
                        PartnerId = order?.PartnerId ?? string.Empty,
                        PartnerName = order?.PartnerName
                                      ?? (side == CabinetSide.Customer ? "Виконавець" : "Замовник"),
                        Last = lastByOrder.TryGetValue(h.OrderId, out var text) ? text : string.Empty,
                        LastAt = h.LastAt,
                        Unread = h.Unread
                    };
                })
                .ToList();
        }

        public async Task<CabinetChat?> GetChatAsync(
            string ownerId, int orderId,
            CabinetSide side = CabinetSide.Customer, CancellationToken cancellationToken = default)
        {
            var order = await db.Orders
                .Where(o => o.Id == orderId
                            && (side == CabinetSide.Customer ? o.CustomerId == ownerId : o.ExecutorId == ownerId))
                .Select(o => new
                {
                    o.Id,
                    o.Title,
                    PartnerId = side == CabinetSide.Customer ? o.ExecutorId : o.CustomerId,
                    PartnerName = side == CabinetSide.Customer
                        ? (o.Executor != null ? o.Executor.FirstName + " " + o.Executor.LastName : null)
                        : o.Customer.FirstName + " " + o.Customer.LastName
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (order == null)
            {
                return null;
            }

            var rows = await db.OrderMessages
                .Where(m => m.OrderId == orderId)
                .OrderBy(m => m.SentAt)
                .Select(m => new
                {
                    m.Id,
                    m.SenderId,
                    SenderName = m.Sender.FirstName + " " + m.Sender.LastName,
                    m.Text,
                    m.SentAt,
                    m.IsRead
                })
                .ToListAsync(cancellationToken);

            return new CabinetChat
            {
                Head = new CabinetThread
                {
                    OrderId = order.Id,
                    OrderTitle = order.Title,
                    PartnerId = order.PartnerId ?? string.Empty,
                    PartnerName = order.PartnerName ?? (side == CabinetSide.Customer ? "Виконавець" : "Замовник"),
                    LastAt = rows.Count > 0 ? rows[^1].SentAt : DateTime.UtcNow
                },
                Messages = rows.Select(m => new CabinetMessage
                {
                    Id = m.Id,
                    Mine = m.SenderId == ownerId,
                    Author = m.SenderName,
                    Text = m.Text,
                    SentAt = m.SentAt,
                    IsRead = m.IsRead
                }).ToList()
            };
        }

        public async Task<int> GetUnreadCountAsync(
            string ownerId, CabinetSide side = CabinetSide.Customer, CancellationToken cancellationToken = default)
        {
            var mine = Mine(side, ownerId);

            return await db.OrderMessages
                .CountAsync(m => mine.Contains(m.OrderId) && m.SenderId != ownerId && !m.IsRead, cancellationToken);
        }

        private IQueryable<int> Mine(CabinetSide side, string ownerId) =>
            side == CabinetSide.Customer
                ? db.Orders.Where(o => o.CustomerId == ownerId).Select(o => o.Id)
                : db.Orders.Where(o => o.ExecutorId == ownerId).Select(o => o.Id);

        public async Task<bool> SendMessageAsync(
            string userId, int orderId, string text,
            CabinetSide side = CabinetSide.Customer, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            var mine = await db.Orders.AnyAsync(
                o => o.Id == orderId
                     && (side == CabinetSide.Customer ? o.CustomerId == userId : o.ExecutorId == userId),
                cancellationToken);

            if (!mine)
            {
                logger.LogWarning("Спроба написати в чуже замовлення {OrderId}", orderId);
                return false;
            }

            db.OrderMessages.Add(new OrderMessage
            {
                OrderId = orderId,
                SenderId = userId,
                Text = text.Trim().Length > 1000 ? text.Trim()[..1000] : text.Trim(),
                SentAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync(cancellationToken);
            return true;
        }


        public async Task<CabinetFavorites> GetFavoritesAsync(
            string ownerId, string query, string group, CancellationToken cancellationToken = default)
        {
            var rows = await db.Favorites
                .Where(f => f.UserId == ownerId && f.TargetExecutorId != null)
                .Select(f => new
                {
                    f.Id,
                    ExecutorId = f.TargetExecutorId!,
                    Name = f.TargetExecutor!.FirstName + " " + f.TargetExecutor.LastName
                })
                .ToListAsync(cancellationToken);

            if (rows.Count == 0)
            {
                return new CabinetFavorites();
            }

            var ids = rows.Select(r => r.ExecutorId).Distinct().ToList();
            var ratings = await RatingsAsync(ids, cancellationToken);

            var work = await db.Orders
                .Where(o => o.ExecutorId != null && ids.Contains(o.ExecutorId))
                .GroupBy(o => new { o.ExecutorId, o.CategoryId })
                .Select(g => new
                {
                    g.Key.ExecutorId,
                    g.Key.CategoryId,
                    Count = g.Count()
                })
                .ToListAsync(cancellationToken);

            var categories = await db.Categories
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    Parent = c.ParentCategory != null ? c.ParentCategory.Name : c.Name
                })
                .ToListAsync(cancellationToken);

            var byId = categories.ToDictionary(c => c.Id);

            var items = new List<CabinetFavorite>(rows.Count);

            foreach (var row in rows)
            {
                var done = work
                    .Where(w => w.ExecutorId == row.ExecutorId)
                    .OrderByDescending(w => w.Count)
                    .ToList();

                var skills = done
                    .Select(w => byId.TryGetValue(w.CategoryId, out var c) ? c.Name : null)
                    .Where(name => name != null)
                    .Take(3)
                    .Select(name => name!)
                    .ToList();

                var first = done.Count > 0 && byId.TryGetValue(done[0].CategoryId, out var top) ? top : null;

                items.Add(new CabinetFavorite
                {
                    Id = row.Id,
                    ExecutorId = row.ExecutorId,
                    Name = row.Name,
                    Group = first?.Parent ?? "Інше",
                    Role = first?.Name ?? "Виконавець",
                    Rating = ratings.TryGetValue(row.ExecutorId, out var r) ? r.Rating : 0,
                    Reviews = ratings.TryGetValue(row.ExecutorId, out var c2) ? c2.Count : 0,
                    Skills = skills
                });
            }

            var groups = items
                .GroupBy(i => i.Group)
                .Select(g => new CabinetFavoriteGroup { Name = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .ThenBy(g => g.Name)
                .ToList();

            if (!string.IsNullOrWhiteSpace(query))
            {
                var text = query.Trim();
                items = items
                    .Where(i => i.Name.Contains(text, StringComparison.OrdinalIgnoreCase)
                                || i.Role.Contains(text, StringComparison.OrdinalIgnoreCase)
                                || i.Skills.Any(s => s.Contains(text, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(group))
            {
                items = items.Where(i => string.Equals(i.Group, group, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            return new CabinetFavorites
            {
                Groups = groups,
                Items = items.OrderBy(i => i.Group).ThenByDescending(i => i.Rating).ToList()
            };
        }

        public async Task<CabinetWallet> GetWalletAsync(string ownerId, CancellationToken cancellationToken = default)
        {
            var totals = await db.Payments
                .Where(p => p.UserId == ownerId)
                .GroupBy(p => p.Status)
                .Select(g => new { Status = g.Key, Count = g.Count(), Sum = g.Sum(p => p.Amount) })
                .ToListAsync(cancellationToken);

            var balance = await db.Users
                .Where(u => u.Id == ownerId)
                .Select(u => u.WalletBalance)
                .FirstOrDefaultAsync(cancellationToken);

            return new CabinetWallet
            {
                Balance = balance,
                Spent = totals.Where(t => t.Status == PaymentStatus.Completed).Sum(t => t.Sum),
                Pending = totals.Where(t => t.Status == PaymentStatus.Pending).Sum(t => t.Sum),
                PendingCount = totals.Where(t => t.Status == PaymentStatus.Pending).Sum(t => t.Count),
                Total = totals.Sum(t => t.Count)
            };
        }

        public async Task<CabinetTransactionsPage> GetTransactionsAsync(
            string ownerId, CabinetPaymentType type, string query, int page, int perPage,
            CancellationToken cancellationToken = default)
        {
            perPage = Math.Clamp(perPage, 1, 50);

            var payments = db.Payments.Where(p => p.UserId == ownerId);

            payments = type switch
            {
                CabinetPaymentType.Payment => payments.Where(p => p.Status != PaymentStatus.Refunded),
                CabinetPaymentType.Refund => payments.Where(p => p.Status == PaymentStatus.Refunded),
                _ => payments
            };

            if (!string.IsNullOrWhiteSpace(query))
            {
                var text = query.Trim();
                payments = payments.Where(p => p.Order.Title.Contains(text) || p.ExternalTransactionId.Contains(text));
            }

            var total = await payments.CountAsync(cancellationToken);
            var pages = Math.Max(1, (int)Math.Ceiling(total / (double)perPage));
            page = Math.Clamp(page, 1, pages);

            var items = await payments
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * perPage)
                .Take(perPage)
                .Select(p => new CabinetTransaction
                {
                    Id = p.Id,
                    CreatedAt = p.CreatedAt,
                    Title = p.Order.Title,
                    OrderId = p.OrderId,
                    Amount = p.Amount,
                    Status = p.Status,
                    Incoming = p.Status == PaymentStatus.Refunded
                })
                .ToListAsync(cancellationToken);

            return new CabinetTransactionsPage { Items = items, Page = page, Pages = pages, Total = total };
        }

        public async Task<List<CabinetReview>> GetReviewsAsync(string ownerId, CancellationToken cancellationToken = default) =>
            await db.Reviews
                .Where(r => r.AuthorId == ownerId)
                .OrderByDescending(r => r.CreatedAt)
                .Take(30)
                .Select(r => new CabinetReview
                {
                    Id = r.Id,
                    OrderId = r.OrderId,
                    OrderTitle = r.Order.Title,
                    TargetName = r.TargetUser.FirstName + " " + r.TargetUser.LastName,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync(cancellationToken);

        public async Task<List<UserAddress>> GetAddressesAsync(string userId, CancellationToken cancellationToken = default) =>
            await db.UserAddresses
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.IsPrimary)
                .ThenBy(a => a.Id)
                .ToListAsync(cancellationToken);

        public async Task<Order> CreateOrderAsync(string userId, Order order, CancellationToken cancellationToken = default)
        {
            order.CustomerId = userId;
            order.Status = OrderStatus.Pending;
            order.CreatedAt = DateTime.UtcNow;
            order.ExecutorId = null;

            db.Orders.Add(order);
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Кабінет: створено замовлення {OrderId} користувачем {UserId}", order.Id, userId);
            return order;
        }

        public async Task<bool> AnswerBidAsync(
            string userId, int bidId, bool accept, CancellationToken cancellationToken = default)
        {
            var bid = await db.Applications
                .Include(a => a.Order)
                .FirstOrDefaultAsync(a => a.Id == bidId, cancellationToken);

            if (bid?.Order == null || bid.Order.CustomerId != userId)
            {
                return false;
            }

            if (accept)
            {
                bid.Status = ApplicationStatus.Accepted;
                bid.Order.ExecutorId = bid.ExecutorId;
                bid.Order.Status = OrderStatus.InProgress;
                bid.Order.Price = bid.ProposedPrice;

                var others = await db.Applications
                    .Where(a => a.OrderId == bid.OrderId && a.Id != bid.Id)
                    .ToListAsync(cancellationToken);

                foreach (var other in others)
                {
                    other.Status = ApplicationStatus.Rejected;
                }
            }
            else
            {
                bid.Status = ApplicationStatus.Rejected;
            }

            await db.SaveChangesAsync(cancellationToken);
            return true;
        }

        private async Task<Dictionary<string, (double Rating, int Count)>> RatingsAsync(
            List<string> executorIds, CancellationToken cancellationToken)
        {
            if (executorIds.Count == 0)
            {
                return new Dictionary<string, (double, int)>();
            }

            var rows = await db.Reviews
                .Where(r => executorIds.Contains(r.TargetUserId))
                .GroupBy(r => r.TargetUserId)
                .Select(g => new
                {
                    Id = g.Key,
                    Rating = g.Average(r => (double)r.Rating),
                    Count = g.Count()
                })
                .ToListAsync(cancellationToken);

            return rows.ToDictionary(r => r.Id, r => (Math.Round(r.Rating, 1), r.Count));
        }

        private async Task<Dictionary<string, string>> TopServiceAsync(
            List<string> executorIds, CancellationToken cancellationToken)
        {
            if (executorIds.Count == 0)
            {
                return new Dictionary<string, string>();
            }

            var rows = await db.Orders
                .Where(o => o.ExecutorId != null && executorIds.Contains(o.ExecutorId))
                .GroupBy(o => new { o.ExecutorId, Name = o.Category.Name })
                .Select(g => new { g.Key.ExecutorId, g.Key.Name, Count = g.Count() })
                .ToListAsync(cancellationToken);

            return rows
                .GroupBy(r => r.ExecutorId!)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.Count).First().Name);
        }

        private static string? ShortName(string? full)
        {
            if (string.IsNullOrWhiteSpace(full))
            {
                return null;
            }

            var parts = full.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            return parts.Length < 2 ? parts[0] : $"{parts[0]} {parts[1][0]}.";
        }
    }
}
