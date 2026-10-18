using AdminPanel.Models;
using BLL.Public;
using DAL.Context;
using Domain.Entities;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace AdminPanel.Controllers;

/// <summary>
/// Особистий кабінет замовника: замовлення, листування, обране, адреси, платежі,
/// підтримка та налаштування акаунта.
///
/// Клас позначено <see cref="AllowAnonymousAttribute"/> навмисно: інакше спрацювала б
/// загальна політика панелі, і звичайного замовника сюди не пустило б. Перевірку входу
/// робимо самі в <see cref="OnActionExecuting"/> — і ведемо на публічну форму входу,
/// а не на службову сторінку адміністраторів.
/// </summary>
[AllowAnonymous]
public sealed partial class CabinetController : Controller
{
    /// <summary>Скільки замовлень на сторінці списку — як у макеті.</summary>
    private const int OrdersPerPage = 6;

    /// <summary>Скільки транзакцій на сторінці платежів.</summary>
    private const int PaymentsPerPage = 8;

    private readonly ICabinetService _cabinet;
    private readonly IExecutorCabinetService _exec;
    private readonly IExecutorInsightsService _insights;
    private readonly IPublicCatalogService _catalog;
    private readonly UserManager<ApplicationUser> _users;
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly ApplicationDbContext _db;
    private readonly CatalogPhotos _photos;
    private readonly ILogger<CabinetController> _logger;

    public CabinetController(
        ICabinetService cabinet,
        IExecutorCabinetService exec,
        IExecutorInsightsService insights,
        IPublicCatalogService catalog,
        UserManager<ApplicationUser> users,
        SignInManager<ApplicationUser> signIn,
        ApplicationDbContext db,
        CatalogPhotos photos,
        ILogger<CabinetController> logger)
    {
        _cabinet = cabinet;
        _exec = exec;
        _insights = insights;
        _catalog = catalog;
        _users = users;
        _signIn = signIn;
        _db = db;
        _photos = photos;
        _logger = logger;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            var back = Request.Path + Request.QueryString;
            context.Result = RedirectToAction("Login", "Auth", new { returnUrl = back.ToString() });
            return;
        }

        base.OnActionExecuting(context);
    }

    // ───────────────────────────────────────────────────────────────────────
    // Головна
    // ───────────────────────────────────────────────────────────────────────

    public async Task<IActionResult> Index(string? tab, CancellationToken cancellationToken = default)
    {
        if (CabinetRole.IsExecutor(HttpContext))
        {
            return await ExecutorIndexAsync(tab, cancellationToken);
        }

        var me = await MeAsync();
        var owner = await _cabinet.ResolveOwnerAsync(me.Id, cancellationToken);

        var model = new CabinetHomeViewModel
        {
            Active = "home",
            Title = "Особистий кабінет — Вибір",
            IsDemo = owner.IsDemo,
            FirstName = me.FirstName,
            Tab = Tab(tab, CabinetContent.HomeTabs, "active")
        };

        model.Summary = await _cabinet.GetSummaryAsync(owner.Id, cancellationToken);

        // Вкладку вибрали іншу — перепитуємо список під неї.
        if (model.Tab != "active")
        {
            var page = await _cabinet.GetOrdersAsync(owner.Id, OrderTab(model.Tab), string.Empty, 1, 3, cancellationToken);
            model.Summary.Orders = page.Items;
        }

        model.Summary.Recommended = (await _catalog.GetPopularServicesAsync(4, cancellationToken)).ToList();

        CabinetArtwork.Apply(model.Summary.Orders, _photos);
        CabinetArtwork.Apply(model.Summary.Threads);

        model.Unread = model.Summary.Threads.Sum(t => t.Unread);

        return View(model);
    }

    // ───────────────────────────────────────────────────────────────────────
    // Замовлення
    // ───────────────────────────────────────────────────────────────────────

    public async Task<IActionResult> Orders(
        string? tab, string? q, int page = 1, CancellationToken cancellationToken = default)
    {
        if (CabinetRole.IsExecutor(HttpContext))
        {
            return await ExecutorOrdersAsync(tab, q, page, cancellationToken);
        }

        var me = await MeAsync();
        var owner = await _cabinet.ResolveOwnerAsync(me.Id, cancellationToken);

        var model = new CabinetOrdersViewModel
        {
            Active = "orders",
            Title = "Мої замовлення — Вибір",
            IsDemo = owner.IsDemo,
            Tab = Tab(tab, CabinetContent.OrderTabs, "all"),
            Query = Clean(q),
            Unread = await BadgeAsync(me.Id, cancellationToken)
        };

        model.Orders = await _cabinet.GetOrdersAsync(
            owner.Id, OrderTab(model.Tab), model.Query, page, OrdersPerPage, cancellationToken);

        CabinetArtwork.Apply(model.Orders.Items, _photos);

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Order(int id, CancellationToken cancellationToken = default)
    {
        var me = await MeAsync();
        var owner = await _cabinet.ResolveOwnerAsync(me.Id, cancellationToken);

        var details = await _cabinet.GetOrderAsync(owner.Id, id, cancellationToken);

        if (details == null)
        {
            return NotFound();
        }

        CabinetArtwork.Apply(new[] { details.Order }, _photos);
        CabinetArtwork.Apply(details.Bids);

        var model = new CabinetOrderViewModel
        {
            Active = "orders",
            Title = details.Order.Title + " — Вибір",
            IsDemo = owner.IsDemo,
            Details = details,
            Unread = await BadgeAsync(me.Id, cancellationToken)
        };

        model.Facts.Add(("Категорія", details.Order.Category));
        model.Facts.Add(("Бюджет", CabinetFormat.Money(details.Order.Price)));

        if (details.Order.ExecutionAt.HasValue)
        {
            model.Facts.Add(("Термін виконання", "до " + CabinetFormat.LongDate(details.Order.ExecutionAt.Value)));
        }

        if (!string.IsNullOrWhiteSpace(details.Address))
        {
            model.Facts.Add(("Місце виконання", details.Address));
        }

        model.Facts.Add(("Статус", CabinetContent.StatusTitle(details.Order.Status, details.Order.ExecutorId != null)));
        model.Facts.Add(("Відгуків виконавців", details.Bids.Count.ToString()));

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? addressId, int? categoryId, CancellationToken cancellationToken = default)
    {
        if (CabinetRole.IsExecutor(HttpContext))
        {
            return RedirectToAction(nameof(Index));
        }

        var me = await MeAsync();

        var model = new CabinetCreateOrderViewModel
        {
            Active = "orders",
            Title = "Створити замовлення — Вибір",
            PriceFrom = 1000,
            PriceTo = 5000,
            AddressId = addressId,
            CategoryId = categoryId ?? 0,
            Place = addressId.HasValue ? "onsite" : "online",
            Unread = await BadgeAsync(me.Id, cancellationToken)
        };

        await FillCreateAsync(model, me.Id, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CabinetCreateOrderViewModel model, CancellationToken cancellationToken = default)
    {
        var me = await MeAsync();

        model.Active = "orders";
        model.Title = "Створити замовлення — Вибір";
        model.Unread = await BadgeAsync(me.Id, cancellationToken);

        var category = await _db.Categories
            .FirstOrDefaultAsync(c => c.Id == model.CategoryId && c.IsActive, cancellationToken);

        if (category == null)
        {
            ModelState.AddModelError(nameof(model.CategoryId), "Оберіть категорію зі списку");
        }

        if (model.Budget == "range" && model.PriceTo < model.PriceFrom)
        {
            ModelState.AddModelError(nameof(model.PriceTo), "Верхня межа не може бути меншою за нижню");
        }

        var address = model.AddressId.HasValue
            ? await _db.UserAddresses.FirstOrDefaultAsync(
                a => a.Id == model.AddressId && a.UserId == me.Id, cancellationToken)
            : null;

        if (model.Place == "onsite" && address == null)
        {
            ModelState.AddModelError(nameof(model.AddressId), "Оберіть адресу, куди має приїхати виконавець");
        }

        if (!ModelState.IsValid)
        {
            await FillCreateAsync(model, me.Id, cancellationToken);
            return View(model);
        }

        var order = new Order
        {
            Title = model.OrderTitle.Trim(),
            Description = model.Description.Trim(),
            Price = model.Budget == "range" ? model.PriceTo : model.PriceFrom,
            CategoryId = category!.Id,
            Address = address == null
                ? "Онлайн"
                : $"{address.Line}, {address.City}",
            ExecutionAt = Deadline(model.Deadline)
        };

        await _cabinet.CreateOrderAsync(me.Id, order, cancellationToken);

        TempData["Cabinet.Flash"] = "Замовлення опубліковано. Виконавці побачать його в каталозі.";
        return RedirectToAction(nameof(Order), new { id = order.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Bid(int id, int orderId, bool accept, CancellationToken cancellationToken = default)
    {
        var me = await MeAsync();
        var done = await _cabinet.AnswerBidAsync(me.Id, id, accept, cancellationToken);

        TempData["Cabinet.Flash"] = done
            ? accept ? "Виконавця підтверджено." : "Відгук відхилено."
            : "Не вдалося змінити відгук.";

        return RedirectToAction(nameof(Order), new { id = orderId });
    }

    // ───────────────────────────────────────────────────────────────────────
    // Повідомлення
    // ───────────────────────────────────────────────────────────────────────

    public async Task<IActionResult> Messages(
        int? orderId, string? filter, string? q, CancellationToken cancellationToken = default)
    {
        if (CabinetRole.IsExecutor(HttpContext))
        {
            return await ExecutorMessagesAsync(orderId, filter, q, cancellationToken);
        }

        var me = await MeAsync();
        var owner = await _cabinet.ResolveOwnerAsync(me.Id, cancellationToken);

        var model = new CabinetMessagesViewModel
        {
            Active = "messages",
            Title = "Повідомлення — Вибір",
            IsDemo = owner.IsDemo,
            Filter = filter is "unread" or "archive" ? filter : "all",
            Query = Clean(q)
        };

        var threadFilter = model.Filter switch
        {
            "unread" => CabinetThreadFilter.Unread,
            "archive" => CabinetThreadFilter.Archive,
            _ => CabinetThreadFilter.All
        };

        model.Threads = await _cabinet.GetThreadsAsync(
            owner.Id, threadFilter, model.Query, CabinetSide.Customer, cancellationToken);
        CabinetArtwork.Apply(model.Threads);

        var selected = orderId ?? model.Threads.FirstOrDefault()?.OrderId;

        if (selected.HasValue)
        {
            model.Chat = await _cabinet.GetChatAsync(
                owner.Id, selected.Value, CabinetSide.Customer, cancellationToken);

            if (model.Chat != null)
            {
                CabinetArtwork.Apply(new[] { model.Chat.Head });

                foreach (var message in model.Chat.Messages)
                {
                    message.Portrait = message.Mine
                        ? CabinetArtwork.Portrait(me.FirstName + " " + me.LastName, me.Id)
                        : model.Chat.Head.PartnerPortrait;
                }
            }
        }

        model.Unread = await BadgeAsync(me.Id, cancellationToken);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(int orderId, string? text, CancellationToken cancellationToken = default)
    {
        var me = await MeAsync();
        var side = CabinetRole.Current(HttpContext);
        var sent = await _cabinet.SendMessageAsync(me.Id, orderId, text ?? string.Empty, side, cancellationToken);

        if (!sent)
        {
            TempData["Cabinet.Flash"] = "Повідомлення не надіслано: порожній текст або чуже замовлення.";
        }

        return RedirectToAction(nameof(Messages), new { orderId });
    }

    // ───────────────────────────────────────────────────────────────────────
    // Обране
    // ───────────────────────────────────────────────────────────────────────

    public async Task<IActionResult> Favorites(
        string? q, string? group, string? tab, CancellationToken cancellationToken = default)
    {
        if (CabinetRole.IsExecutor(HttpContext))
        {
            return RedirectToAction(nameof(Index));
        }

        var me = await MeAsync();
        var owner = await _cabinet.ResolveOwnerAsync(me.Id, cancellationToken);

        var model = new CabinetFavoritesViewModel
        {
            Active = "favorites",
            Title = "Обране — Вибір",
            IsDemo = owner.IsDemo,
            Query = Clean(q),
            Group = Clean(group),
            Tab = tab == "services" ? "services" : "executors",
            Unread = await BadgeAsync(me.Id, cancellationToken)
        };

        model.Data = await _cabinet.GetFavoritesAsync(owner.Id, model.Query, model.Group, cancellationToken);
        CabinetArtwork.Apply(model.Data.Items);

        if (model.Tab == "services")
        {
            var seen = RecentViews.Read(Request);
            var services = await _catalog.GetServicesByIdsAsync(seen, cancellationToken);

            if (services.Count == 0)
            {
                services = await _catalog.GetMostOrderedServicesAsync(6, cancellationToken);
            }

            model.Services = services.ToList();
        }

        return View(model);
    }

    // ───────────────────────────────────────────────────────────────────────
    // Адреси
    // ───────────────────────────────────────────────────────────────────────

    public async Task<IActionResult> Addresses(int? edit, bool add = false, CancellationToken cancellationToken = default)
    {
        if (CabinetRole.IsExecutor(HttpContext))
        {
            return RedirectToAction(nameof(Index));
        }

        var me = await MeAsync();

        var model = new CabinetAddressesViewModel
        {
            Active = "addresses",
            Title = "Мої адреси — Вибір",
            Addresses = await _cabinet.GetAddressesAsync(me.Id, cancellationToken),
            Unread = await BadgeAsync(me.Id, cancellationToken)
        };

        if (edit.HasValue)
        {
            var address = model.Addresses.FirstOrDefault(a => a.Id == edit.Value);

            if (address != null)
            {
                model.Form = ToForm(address);
            }
        }
        else if (add)
        {
            model.Form = new CabinetAddressForm { IsPrimary = model.Addresses.Count == 0 };
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAddress(CabinetAddressForm form, CancellationToken cancellationToken = default)
    {
        var me = await MeAsync();

        if (!ModelState.IsValid)
        {
            var model = new CabinetAddressesViewModel
            {
                Active = "addresses",
                Title = "Мої адреси — Вибір",
                Addresses = await _cabinet.GetAddressesAsync(me.Id, cancellationToken),
                Form = form,
                Unread = await BadgeAsync(me.Id, cancellationToken)
            };

            return View(nameof(Addresses), model);
        }

        var address = form.Id > 0
            ? await _db.UserAddresses.FirstOrDefaultAsync(a => a.Id == form.Id && a.UserId == me.Id, cancellationToken)
            : null;

        if (form.Id > 0 && address == null)
        {
            return NotFound();
        }

        if (address == null)
        {
            address = new UserAddress { UserId = me.Id };
            _db.UserAddresses.Add(address);
        }

        address.Title = form.Title.Trim();
        address.Kind = Enum.TryParse<AddressKind>(form.Kind, out var kind) ? kind : AddressKind.Home;
        address.Line = form.Line.Trim();
        address.City = form.City.Trim();
        address.Region = Empty(form.Region);
        address.PostalCode = Empty(form.PostalCode);
        address.Note = Empty(form.Note);
        address.IsPrimary = form.IsPrimary;

        if (form.IsPrimary)
        {
            // Основна адреса одна: решту знімаємо з позначки.
            var others = await _db.UserAddresses
                .Where(a => a.UserId == me.Id && a.Id != address.Id)
                .ToListAsync(cancellationToken);

            foreach (var other in others)
            {
                other.IsPrimary = false;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        TempData["Cabinet.Flash"] = form.Id > 0 ? "Адресу оновлено." : "Адресу додано.";
        return RedirectToAction(nameof(Addresses));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAddress(int id, CancellationToken cancellationToken = default)
    {
        var me = await MeAsync();

        var address = await _db.UserAddresses
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == me.Id, cancellationToken);

        if (address != null)
        {
            _db.UserAddresses.Remove(address);
            await _db.SaveChangesAsync(cancellationToken);
            TempData["Cabinet.Flash"] = "Адресу видалено.";
        }

        return RedirectToAction(nameof(Addresses));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UseAddress(int id, CancellationToken cancellationToken = default)
    {
        var me = await MeAsync();

        var address = await _db.UserAddresses
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == me.Id, cancellationToken);

        if (address == null)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Create), new { addressId = id });
    }

    // ───────────────────────────────────────────────────────────────────────
    // Платежі
    // ───────────────────────────────────────────────────────────────────────

    public async Task<IActionResult> Payments(
        string? type, string? q, string? tab, int page = 1, CancellationToken cancellationToken = default)
    {
        if (CabinetRole.IsExecutor(HttpContext))
        {
            return RedirectToAction(nameof(Index));
        }

        var me = await MeAsync();
        var owner = await _cabinet.ResolveOwnerAsync(me.Id, cancellationToken);

        var model = new CabinetPaymentsViewModel
        {
            Active = "payments",
            Title = "Платежі та рахунки — Вибір",
            IsDemo = owner.IsDemo,
            Type = type is "payment" or "refund" ? type : "all",
            Query = Clean(q),
            Tab = tab == "invoices" ? "invoices" : "transactions",
            Unread = await BadgeAsync(me.Id, cancellationToken)
        };

        model.Wallet = await _cabinet.GetWalletAsync(owner.Id, cancellationToken);

        var kind = model.Type switch
        {
            "payment" => CabinetPaymentType.Payment,
            "refund" => CabinetPaymentType.Refund,
            _ => CabinetPaymentType.All
        };

        model.Transactions = await _cabinet.GetTransactionsAsync(
            owner.Id, kind, model.Query, page, PaymentsPerPage, cancellationToken);

        return View(model);
    }

    // ───────────────────────────────────────────────────────────────────────
    // Відгуки
    // ───────────────────────────────────────────────────────────────────────

    public async Task<IActionResult> Reviews(
        string? tab, string? sort, int page = 1, CancellationToken cancellationToken = default)
    {
        if (CabinetRole.IsExecutor(HttpContext))
        {
            return await ExecutorReviewsAsync(tab, sort, page, cancellationToken);
        }

        var me = await MeAsync();
        var owner = await _cabinet.ResolveOwnerAsync(me.Id, cancellationToken);

        var model = new CabinetReviewsViewModel
        {
            Active = "reviews",
            Title = "Відгуки — Вибір",
            IsDemo = owner.IsDemo,
            Reviews = await _cabinet.GetReviewsAsync(owner.Id, cancellationToken),
            Unread = await BadgeAsync(me.Id, cancellationToken)
        };

        CabinetArtwork.Apply(model.Reviews);

        var completed = await _cabinet.GetOrdersAsync(
            owner.Id, CabinetOrderTab.Completed, string.Empty, 1, 20, cancellationToken);

        var written = model.Reviews.Select(r => r.OrderId).ToHashSet();

        model.Awaiting = completed.Items
            .Where(o => !written.Contains(o.Id))
            .Take(4)
            .ToList();

        CabinetArtwork.Apply(model.Awaiting, _photos);

        return View(model);
    }

    // ───────────────────────────────────────────────────────────────────────
    // Підтримка
    // ───────────────────────────────────────────────────────────────────────

    public async Task<IActionResult> Support(string? q, CancellationToken cancellationToken = default)
    {
        if (CabinetRole.IsExecutor(HttpContext))
        {
            return await ExecutorSupportAsync(q, null, cancellationToken);
        }

        var me = await MeAsync();

        var model = new CabinetSupportViewModel
        {
            Active = "support",
            Title = "Підтримка — Вибір",
            Query = Clean(q),
            Unread = await BadgeAsync(me.Id, cancellationToken)
        };

        model.Faq = PageContent.Faq
            .Where(item => model.Query.Length == 0
                           || item.Question.Contains(model.Query, StringComparison.OrdinalIgnoreCase)
                           || item.Answer.Contains(model.Query, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return View(model);
    }

    // ───────────────────────────────────────────────────────────────────────
    // Налаштування
    // ───────────────────────────────────────────────────────────────────────

    public async Task<IActionResult> Settings(CancellationToken cancellationToken = default)
    {
        if (CabinetRole.IsExecutor(HttpContext))
        {
            return await ExecutorSettingsAsync(cancellationToken);
        }

        var me = await MeAsync();

        var model = ToSettings(me);
        model.Unread = await BadgeAsync(me.Id, cancellationToken);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Settings(CabinetSettingsViewModel model, CancellationToken cancellationToken = default)
    {
        var me = await MeAsync();

        model.Active = "settings";
        model.Title = "Налаштування — Вибір";
        model.Avatar = me.AvatarUrl;
        model.Unread = await BadgeAsync(me.Id, cancellationToken);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        me.FirstName = model.FirstName.Trim();
        me.LastName = model.LastName.Trim();
        me.FullName = $"{me.FirstName} {me.LastName}";
        me.PhoneNumber = Empty(model.Phone);
        me.TwoFactorEnabled = model.TwoFactor;

        me.NotifyByEmail = model.NotifyByEmail;
        me.NotifyByPush = model.NotifyByPush;
        me.NotifyBySms = model.NotifyBySms;
        me.NotifyMarketing = model.NotifyMarketing;

        me.ProfileVisibility = Pick(CabinetContent.Visibility, model.ProfileVisibility, "Registered");
        me.ShowOnlineStatus = model.ShowOnlineStatus;
        me.ShareOrderHistory = model.ShareOrderHistory;

        me.Currency = Pick(CabinetContent.Currencies, model.Currency, "UAH");
        me.Language = Pick(CabinetContent.Languages, model.Language, "uk");
        me.TimeZoneName = Pick(CabinetContent.TimeZones, model.TimeZoneName, "Europe/Kyiv");
        me.DateFormat = Pick(CabinetContent.DateFormats, model.DateFormat, "dd.MM.yyyy");
        me.TimeFormat = Pick(CabinetContent.TimeFormats, model.TimeFormat, "24");
        me.Theme = model.Theme == "dark" ? "dark" : "light";
        me.UpdatedAt = DateTime.UtcNow;

        var result = await _users.UpdateAsync(me);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            _logger.LogWarning("Не вдалося зберегти налаштування користувача {UserId}", me.Id);
            return View(model);
        }

        TempData["Cabinet.Flash"] = "Зміни збережено.";
        return RedirectToAction(nameof(Settings));
    }

    // ───────────────────────────────────────────────────────────────────────
    // Допоміжне
    // ───────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Лічильник на бейджі «Повідомлення». Рахуємо по тому, чиї дані показує кабінет,
    /// щоб у демонстраційному режимі число в меню збігалося зі списком листувань.
    /// </summary>
    private async Task<int> BadgeAsync(string userId, CancellationToken cancellationToken)
    {
        if (CabinetRole.IsExecutor(HttpContext))
        {
            return await _cabinet.GetUnreadCountAsync(userId, CabinetSide.Executor, cancellationToken);
        }

        var owner = await _cabinet.ResolveOwnerAsync(userId, cancellationToken);
        return await _cabinet.GetUnreadCountAsync(owner.Id, CabinetSide.Customer, cancellationToken);
    }

    private async Task<ApplicationUser> MeAsync() =>
        await _users.GetUserAsync(User) ?? throw new InvalidOperationException("Увійшов користувач, якого немає в базі");

    private async Task FillCreateAsync(CabinetCreateOrderViewModel model, string userId, CancellationToken cancellationToken)
    {
        model.Addresses = await _cabinet.GetAddressesAsync(userId, cancellationToken);

        // У списку — тільки кінцеві послуги: замовлення вішається на підкатегорію,
        // а не на напрям, інакше виконавцям його не знайти у своєму розділі.
        var rows = await _db.Categories
            .Where(c => c.IsActive && c.ParentCategoryId != null)
            .OrderBy(c => c.ParentCategory!.Name)
            .ThenBy(c => c.Name)
            .Select(c => new { c.Id, c.Name, Parent = c.ParentCategory!.Name })
            .ToListAsync(cancellationToken);

        model.Categories = rows
            .Select(r => new Choice(r.Id.ToString(), $"{r.Parent} › {r.Name}"))
            .ToList();
    }

    private CabinetSettingsViewModel ToSettings(ApplicationUser user) => new()
    {
        Active = "settings",
        Title = "Налаштування — Вибір",
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email ?? string.Empty,
        Phone = user.PhoneNumber,
        Avatar = user.AvatarUrl,
        TwoFactor = user.TwoFactorEnabled,
        NotifyByEmail = user.NotifyByEmail,
        NotifyByPush = user.NotifyByPush,
        NotifyBySms = user.NotifyBySms,
        NotifyMarketing = user.NotifyMarketing,
        ProfileVisibility = user.ProfileVisibility ?? "Registered",
        ShowOnlineStatus = user.ShowOnlineStatus,
        ShareOrderHistory = user.ShareOrderHistory,
        PaymentCard = user.PaymentCard ?? string.Empty,
        Currency = user.Currency ?? "UAH",
        Language = user.Language ?? "uk",
        TimeZoneName = user.TimeZoneName ?? "Europe/Kyiv",
        DateFormat = user.DateFormat ?? "dd.MM.yyyy",
        TimeFormat = user.TimeFormat ?? "24",
        Theme = user.Theme ?? "light"
    };

    private static CabinetAddressForm ToForm(UserAddress address) => new()
    {
        Id = address.Id,
        Title = address.Title,
        Kind = address.Kind.ToString(),
        Line = address.Line,
        City = address.City,
        Region = address.Region,
        PostalCode = address.PostalCode,
        Note = address.Note,
        IsPrimary = address.IsPrimary
    };

    private static DateTime? Deadline(string key) => key switch
    {
        "asap" => DateTime.UtcNow.AddDays(1),
        "3" => DateTime.UtcNow.AddDays(3),
        "7" => DateTime.UtcNow.AddDays(7),
        "30" => DateTime.UtcNow.AddDays(30),
        _ => null
    };

    private static CabinetOrderTab OrderTab(string key) => key switch
    {
        "active" => CabinetOrderTab.Active,
        "progress" => CabinetOrderTab.InProgress,
        "completed" => CabinetOrderTab.Completed,
        "cancelled" => CabinetOrderTab.Cancelled,
        _ => CabinetOrderTab.All
    };

    private static string Tab(string? value, Choice[] allowed, string fallback) =>
        allowed.Any(c => c.Key == value) ? value! : fallback;

    private static string Pick(Choice[] allowed, string? value, string fallback) =>
        allowed.Any(c => string.Equals(c.Key, value, StringComparison.OrdinalIgnoreCase)) ? value! : fallback;

    private static string Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

    private static string? Empty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
