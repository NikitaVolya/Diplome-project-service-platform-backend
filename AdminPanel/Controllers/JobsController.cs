using AdminPanel.Localization;
using AdminPanel.Models;
using BLL.Public;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminPanel.Controllers;

[AllowAnonymous]
[Route("Jobs")]
public sealed class JobsController(
    IOrderBoardService board, CatalogPhotos photos, IWebHostEnvironment environment,
    ILogger<JobsController> logger) : Controller
{
    private const int PerPage = 5;

    private const int PerPageGrid = 9;

    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] JobsViewModel model, CancellationToken cancellationToken)
    {
        model.View = model.View == "grid" ? "grid" : "list";
        model.Sort = JobsViewModel.Sorts.Any(s => s.Key == model.Sort) ? model.Sort : "new";
        model.Place = model.Place is "onsite" or "remote" ? model.Place : null;
        model.Due = JobsViewModel.Deadlines.Any(d => d.Key == model.Due) ? model.Due : null;
        model.Q = model.Q?.Trim();
        model.City = string.IsNullOrWhiteSpace(model.City) ? null : model.City.Trim();
        model.SignedIn = User.Identity?.IsAuthenticated == true;
        model.CanBid = model.SignedIn && CabinetRole.CanExecute(User);

        var perPage = model.View == "grid" ? PerPageGrid : PerPage;

        if (model.Demo && environment.IsDevelopment())
        {
            model.Result = JobsDemo.Page(model.Page, perPage);
            model.Categories = JobsDemo.Categories;
            return View(model);
        }

        model.Demo = false;

        try
        {
            model.Categories = await board.GetCategoriesAsync(cancellationToken);
            model.Result = await board.SearchAsync(
                model.ToQuery(perPage),
                User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Не вдалося завантажити замовлення для виконавців");
            model.Unavailable = true;
            return View(model);
        }

        foreach (var item in model.Result.Items)
        {
            item.Image = photos.Find(item.Parent, item.Category) ?? photos.Find(item.Category, item.Category);
        }

        return View(model);
    }

    [HttpPost("Bid")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Bid(
        int orderId, decimal? price, int? days, string? comment, string? returnUrl,
        CancellationToken cancellationToken)
    {
        var back = Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Action(nameof(Index))!;

        if (User.Identity?.IsAuthenticated != true)
        {
            return RedirectToAction("Login", "Auth", new { returnUrl = back });
        }

        if (!CabinetRole.CanExecute(User))
        {
            TempData["Jobs.Error"] = Tr.T("Подавати пропозиції можуть лише виконавці. Стати виконавцем можна в особистому кабінеті.");
            return LocalRedirect(back);
        }

        if (price is null || (comment?.Length ?? 0) > 1000)
        {
            TempData["Jobs.Error"] = Tr.T("Вкажіть свою ціну — без неї пропозицію не надіслати.");
            return LocalRedirect(back);
        }

        var me = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
        var result = await board.PlaceBidAsync(me, orderId, price.Value, days, comment, cancellationToken);

        if (result == BidResult.Sent)
        {
            TempData["Jobs.Flash"] = Tr.T("Пропозицію надіслано. Замовник побачить її на сторінці замовлення.");
        }
        else
        {
            TempData["Jobs.Error"] = result switch
            {
                BidResult.OwnOrder => Tr.T("Це ваше замовлення — на нього не можна подати пропозицію."),
                BidResult.AlreadySent => Tr.T("Ви вже подали пропозицію до цього замовлення."),
                BidResult.Invalid => Tr.T("Перевірте ціну й термін: ціна від 0 до 1 000 000, термін від 1 до 365 днів."),
                _ => Tr.T("Замовлення вже недоступне: можливо, замовник обрав виконавця.")
            };
        }

        return LocalRedirect(back);
    }

    [HttpPost("Favorite")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Favorite(int orderId, string? returnUrl, CancellationToken cancellationToken)
    {
        var back = Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Action(nameof(Index))!;

        if (User.Identity?.IsAuthenticated != true)
        {
            return RedirectToAction("Login", "Auth", new { returnUrl = back });
        }

        var me = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
        var state = await board.ToggleFavoriteAsync(me, orderId, cancellationToken);

        if (Request.Headers.XRequestedWith == "fetch")
        {
            return state == null ? NotFound() : Json(new { favorite = state });
        }

        return LocalRedirect(back);
    }
}

internal static class JobsDemo
{
    public static readonly List<OrderBoardCategory> Categories = new()
    {
        new(1, "Дім і ремонт", null),
        new(2, "Прибирання", null),
        new(3, "Дизайн і творчість", null),
        new(4, "IT та розробка", null),
        new(101, "Сантехнічні роботи", 1),
        new(102, "Дизайн інтер'єру", 3)
    };

    private static readonly OrderBoardItem[] Items =
    {
        new()
        {
            Id = 1, Title = "Потрібно замінити змішувач на кухні", Category = "Сантехнічні роботи",
            Description = "Тече кран, потрібно зняти старий змішувач і встановити новий. Матеріали є. Робота бажано сьогодні або завтра.",
            Price = 800, Address = "Київ", Bids = 3, ExecutionAt = DateTime.UtcNow.AddDays(1), IsUrgent = true,
            Image = "/img/jobs/demo-1.jpg"
        },
        new()
        {
            Id = 2, Title = "Фарбування стін у квартирі", Category = "Дім і ремонт",
            Description = "Потрібно пофарбувати стіни та зашпаклювати дірки у двох кімнатах. Близько 30м. Матеріали в наявності.",
            Price = 5000, Address = "Львів", Bids = 7, ExecutionAt = DateTime.UtcNow.AddDays(25),
            Image = "/img/jobs/demo-2.jpg"
        },
        new()
        {
            Id = 3, Title = "Консультація з дизайну квартири", Category = "Дизайн інтер'єру",
            Description = "Потрібна консультація дизайнера щодо планування кухні та вітальні. Онлайн або на місці.",
            Price = 4500, Address = "Вінниця", Bids = 5, ExecutionAt = DateTime.UtcNow.AddDays(6),
            Image = "/img/jobs/demo-3.jpg"
        },
        new()
        {
            Id = 4, Title = "Регулярне прибирання квартири", Category = "Прибирання",
            Description = "Потрібна допомога з прибиранням квартири раз на тиждень. В квартирі є домашні улюбленці. Квартира 60м.",
            Price = 500, Address = "Рівне", Bids = 12, IsRegular = true,
            Image = "/img/jobs/demo-4.jpg"
        },
        new()
        {
            Id = 5, Title = "Налаштування сайту на WordPress", Category = "IT та розробка",
            Description = "Потрібно встановити плагіни, налаштувати форму зворотного звязку та виправити помилки.",
            Price = 3000, Address = "Дніпро", Bids = 4, ExecutionAt = DateTime.UtcNow.AddDays(33),
            Image = "/img/jobs/demo-5.jpg"
        }
    };

    public static OrderBoardPage Page(int page, int perPage)
    {
        var all = Enumerable.Range(0, 60).Select(i =>
        {
            var source = Items[i % Items.Length];
            return new OrderBoardItem
            {
                Id = i + 1, Title = source.Title, Category = source.Category, Description = source.Description,
                Price = source.Price, Address = source.Address, Bids = source.Bids, ExecutionAt = source.ExecutionAt,
                IsUrgent = source.IsUrgent, IsRegular = source.IsRegular, Image = source.Image, CreatedAt = DateTime.UtcNow
            };
        }).ToList();

        var pages = (int)Math.Ceiling(all.Count / (double)perPage);
        page = Math.Clamp(page, 1, pages);

        return new OrderBoardPage
        {
            Items = all.Skip((page - 1) * perPage).Take(perPage).ToList(),
            Total = 2986,
            Page = page,
            Pages = pages
        };
    }
}
