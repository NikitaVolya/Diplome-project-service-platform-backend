using DAL.Context;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BLL.Public
{
    public interface IExecutorInsightsService
    {
        /// <summary>Аналітика за останні <paramref name="days"/> днів із порівнянням до попередніх таких самих.</summary>
        Task<ExecAnalytics> GetAnalyticsAsync(
            string ownerId, int days, CancellationToken cancellationToken = default);

        Task<ExecFinance> GetFinanceAsync(
            string ownerId, int days, CancellationToken cancellationToken = default);

        /// <summary>Заявка на виведення. Повертає false, якщо суми немає або вона більша за доступну.</summary>
        Task<bool> WithdrawAsync(
            string ownerId, decimal amount, int? methodId, CancellationToken cancellationToken = default);

        /// <summary>Додає спосіб виведення або, коли <paramref name="id"/> не нуль, править наявний.</summary>
        Task<int> SaveMethodAsync(
            string ownerId, int id, PayoutKind kind, string label, string? note,
            CancellationToken cancellationToken = default);

        Task<ExecMethod?> GetMethodAsync(string ownerId, int id, CancellationToken cancellationToken = default);

        Task<bool> DeleteMethodAsync(string ownerId, int methodId, CancellationToken cancellationToken = default);

        Task<int> AddTicketAsync(
            string ownerId, string topic, string category, string message,
            CancellationToken cancellationToken = default);

        Task<List<ExecTicket>> GetTicketsAsync(
            string ownerId, int take, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Числа для «Аналітики» та «Фінансів».
    ///
    /// Перегляди читаємо з денних підсумків, замовлення й заробіток — із самих замовлень.
    /// Окремої таблиці нарахувань немає навмисно: дохід виконавця — це його завершені
    /// замовлення, і другий запис про ті самі гроші рано чи пізно розійшовся б із першим.
    /// </summary>
    public sealed class ExecutorInsightsService(ApplicationDbContext db, ILogger<ExecutorInsightsService> logger)
        : IExecutorInsightsService
    {
        /// <summary>Комісія платформи — її ж показує картка «Інформація».</summary>
        public const decimal Commission = 0.05m;

        /// <summary>Мінімум, який можна вивести.</summary>
        public const decimal MinWithdraw = 200m;

        /// <summary>Скільки днів гроші за завершене замовлення лишаються «в очікуванні».</summary>
        private const int HoldDays = 3;

        private const int TransactionsOnPage = 5;

        // ───────────────────────────────────────────────────────────────────
        // Аналітика
        // ───────────────────────────────────────────────────────────────────

        public async Task<ExecAnalytics> GetAnalyticsAsync(
            string ownerId, int days, CancellationToken cancellationToken = default)
        {
            days = Math.Clamp(days, 7, 180);

            var today = DateTime.UtcNow.Date;
            var from = today.AddDays(-(days - 1));
            var before = from.AddDays(-days);

            // Беремо обидва періоди одним запитом: потім ділимо в пам'яті.
            var visits = await db.ProfileVisits
                .Where(v => v.OwnerId == ownerId && v.Day >= before && v.Day <= today)
                .OrderBy(v => v.Day)
                .ToListAsync(cancellationToken);

            var orders = await db.Orders
                .Where(o => o.ExecutorId == ownerId && o.CreatedAt >= before)
                .Select(o => new { o.CreatedAt, o.Price, Category = o.Category!.Name, o.Status })
                .ToListAsync(cancellationToken);

            var reviews = await db.Reviews
                .Where(r => r.TargetUserId == ownerId && r.CreatedAt >= before)
                .Select(r => r.CreatedAt)
                .ToListAsync(cancellationToken);

            var now = visits.Where(v => v.Day >= from).ToList();
            var past = visits.Where(v => v.Day < from).ToList();

            var model = new ExecAnalytics
            {
                From = from,
                To = today,
                Views = now.Sum(v => v.Total),
                Reviews = reviews.Count(r => r >= from),
                Orders = orders.Count(o => o.CreatedAt >= from)
            };

            var wasViews = past.Sum(v => v.Total);
            var wasReviews = reviews.Count(r => r < from);
            var wasOrders = orders.Count(o => o.CreatedAt < from);

            model.Conversion = Percent(model.Orders, model.Views);
            var wasConversion = Percent(wasOrders, wasViews);

            model.ViewsChange = Growth(model.Views, wasViews);
            model.ReviewsChange = Growth(model.Reviews, wasReviews);
            model.OrdersChange = Growth(model.Orders, wasOrders);

            // Конверсію порівнюємо у відсоткових пунктах, а не у відсотках від відсотка:
            // «+1.1%» у макеті — це різниця 6.2 і 5.1, а не приріст на 21%.
            model.ConversionChange = Math.Round(model.Conversion - wasConversion, 1);

            // ── Графік ───────────────────────────────────────────────────────
            var byDay = orders
                .Where(o => o.CreatedAt >= from)
                .GroupBy(o => o.CreatedAt.Date)
                .ToDictionary(g => g.Key, g => g.Count());

            foreach (var visit in now)
            {
                model.Days.Add(new ExecDay
                {
                    Day = visit.Day,
                    Views = visit.PortfolioViews,
                    Orders = byDay.TryGetValue(visit.Day, out var count) ? count : 0
                });
            }

            // Стеля графіка — найближче «кругле» число над піком, щоб підписи осі
            // ділилися на п'ять кроків без дробів.
            var peak = model.Days.Count == 0
                ? 0
                : model.Days.Max(d => Math.Max(d.Views, d.Orders));

            model.Ceiling = (int)Nice(peak);

            // ── Джерела ──────────────────────────────────────────────────────
            var total = Math.Max(1, model.Views);

            model.Sources = new List<ExecSource>
            {
                Source("search", "Пошук на платформі", now.Sum(v => v.FromSearch), total),
                Source("catalog", "Категорії", now.Sum(v => v.FromCatalog), total),
                Source("direct", "Прямі переходи", now.Sum(v => v.FromDirect), total),
                Source("referral", "Рекомендації", now.Sum(v => v.FromReferral), total),
                Source("other", "Інші", now.Sum(v => v.FromOther), total)
            };

            // ── Популярні послуги ────────────────────────────────────────────
            model.Popular = orders
                .Where(o => o.CreatedAt >= from)
                .GroupBy(o => o.Category)
                .Select(g => new ExecPopular { Title = g.Key, Orders = g.Count() })
                .OrderByDescending(p => p.Orders)
                .ThenBy(p => p.Title)
                .Take(4)
                .ToList();

            // ── Динаміка ─────────────────────────────────────────────────────
            var bill = orders.Where(o => o.CreatedAt >= from).ToList();
            var wasBill = orders.Where(o => o.CreatedAt < from).ToList();

            var average = bill.Count == 0 ? 0 : Math.Round(bill.Average(o => o.Price));
            var wasAverage = wasBill.Count == 0 ? 0 : Math.Round(wasBill.Average(o => o.Price));

            model.Metrics = new List<ExecMetric>
            {
                Metric("Перегляди профілю", model.Views, wasViews, model.ViewsChange, "%"),
                Metric("Відгуки профілю", model.Reviews, wasReviews, model.ReviewsChange, "%"),
                Metric("Отримано замовлень", model.Orders, wasOrders, model.OrdersChange, "%"),
                new()
                {
                    Title = "Середній чек",
                    Now = $"{average:0} грн",
                    Was = $"{wasAverage:0} грн",
                    Change = Sign(Growth((int)average, (int)wasAverage)) + "%",
                    Direction = Math.Sign(average - wasAverage)
                },
                new()
                {
                    Title = "Конверсія в замовлення",
                    Now = $"{model.Conversion:0.0}%",
                    Was = $"{wasConversion:0.0}%",
                    Change = Sign(model.ConversionChange) + "%",
                    Direction = Math.Sign(model.ConversionChange)
                }
            };

            return model;
        }

        // ───────────────────────────────────────────────────────────────────
        // Фінанси
        // ───────────────────────────────────────────────────────────────────

        public async Task<ExecFinance> GetFinanceAsync(
            string ownerId, int days, CancellationToken cancellationToken = default)
        {
            days = Math.Clamp(days, 7, 180);

            var today = DateTime.UtcNow.Date;
            var from = today.AddDays(-(days - 1));
            var before = from.AddDays(-days);

            // Нарахування рахуємо від завершених замовлень; дата нарахування — строк здачі,
            // а якщо його немає — день створення.
            var earned = await db.Orders
                .Where(o => o.ExecutorId == ownerId)
                .Select(o => new
                {
                    o.Id,
                    o.Title,
                    o.Price,
                    o.Status,
                    When = o.ExecutionAt ?? o.CreatedAt
                })
                .ToListAsync(cancellationToken);

            var payouts = await db.Payouts
                .Where(p => p.OwnerId == ownerId)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new { p.Amount, p.MethodLabel, p.Status, p.CreatedAt })
                .ToListAsync(cancellationToken);

            var methods = await db.PayoutMethods
                .Where(m => m.OwnerId == ownerId)
                .OrderByDescending(m => m.IsPrimary)
                .ThenBy(m => m.Id)
                .ToListAsync(cancellationToken);

            var done = earned.Where(o => o.Status == OrderStatus.Completed).ToList();
            var work = earned.Where(o => o.Status == OrderStatus.InProgress).ToList();

            var model = new ExecFinance
            {
                From = from,
                To = today,
                // Макет показує саме підсумок за весь час: доступне, виплачене й те,
                // що ще лежить на утриманні, разом дають це число.
                Income = Net(done.Sum(o => o.Price)),
                Pending = Net(work.Sum(o => o.Price)),
                PendingOrders = work.Count,
                Paid = payouts.Where(p => p.Status == PayoutStatus.Paid).Sum(p => p.Amount)
            };

            // Відсоток під числом порівнює два останні періоди, а не весь час:
            // підпис на макеті саме про зміну за місяць.
            var nowIncome = Net(done.Where(o => o.When >= from).Sum(o => o.Price));
            var wasIncome = Net(done.Where(o => o.When >= before && o.When < from).Sum(o => o.Price));
            model.IncomeChange = Growth((int)nowIncome, (int)wasIncome);

            // Доступне — усе зароблене, що відлежало свої дні, мінус уже замовлені виплати.
            var ready = Net(done.Where(o => o.When <= today.AddDays(-HoldDays)).Sum(o => o.Price));
            var taken = payouts
                .Where(p => p.Status != PayoutStatus.Rejected)
                .Sum(p => p.Amount);

            model.Available = Math.Max(0, ready - taken);

            // ── Графік доходу по днях ────────────────────────────────────────
            var byDay = done
                .Where(o => o.When >= from)
                .GroupBy(o => o.When.Date)
                .ToDictionary(g => g.Key, g => Net(g.Sum(o => o.Price)));

            for (var day = from; day <= today; day = day.AddDays(1))
            {
                model.Bars.Add(new ExecBar
                {
                    Day = day,
                    Amount = byDay.TryGetValue(day, out var sum) ? sum : 0
                });
            }

            model.Ceiling = model.Bars.Count == 0 ? 0 : (decimal)Nice(model.Bars.Max(b => b.Amount));

            // ── Останні транзакції: нарахування й виплати в одному списку ────
            var income = done
                .Where(o => o.When >= from)
                .Select(o => new ExecTxn
                {
                    Date = o.When,
                    Title = o.Title,
                    Income = true,
                    Amount = Net(o.Price),
                    StateKey = o.When <= today.AddDays(-HoldDays) ? "paid" : "ready",
                    State = o.When <= today.AddDays(-HoldDays) ? "Виплачено" : "Доступно"
                });

            var spent = payouts
                .Where(p => p.CreatedAt >= from)
                .Select(p => new ExecTxn
                {
                    Date = p.CreatedAt,
                    Title = $"Виведення коштів на {p.MethodLabel ?? "картку"}",
                    Income = false,
                    Amount = p.Amount,
                    StateKey = p.Status == PayoutStatus.Paid ? "paid" : "pending",
                    State = p.Status switch
                    {
                        PayoutStatus.Paid => "Виплачено",
                        PayoutStatus.Rejected => "Відхилено",
                        _ => "В очікуванні"
                    }
                });

            model.Transactions = income
                .Concat(spent)
                .OrderByDescending(t => t.Date)
                .Take(TransactionsOnPage)
                .ToList();

            model.Methods = methods
                .Select(m => new ExecMethod
                {
                    Id = m.Id,
                    Kind = m.Kind,
                    Label = m.Label,
                    Note = m.Note ?? string.Empty,
                    IsPrimary = m.IsPrimary
                })
                .ToList();

            return model;
        }

        public async Task<bool> WithdrawAsync(
            string ownerId, decimal amount, int? methodId, CancellationToken cancellationToken = default)
        {
            var finance = await GetFinanceAsync(ownerId, 30, cancellationToken);

            if (amount < MinWithdraw || amount > finance.Available)
            {
                return false;
            }

            var method = methodId == null
                ? await db.PayoutMethods
                    .Where(m => m.OwnerId == ownerId)
                    .OrderByDescending(m => m.IsPrimary)
                    .FirstOrDefaultAsync(cancellationToken)
                : await db.PayoutMethods
                    .FirstOrDefaultAsync(m => m.Id == methodId && m.OwnerId == ownerId, cancellationToken);

            if (method == null)
            {
                return false;
            }

            db.Payouts.Add(new Payout
            {
                OwnerId = ownerId,
                Amount = amount,
                MethodId = method.Id,
                MethodLabel = method.Label,
                Status = PayoutStatus.Pending,
                CreatedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Виконавець {OwnerId} замовив виплату {Amount} на спосіб {MethodId}",
                ownerId, amount, method.Id);

            return true;
        }

        public async Task<int> SaveMethodAsync(
            string ownerId, int id, PayoutKind kind, string label, string? note,
            CancellationToken cancellationToken = default)
        {
            var clean = string.IsNullOrWhiteSpace(note) ? null : note.Trim();

            if (id > 0)
            {
                var known = await db.PayoutMethods
                    .FirstOrDefaultAsync(m => m.Id == id && m.OwnerId == ownerId, cancellationToken);

                if (known == null)
                {
                    return 0;
                }

                known.Kind = kind;
                known.Label = label.Trim();
                known.Note = clean;

                await db.SaveChangesAsync(cancellationToken);

                return known.Id;
            }

            var first = !await db.PayoutMethods.AnyAsync(m => m.OwnerId == ownerId, cancellationToken);

            var method = new PayoutMethod
            {
                OwnerId = ownerId,
                Kind = kind,
                Label = label.Trim(),
                Note = clean,

                // Перший спосіб одразу стає основним — інакше виводити не було б куди.
                IsPrimary = first,
                CreatedAt = DateTime.UtcNow
            };

            db.PayoutMethods.Add(method);
            await db.SaveChangesAsync(cancellationToken);

            return method.Id;
        }

        public async Task<ExecMethod?> GetMethodAsync(
            string ownerId, int id, CancellationToken cancellationToken = default)
        {
            var row = await db.PayoutMethods
                .Where(m => m.Id == id && m.OwnerId == ownerId)
                .Select(m => new { m.Id, m.Kind, m.Label, m.Note, m.IsPrimary })
                .FirstOrDefaultAsync(cancellationToken);

            return row == null
                ? null
                : new ExecMethod
                {
                    Id = row.Id,
                    Kind = row.Kind,
                    Label = row.Label,
                    Note = row.Note ?? string.Empty,
                    IsPrimary = row.IsPrimary
                };
        }

        public async Task<bool> DeleteMethodAsync(
            string ownerId, int methodId, CancellationToken cancellationToken = default)
        {
            var method = await db.PayoutMethods
                .FirstOrDefaultAsync(m => m.Id == methodId && m.OwnerId == ownerId, cancellationToken);

            if (method == null)
            {
                return false;
            }

            // У базі немає ON DELETE SET NULL (див. ApplicationDbContext), тож звільняємо
            // заявки самі — історія виплат лишається з текстовим підписом способу.
            var linked = await db.Payouts
                .Where(p => p.OwnerId == ownerId && p.MethodId == method.Id)
                .ToListAsync(cancellationToken);

            foreach (var payout in linked)
            {
                payout.MethodId = null;
            }

            db.PayoutMethods.Remove(method);
            await db.SaveChangesAsync(cancellationToken);

            // Основний спосіб прибрали — найстарший із решти займає його місце.
            var next = await db.PayoutMethods
                .Where(m => m.OwnerId == ownerId)
                .OrderBy(m => m.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (method.IsPrimary && next != null)
            {
                next.IsPrimary = true;
                await db.SaveChangesAsync(cancellationToken);
            }

            return true;
        }

        // ───────────────────────────────────────────────────────────────────
        // Підтримка
        // ───────────────────────────────────────────────────────────────────

        public async Task<int> AddTicketAsync(
            string ownerId, string topic, string category, string message,
            CancellationToken cancellationToken = default)
        {
            var ticket = new SupportTicket
            {
                OwnerId = ownerId,
                Topic = topic.Trim(),
                Category = category.Trim(),
                Message = message.Trim(),
                Status = TicketStatus.New,
                CreatedAt = DateTime.UtcNow
            };

            db.SupportTickets.Add(ticket);
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Нове звернення в підтримку {TicketId} від {OwnerId}", ticket.Id, ownerId);

            return ticket.Id;
        }

        public async Task<List<ExecTicket>> GetTicketsAsync(
            string ownerId, int take, CancellationToken cancellationToken = default)
        {
            var rows = await db.SupportTickets
                .Where(t => t.OwnerId == ownerId)
                .OrderByDescending(t => t.CreatedAt)
                .Take(Math.Clamp(take, 1, 20))
                .Select(t => new { t.Id, t.Topic, t.Category, t.Message, t.Status, t.CreatedAt })
                .ToListAsync(cancellationToken);

            return rows
                .Select(t => new ExecTicket
                {
                    Id = t.Id,
                    Topic = t.Topic,
                    Category = t.Category,
                    Message = t.Message,
                    Status = t.Status,
                    CreatedAt = t.CreatedAt
                })
                .ToList();
        }

        // ───────────────────────────────────────────────────────────────────
        // Допоміжне
        // ───────────────────────────────────────────────────────────────────

        /// <summary>
        /// Кругла стеля для осі графіка: найменше число з ряду 5, 10, 25, 50, 100…,
        /// помножене на п'ять кроків, яке все ще більше за пік.
        /// </summary>
        private static double Nice(decimal peak)
        {
            int[] steps = { 1, 2, 5, 10, 20, 25, 50, 100, 200, 250, 500, 1000, 2000, 2500, 5000, 10000, 20000, 25000, 50000, 100000 };

            foreach (var step in steps)
            {
                if (step * 5 >= (double)peak)
                {
                    return step * 5;
                }
            }

            return Math.Ceiling((double)peak / 100000) * 100000;
        }

        /// <summary>Чисте, що лишається виконавцеві після комісії платформи.</summary>
        private static decimal Net(decimal gross) => Math.Round(gross * (1 - Commission), 2);

        private static double Percent(int part, int whole) =>
            whole == 0 ? 0 : Math.Round(part * 100d / whole, 1);

        /// <summary>Приріст у відсотках. Коли попереднього періоду не було — нуль, а не нескінченність.</summary>
        private static double Growth(int now, int was) =>
            was == 0 ? (now == 0 ? 0 : 100) : Math.Round((now - was) * 100d / was, 1);

        private static string Sign(double value) =>
            value > 0 ? $"+{value:0.#}" : value.ToString("0.#");

        private static ExecSource Source(string key, string title, int views, int total) => new()
        {
            Key = key,
            Title = title,
            Views = views,
            Share = Math.Round(views * 100d / total, 1)
        };

        private static ExecMetric Metric(string title, int now, int was, double change, string unit) => new()
        {
            Title = title,
            Now = now.ToString(),
            Was = was.ToString(),
            Change = Sign(change) + unit,
            Direction = Math.Sign(now - was)
        };
    }
}
