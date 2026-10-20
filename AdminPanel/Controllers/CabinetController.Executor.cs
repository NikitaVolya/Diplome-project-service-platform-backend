using AdminPanel.Localization;
using AdminPanel.Models;
using BLL.Public;
using Domain.Common;
using Domain.Entities;
using Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace AdminPanel.Controllers;

/// <summary>
/// Екрани кабінету з боку виконавця.
///
/// Лежать у тому самому контролері й за тими самими адресами /Cabinet/..., що й екрани
/// замовника: людина натискає одну кнопку «Профіль» у шапці, а що саме відкриється —
/// вирішує її роль (див. <see cref="CabinetRole"/>). Винесено в окремий файл частковим
/// класом, щоб два набори екранів не перетворили контролер на суцільне полотно.
/// </summary>
public sealed partial class CabinetController
{
    /// <summary>Скільки замовлень на сторінці списку виконавця.</summary>
    private const int ExecOrdersPerPage = 6;

    private const int ExecServicesPerPage = 7;
    private const int ExecReviewsPerPage = 5;
    private const int ExecWorksPerPage = 16;

    // ───────────────────────────────────────────────────────────────────────
    // Головна
    // ───────────────────────────────────────────────────────────────────────

    private async Task<IActionResult> ExecutorIndexAsync(string? tab, CancellationToken cancellationToken)
    {
        var me = await MeAsync();

        var model = new ExecHomeViewModel
        {
            Active = "home",
            Title = Tr.T("Кабінет виконавця — Вибір"),
            FullName = $"{me.FirstName} {me.LastName}".Trim(),
            Headline = me.Headline ?? Tr.T("Виконавець"),
            City = me.City ?? string.Empty,
            Avatar = string.IsNullOrEmpty(me.AvatarUrl)
                ? CabinetArtwork.Portrait($"{me.FirstName} {me.LastName}", me.Id)
                : me.AvatarUrl,
            Tab = Tab(tab, ExecutorContent.HomeTabs, "fresh")
        };

        await DressAsync(model, me, cancellationToken);

        model.Summary = await _exec.GetSummaryAsync(me.Id, cancellationToken);

        // Вкладку вибрали іншу — перепитуємо список під неї.
        if (model.Tab != "fresh")
        {
            var page = await _exec.GetOrdersAsync(me.Id, ExecTab(model.Tab), string.Empty, 1, 3, cancellationToken);
            model.Summary.Orders = page.Items;
        }

        var marks = await _exec.GetReviewsAsync(me.Id, ExecReviewTab.Received, "new", 1, 1, cancellationToken);
        model.Rating = marks.Average;
        model.Reviews = marks.Total;

        ExecutorArtwork.Apply(model.Summary.Orders, _photos);
        ExecutorArtwork.Apply(model.Summary.Services, _photos);
        ExecutorArtwork.Apply(model.Summary.Works, _photos);

        return View("Executor/Index", model);
    }

    // ───────────────────────────────────────────────────────────────────────
    // Замовлення
    // ───────────────────────────────────────────────────────────────────────

    private async Task<IActionResult> ExecutorOrdersAsync(
        string? tab, string? q, int page, CancellationToken cancellationToken)
    {
        var me = await MeAsync();

        var model = new ExecOrdersViewModel
        {
            Active = "orders",
            Title = Tr.T("Замовлення — Вибір"),
            Tab = Tab(tab, ExecutorContent.OrderTabs, "all"),
            Query = Clean(q)
        };

        await DressAsync(model, me, cancellationToken);

        model.Counters = await _exec.GetCountersAsync(me.Id, cancellationToken);
        model.Orders = await _exec.GetOrdersAsync(
            me.Id, ExecTab(model.Tab), model.Query, page, ExecOrdersPerPage, cancellationToken);

        ExecutorArtwork.Apply(model.Orders.Items, _photos);

        return View("Executor/Orders", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AnswerOrder(
        int id, bool accept, string? tab, CancellationToken cancellationToken = default)
    {
        var me = await MeAsync();
        var done = await _exec.AnswerOrderAsync(me.Id, id, accept, cancellationToken);

        TempData["Cabinet.Flash"] = done
            ? accept
                ? Tr.T("Замовлення взято в роботу.")
                : Tr.T("Замовлення відхилено й повернулося в пошук.")
            : Tr.T("Не вдалося змінити замовлення: можливо, його вже забрали.");

        return RedirectToAction(nameof(Orders), new { tab });
    }

    // ───────────────────────────────────────────────────────────────────────
    // Мої послуги
    // ───────────────────────────────────────────────────────────────────────

    public async Task<IActionResult> Services(
        string? q, int categoryId = 0, int page = 1, int? edit = null, bool add = false,
        CancellationToken cancellationToken = default)
    {
        if (!CabinetRole.IsExecutor(HttpContext))
        {
            return RedirectToAction(nameof(Index));
        }

        var me = await MeAsync();

        var model = new ExecServicesViewModel
        {
            Active = "services",
            Title = Tr.T("Мої послуги — Вибір"),
            Query = Clean(q),
            CategoryId = categoryId
        };

        await DressAsync(model, me, cancellationToken);
        await FillServiceFormAsync(model, cancellationToken);

        model.Services = await _exec.GetServicesAsync(
            me.Id, model.Query, categoryId, page, ExecServicesPerPage, cancellationToken);

        ExecutorArtwork.Apply(model.Services.Items, _photos);

        if (edit.HasValue)
        {
            var service = await _exec.GetServiceAsync(me.Id, edit.Value, cancellationToken);

            if (service != null)
            {
                model.Form = new ExecServiceForm
                {
                    Id = service.Id,
                    Title = service.Title,
                    CategoryId = service.CategoryId,
                    Description = service.Description,
                    FromPrice = service.FromPrice,
                    Image = service.Image,
                    Publish = service.State != ServiceState.Draft
                };
            }
        }
        else if (add)
        {
            model.Form = new ExecServiceForm();
        }

        return View("Executor/Services", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveService(ExecServiceForm form, CancellationToken cancellationToken = default)
    {
        var me = await MeAsync();

        if (!ModelState.IsValid)
        {
            var model = new ExecServicesViewModel
            {
                Active = "services",
                Title = Tr.T("Мої послуги — Вибір"),
                Form = form
            };

            await DressAsync(model, me, cancellationToken);
            await FillServiceFormAsync(model, cancellationToken);

            model.Services = await _exec.GetServicesAsync(me.Id, string.Empty, 0, 1, ExecServicesPerPage, cancellationToken);
            ExecutorArtwork.Apply(model.Services.Items, _photos);

            return View("Executor/Services", model);
        }

        var service = form.Id > 0
            ? await _exec.GetServiceAsync(me.Id, form.Id, cancellationToken)
            : new ExecutorService();

        if (service == null)
        {
            return NotFound();
        }

        service.Title = form.Title.Trim();
        service.CategoryId = form.CategoryId;
        service.Description = Empty(form.Description);
        service.FromPrice = form.FromPrice;
        service.Image = Empty(form.Image);

        // Чернетку публікують прапорцем; активну послугу він не знімає з паузи —
        // для цього є окремий перемикач на картці.
        if (form.Publish && service.State == ServiceState.Draft)
        {
            service.State = ServiceState.Active;
        }
        else if (!form.Publish)
        {
            service.State = ServiceState.Draft;
        }

        await _exec.SaveServiceAsync(me.Id, service, cancellationToken);

        TempData["Cabinet.Flash"] = form.Id > 0 ? Tr.T("Послугу оновлено.") : Tr.T("Послугу додано.");
        return RedirectToAction(nameof(Services));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleService(int id, CancellationToken cancellationToken = default)
    {
        var me = await MeAsync();
        var done = await _exec.ToggleServiceAsync(me.Id, id, cancellationToken);

        if (done)
        {
            TempData["Cabinet.Flash"] = Tr.T("Стан послуги змінено.");
        }

        return RedirectToAction(nameof(Services));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteService(int id, CancellationToken cancellationToken = default)
    {
        var me = await MeAsync();

        if (await _exec.DeleteServiceAsync(me.Id, id, cancellationToken))
        {
            TempData["Cabinet.Flash"] = Tr.T("Послугу видалено.");
        }

        return RedirectToAction(nameof(Services));
    }

    // ───────────────────────────────────────────────────────────────────────
    // Календар
    // ───────────────────────────────────────────────────────────────────────

    public async Task<IActionResult> Calendar(
        string? week, string? q, bool add = false, CancellationToken cancellationToken = default)
    {
        if (!CabinetRole.IsExecutor(HttpContext))
        {
            return RedirectToAction(nameof(Index));
        }

        var me = await MeAsync();
        var day = DateTime.TryParse(week, out var parsed) ? parsed : DateTime.UtcNow;

        var model = new ExecCalendarViewModel
        {
            Active = "calendar",
            Title = Tr.T("Календар — Вибір"),
            Query = Clean(q)
        };

        await DressAsync(model, me, cancellationToken);

        model.Calendar = await _exec.GetCalendarAsync(me.Id, day, model.Query, cancellationToken);
        model.Week = model.Calendar.Monday.ToString("yyyy-MM-dd");
        model.Previous = model.Calendar.Monday.AddDays(-7).ToString("yyyy-MM-dd");
        model.Next = model.Calendar.Monday.AddDays(7).ToString("yyyy-MM-dd");

        if (add)
        {
            model.Form = new ExecEventForm { Date = DateTime.UtcNow.ToString("yyyy-MM-dd") };
        }

        return View("Executor/Calendar", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddEvent(ExecEventForm form, CancellationToken cancellationToken = default)
    {
        var me = await MeAsync();

        if (!DateTime.TryParse(form.Date, out var date))
        {
            ModelState.AddModelError(nameof(form.Date), Tr.T("Вкажіть дату у форматі РРРР-ММ-ДД"));
        }

        if (!ModelState.IsValid)
        {
            var model = new ExecCalendarViewModel
            {
                Active = "calendar",
                Title = Tr.T("Календар — Вибір"),
                Form = form
            };

            await DressAsync(model, me, cancellationToken);

            model.Calendar = await _exec.GetCalendarAsync(me.Id, DateTime.UtcNow, string.Empty, cancellationToken);
            model.Week = model.Calendar.Monday.ToString("yyyy-MM-dd");
            model.Previous = model.Calendar.Monday.AddDays(-7).ToString("yyyy-MM-dd");
            model.Next = model.Calendar.Monday.AddDays(7).ToString("yyyy-MM-dd");

            return View("Executor/Calendar", model);
        }

        var start = date.Date.AddHours(Math.Clamp(form.Hour, 0, 23));

        await _exec.AddEventAsync(me.Id, new CalendarEvent
        {
            Title = form.Title.Trim(),
            Kind = Enum.TryParse<EventKind>(form.Kind, out var kind) ? kind : EventKind.Meeting,
            StartAt = start,
            EndAt = start.AddHours(Math.Clamp(form.Hours, 1, 12)),
            PartnerName = Empty(form.PartnerName),
            Note = Empty(form.Note)
        }, cancellationToken);

        TempData["Cabinet.Flash"] = Tr.T("Подію додано в календар.");
        return RedirectToAction(nameof(Calendar), new { week = start.ToString("yyyy-MM-dd") });
    }

    // ───────────────────────────────────────────────────────────────────────
    // Портфоліо
    // ───────────────────────────────────────────────────────────────────────

    public async Task<IActionResult> Portfolio(
        string? group, int page = 1, bool add = false, CancellationToken cancellationToken = default)
    {
        if (!CabinetRole.IsExecutor(HttpContext))
        {
            return RedirectToAction(nameof(Index));
        }

        var me = await MeAsync();

        var model = new ExecPortfolioViewModel
        {
            Active = "portfolio",
            Title = Tr.T("Портфоліо — Вибір"),
            Group = Clean(group)
        };

        await DressAsync(model, me, cancellationToken);

        model.Portfolio = await _exec.GetPortfolioAsync(me.Id, model.Group, page, ExecWorksPerPage, cancellationToken);
        ExecutorArtwork.Apply(model.Portfolio.Items, _photos);

        if (add)
        {
            model.Form = new ExecWorkForm();
        }

        return View("Executor/Portfolio", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddWork(ExecWorkForm form, CancellationToken cancellationToken = default)
    {
        var me = await MeAsync();

        if (!ModelState.IsValid)
        {
            var model = new ExecPortfolioViewModel
            {
                Active = "portfolio",
                Title = Tr.T("Портфоліо — Вибір"),
                Form = form
            };

            await DressAsync(model, me, cancellationToken);

            model.Portfolio = await _exec.GetPortfolioAsync(me.Id, string.Empty, 1, ExecWorksPerPage, cancellationToken);
            ExecutorArtwork.Apply(model.Portfolio.Items, _photos);

            return View("Executor/Portfolio", model);
        }

        await _exec.AddWorkAsync(me.Id, new PortfolioWork
        {
            Title = form.Title.Trim(),
            Group = form.Group.Trim(),
            Image = Empty(form.Image),
            Link = Empty(form.Link)
        }, cancellationToken);

        TempData["Cabinet.Flash"] =
            Tr.F("Роботу додано. Покладіть знімок у {0}.", ExecutorArtwork.Expected(ExecutorArtwork.PortfolioFolder, form.Image ?? form.Title));

        return RedirectToAction(nameof(Portfolio));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteWork(int id, CancellationToken cancellationToken = default)
    {
        var me = await MeAsync();

        if (await _exec.DeleteWorkAsync(me.Id, id, cancellationToken))
        {
            TempData["Cabinet.Flash"] = Tr.T("Роботу прибрано з портфоліо.");
        }

        return RedirectToAction(nameof(Portfolio));
    }

    // ───────────────────────────────────────────────────────────────────────
    // Повідомлення та відгуки
    // ───────────────────────────────────────────────────────────────────────

    private async Task<IActionResult> ExecutorMessagesAsync(
        int? orderId, string? filter, string? q, CancellationToken cancellationToken)
    {
        var me = await MeAsync();

        var model = new ExecMessagesViewModel
        {
            Active = "messages",
            Title = Tr.T("Повідомлення — Вибір"),
            Filter = filter is "unread" or "archive" ? filter : "all",
            Query = Clean(q)
        };

        await DressAsync(model, me, cancellationToken);

        var threadFilter = model.Filter switch
        {
            "unread" => CabinetThreadFilter.Unread,
            "archive" => CabinetThreadFilter.Archive,
            _ => CabinetThreadFilter.All
        };

        model.Threads = await _cabinet.GetThreadsAsync(
            me.Id, threadFilter, model.Query, CabinetSide.Executor, cancellationToken);

        CabinetArtwork.Apply(model.Threads);

        var all = await _cabinet.GetThreadsAsync(
            me.Id, CabinetThreadFilter.All, string.Empty, CabinetSide.Executor, cancellationToken);

        model.TotalThreads = all.Count;
        model.UnreadThreads = all.Count(t => t.Unread > 0);

        var selected = orderId ?? model.Threads.FirstOrDefault()?.OrderId;

        if (selected.HasValue)
        {
            model.Chat = await _cabinet.GetChatAsync(me.Id, selected.Value, CabinetSide.Executor, cancellationToken);

            if (model.Chat != null)
            {
                CabinetArtwork.Apply(new[] { model.Chat.Head });

                foreach (var message in model.Chat.Messages)
                {
                    message.Portrait = message.Mine
                        ? CabinetArtwork.Portrait($"{me.FirstName} {me.LastName}", me.Id)
                        : model.Chat.Head.PartnerPortrait;
                }
            }
        }

        return View("Executor/Messages", model);
    }

    private async Task<IActionResult> ExecutorReviewsAsync(
        string? tab, string? sort, int page, CancellationToken cancellationToken)
    {
        var me = await MeAsync();

        var model = new ExecReviewsViewModel
        {
            Active = "reviews",
            Title = Tr.T("Відгуки — Вибір"),
            Tab = Tab(tab, ExecutorContent.ReviewTabs, "received"),
            Sort = Tab(sort, ExecutorContent.ReviewSorts, "new")
        };

        await DressAsync(model, me, cancellationToken);

        var which = model.Tab switch
        {
            "given" => ExecReviewTab.Given,
            "torate" => ExecReviewTab.ToRate,
            _ => ExecReviewTab.Received
        };

        model.Reviews = await _exec.GetReviewsAsync(
            me.Id, which, model.Sort, page, ExecReviewsPerPage, cancellationToken);

        ExecutorArtwork.Apply(model.Reviews.Items, _photos);
        ExecutorArtwork.Apply(model.Reviews.Awaiting, _photos);

        return View("Executor/Reviews", model);
    }

    // ───────────────────────────────────────────────────────────────────────
    // Аналітика
    // ───────────────────────────────────────────────────────────────────────

    public async Task<IActionResult> Analytics(int? days, CancellationToken cancellationToken = default)
    {
        if (!CabinetRole.IsExecutor(HttpContext))
        {
            return RedirectToAction(nameof(Index));
        }

        var me = await MeAsync();

        var model = new ExecAnalyticsViewModel
        {
            Active = "analytics",
            Title = Tr.T("Аналітика — Вибір"),
            Days = Period(days)
        };

        model.Data = await _insights.GetAnalyticsAsync(me.Id, model.Days, cancellationToken);

        await DressAsync(model, me, cancellationToken);

        return View("Executor/Analytics", model);
    }

    // ───────────────────────────────────────────────────────────────────────
    // Фінанси
    // ───────────────────────────────────────────────────────────────────────

    public async Task<IActionResult> Finance(
        int? days, string? open, int? id, CancellationToken cancellationToken = default)
    {
        if (!CabinetRole.IsExecutor(HttpContext))
        {
            return RedirectToAction(nameof(Index));
        }

        var me = await MeAsync();

        var model = new ExecFinanceViewModel
        {
            Active = "finance",
            Title = Tr.T("Фінанси — Вибір"),
            Days = Period(days),
            Open = open is "method" or "pay" ? open : null
        };

        if (model.Open == "method")
        {
            // З кнопки «Змінити» приходить id — тоді форма відкривається заповненою.
            var known = id is > 0
                ? await _insights.GetMethodAsync(me.Id, id.Value, cancellationToken)
                : null;

            model.Form = new ExecMethodForm
            {
                Id = known?.Id ?? 0,
                Kind = (known?.Kind ?? PayoutKind.Card).ToString(),
                Label = known?.Label ?? string.Empty,
                Note = known?.Note
            };
        }

        await FillFinanceAsync(model, me, cancellationToken);

        return View("Executor/Finance", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Withdraw(ExecWithdrawForm form, CancellationToken cancellationToken = default)
    {
        if (!CabinetRole.IsExecutor(HttpContext))
        {
            return RedirectToAction(nameof(Index));
        }

        var me = await MeAsync();

        var done = await _insights.WithdrawAsync(me.Id, form.Amount, form.MethodId, cancellationToken);

        TempData["Cabinet.Flash"] = done
            ? Tr.F("Заявку на {0} прийнято. Гроші йдуть 1–3 робочі дні.", CabinetFormat.Money(form.Amount))
            : Tr.T("Не вдалося замовити виплату: перевірте суму й спосіб виведення.");

        return RedirectToAction(nameof(Finance));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMethod(ExecMethodForm form, CancellationToken cancellationToken = default)
    {
        if (!CabinetRole.IsExecutor(HttpContext))
        {
            return RedirectToAction(nameof(Index));
        }

        var me = await MeAsync();

        if (!ModelState.IsValid)
        {
            var model = new ExecFinanceViewModel
            {
                Active = "finance",
                Title = Tr.T("Фінанси — Вибір"),
                Days = 30,
                Open = "method",
                Form = form
            };

            await FillFinanceAsync(model, me, cancellationToken);

            return View("Executor/Finance", model);
        }

        var kind = Enum.TryParse<PayoutKind>(form.Kind, out var parsed) ? parsed : PayoutKind.Card;

        await _insights.SaveMethodAsync(me.Id, form.Id, kind, form.Label, form.Note, cancellationToken);

        TempData["Cabinet.Flash"] = form.Id > 0
            ? Tr.T("Спосіб виведення оновлено.")
            : Tr.T("Спосіб виведення додано.");

        return RedirectToAction(nameof(Finance));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMethod(int id, CancellationToken cancellationToken = default)
    {
        if (!CabinetRole.IsExecutor(HttpContext))
        {
            return RedirectToAction(nameof(Index));
        }

        var me = await MeAsync();

        TempData["Cabinet.Flash"] = await _insights.DeleteMethodAsync(me.Id, id, cancellationToken)
            ? Tr.T("Спосіб виведення прибрано.")
            : Tr.T("Такого способу виведення вже немає.");

        return RedirectToAction(nameof(Finance));
    }

    // ───────────────────────────────────────────────────────────────────────
    // Підтримка
    // ───────────────────────────────────────────────────────────────────────

    private async Task<IActionResult> ExecutorSupportAsync(
        string? query, ExecTicketForm? form, CancellationToken cancellationToken)
    {
        var me = await MeAsync();

        var model = new ExecSupportViewModel
        {
            Active = "support",
            Title = Tr.T("Підтримка — Вибір"),
            Query = Clean(query),
            Form = form ?? new ExecTicketForm()
        };

        // Шукаємо в тій самій довідці, що й публічна сторінка «Допомога»:
        // два різні набори відповідей на ті самі питання розійшлися б одразу.
        model.Faq = PageContent.Faq
            .Where(item => model.Query.Length == 0
                           || item.Question.Contains(model.Query, StringComparison.OrdinalIgnoreCase)
                           || item.Answer.Contains(model.Query, StringComparison.OrdinalIgnoreCase))
            .ToList();

        model.Tickets = await _insights.GetTicketsAsync(me.Id, 3, cancellationToken);

        await DressAsync(model, me, cancellationToken);

        return View("Executor/Support", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendTicket(ExecTicketForm form, CancellationToken cancellationToken = default)
    {
        if (!CabinetRole.IsExecutor(HttpContext))
        {
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
        {
            return await ExecutorSupportAsync(null, form, cancellationToken);
        }

        var me = await MeAsync();

        await _insights.AddTicketAsync(me.Id, form.Topic, form.Category, form.Message, cancellationToken);

        TempData["Cabinet.Flash"] = Tr.T("Звернення надіслано. Відповідь прийде на пошту протягом 6 годин.");

        return RedirectToAction(nameof(Support));
    }

    // ───────────────────────────────────────────────────────────────────────
    // Налаштування
    // ───────────────────────────────────────────────────────────────────────

    private async Task<IActionResult> ExecutorSettingsAsync(CancellationToken cancellationToken)
    {
        var me = await MeAsync();

        var model = ToExecSettings(me);
        await DressAsync(model, me, cancellationToken);

        return View("Executor/Settings", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveProfile(
        ExecSettingsViewModel model, CancellationToken cancellationToken = default)
    {
        if (!CabinetRole.IsExecutor(HttpContext))
        {
            return RedirectToAction(nameof(Index));
        }

        var me = await MeAsync();

        model.Active = "settings";
        model.Title = Tr.T("Налаштування — Вибір");
        model.Avatar = me.AvatarUrl ?? CabinetArtwork.Portrait(me.FirstName, me.Id);
        model.MailConfirmed = me.EmailConfirmed;

        if (!ModelState.IsValid)
        {
            await DressAsync(model, me, cancellationToken);
            return View("Executor/Settings", model);
        }

        me.FirstName = model.FirstName.Trim();
        me.LastName = model.LastName.Trim();
        me.FullName = $"{me.FirstName} {me.LastName}";
        me.Nickname = Empty(model.Nickname);
        me.Headline = Empty(model.Headline);
        me.About = Empty(model.About);
        me.PhoneNumber = Empty(model.Phone);
        me.City = Empty(model.City);
        me.TwoFactorEnabled = model.TwoFactor;

        me.NotifyOrders = model.NotifyOrders;
        me.NotifyMessages = model.NotifyMessages;
        me.NotifyReviews = model.NotifyReviews;
        me.NotifyReminders = model.NotifyReminders;
        me.NotifyMarketing = model.NotifyMarketing;

        me.Language = Pick(CabinetContent.Languages, model.Language, "uk");

        // «Мова інтерфейсу» в налаштуваннях і перемикач унизу сайту — одне й те саме.
        PublicCulture.Remember(Response, me.Language);
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

            _logger.LogWarning("Не вдалося зберегти профіль виконавця {UserId}", me.Id);

            await DressAsync(model, me, cancellationToken);
            return View("Executor/Settings", model);
        }

        TempData["Cabinet.Flash"] = Tr.T("Зміни збережено.");

        return RedirectToAction(nameof(Settings));
    }

    /// <summary>Вийти з усіх пристроїв, крім поточного — міняємо штамп безпеки.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SignOutEverywhere(CancellationToken cancellationToken = default)
    {
        var me = await MeAsync();

        // Новий штамп безпеки робить недійсними всі видані куки…
        await _users.UpdateSecurityStampAsync(me);

        // …тому поточному пристрою одразу видаємо нову — як і обіцяє підпис на макеті.
        await _signIn.RefreshSignInAsync(me);

        TempData["Cabinet.Flash"] = Tr.T("Інші пристрої вийшли з акаунта.");

        return RedirectToAction(nameof(Settings));
    }

    // ───────────────────────────────────────────────────────────────────────
    // Перемикання боку
    // ───────────────────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Switch(string? to)
    {
        var side = to == "executor" ? CabinetSide.Executor : CabinetSide.Customer;

        var hasRole = side == CabinetSide.Executor ? CabinetRole.CanExecute(User) : CabinetRole.CanOrder(User);

        // «Стати виконавцем» / «Стати замовником»: якщо такої ролі в акаунті ще немає,
        // додаємо її тому ж акаунту — заводити другу реєстрацію з іншою поштою не треба.
        if (!hasRole)
        {
            var me = await MeAsync();
            var role = side == CabinetSide.Executor ? AppRoles.Executor : AppRoles.Customer;

            var added = await _users.AddToRoleAsync(me, role);

            if (!added.Succeeded)
            {
                _logger.LogWarning("Не вдалося додати роль {Role} користувачу {UserId}: {Errors}",
                    role, me.Id, string.Join("; ", added.Errors.Select(e => e.Description)));
                TempData["Cabinet.Error"] = Tr.T("Не вдалося змінити роль. Спробуйте пізніше.");
                return RedirectToAction(nameof(Index));
            }

            // Ролі зберігаються в cookie входу — без оновлення сайт ще не знав би про нову.
            await _signIn.RefreshSignInAsync(me);

            TempData["Cabinet.Flash"] = side == CabinetSide.Executor
                ? Tr.T("Готово! Тепер ви також виконавець: заповніть профіль і додайте свої послуги.")
                : Tr.T("Готово! Тепер ви також замовник: можете створювати замовлення.");
        }

        CabinetRole.Remember(HttpContext, side);

        return RedirectToAction(side == CabinetSide.Executor && !hasRole ? nameof(Settings) : nameof(Index));
    }

    // ───────────────────────────────────────────────────────────────────────
    // Допоміжне
    // ───────────────────────────────────────────────────────────────────────

    /// <summary>Спільні для всіх екранів виконавця дрібниці: бейджі меню й віджет присутності.</summary>
    private async Task DressAsync(ExecutorPageModel model, ApplicationUser me, CancellationToken cancellationToken)
    {
        model.Unread = await _cabinet.GetUnreadCountAsync(me.Id, CabinetSide.Executor, cancellationToken);
        model.Fresh = (await _exec.GetCountersAsync(me.Id, cancellationToken)).Fresh;
        model.Presence = ExecutorContent.PresenceTitle(me.Presence);
        model.ProfileViews = me.ProfileViews;
        model.CanSwitch = CabinetRole.CanSwitch(User);
    }

    private async Task FillServiceFormAsync(ExecServicesViewModel model, CancellationToken cancellationToken)
    {
        var rows = await _exec.GetLeafCategoriesAsync(cancellationToken);

        model.Categories = rows
            .Select(r => new Choice(r.Id.ToString(), $"{Tr.T(r.Parent)} › {Tr.T(r.Name)}"))
            .ToList();
    }

    private static ExecOrderTab ExecTab(string key) => key switch
    {
        "fresh" => ExecOrderTab.Fresh,
        // «Заплановані» на головній — ті самі взяті в роботу замовлення зі строком попереду.
        "planned" => ExecOrderTab.InWork,
        "work" => ExecOrderTab.InWork,
        "completed" => ExecOrderTab.Completed,
        "cancelled" => ExecOrderTab.Cancelled,
        _ => ExecOrderTab.All
    };

    /// <summary>Період зі списку; чуже число з адреси не пускаємо далі.</summary>
    private static int Period(int? days) =>
        ExecutorContent.Periods.Any(p => p.Days == days) ? days!.Value : 30;

    private async Task FillFinanceAsync(
        ExecFinanceViewModel model, ApplicationUser me, CancellationToken cancellationToken)
    {
        model.Data = await _insights.GetFinanceAsync(me.Id, model.Days, cancellationToken);
        model.Minimum = ExecutorInsightsService.MinWithdraw;
        model.Commission = ExecutorInsightsService.Commission * 100;

        await DressAsync(model, me, cancellationToken);
    }

    private ExecSettingsViewModel ToExecSettings(ApplicationUser user) => new()
    {
        Active = "settings",
        Title = Tr.T("Налаштування — Вибір"),
        Avatar = user.AvatarUrl ?? CabinetArtwork.Portrait(user.FirstName, user.Id),
        FirstName = user.FirstName,
        LastName = user.LastName,
        Nickname = user.Nickname,
        Headline = user.Headline,
        About = user.About,
        Email = user.Email ?? string.Empty,
        Phone = user.PhoneNumber,
        City = user.City,
        TwoFactor = user.TwoFactorEnabled,
        NotifyOrders = user.NotifyOrders,
        NotifyMessages = user.NotifyMessages,
        NotifyReviews = user.NotifyReviews,
        NotifyReminders = user.NotifyReminders,
        NotifyMarketing = user.NotifyMarketing,
        // Показуємо мову, якою сайт зараз відкритий (її могли змінити й унизу сторінки).
        Language = PublicCulture.Current.Code,
        TimeZoneName = user.TimeZoneName ?? "Europe/Kyiv",
        DateFormat = user.DateFormat ?? "dd.MM.yyyy",
        TimeFormat = user.TimeFormat ?? "24",
        Theme = user.Theme ?? "light",
        MailConfirmed = user.EmailConfirmed
    };
}
