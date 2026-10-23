using AdminPanel.Localization;
using BLL.Public;

namespace AdminPanel.Models;

/// <summary>Сторінка «Знайти замовлення»: фільтри з адресного рядка та знайдені замовлення.</summary>
public sealed class JobsViewModel
{
    public string? Q { get; set; }

    public int? Category { get; set; }

    public string? City { get; set; }

    public int? Radius { get; set; }

    public bool OneTime { get; set; }

    public bool Regular { get; set; }

    public bool Urgent { get; set; }

    public decimal? From { get; set; }

    public decimal? To { get; set; }

    public string? Due { get; set; }

    public string? Place { get; set; }

    public string Sort { get; set; } = "new";

    public string View { get; set; } = "list";

    public int Page { get; set; } = 1;

    public bool Demo { get; set; }

    // ── Заповнює контролер ──────────────────────────────────────────────

    public OrderBoardPage Result { get; set; } = new();

    public List<OrderBoardCategory> Categories { get; set; } = new();

    public bool Unavailable { get; set; }

    public bool SignedIn { get; set; }

    public bool CanBid { get; set; }

    public bool HasFilters =>
        !string.IsNullOrWhiteSpace(Q) || Category.HasValue || !string.IsNullOrWhiteSpace(City) || Radius.HasValue
        || OneTime || Regular || Urgent || From.HasValue || To.HasValue
        || !string.IsNullOrWhiteSpace(Due) || !string.IsNullOrWhiteSpace(Place);

    public OrderBoardQuery ToQuery(int perPage) => new()
    {
        Text = Q,
        CategoryId = Category,
        City = City,
        Radius = Radius,
        OneTime = OneTime,
        Regular = Regular,
        Urgent = Urgent,
        PriceFrom = From,
        PriceTo = To,
        Due = Due,
        Place = Place,
        Sort = Sort switch
        {
            "old" => OrderBoardSort.Oldest,
            "expensive" => OrderBoardSort.PriceHigh,
            "cheap" => OrderBoardSort.PriceLow,
            "deadline" => OrderBoardSort.Deadline,
            "bids" => OrderBoardSort.FewBids,
            _ => OrderBoardSort.Newest
        },
        Page = Page,
        PerPage = perPage
    };

    /// <summary>Параметри поточного пошуку — для посилань пагінації та перемикача вигляду.</summary>
    public Dictionary<string, string> Route(int? page = null, string? view = null)
    {
        var values = new Dictionary<string, string>();

        void Add(string key, object? value)
        {
            var text = value switch
            {
                null => null,
                bool flag => flag ? "true" : null,
                decimal number => number.ToString(System.Globalization.CultureInfo.InvariantCulture),
                _ => value.ToString()
            };

            if (!string.IsNullOrWhiteSpace(text)) values[key] = text;
        }

        Add("q", Q);
        Add("category", Category);
        Add("city", City);
        Add("radius", Radius);
        Add("oneTime", OneTime);
        Add("regular", Regular);
        Add("urgent", Urgent);
        Add("from", From);
        Add("to", To);
        Add("due", Due);
        Add("place", Place);
        Add("sort", Sort == "new" ? null : Sort);
        Add("view", (view ?? View) == "list" ? null : view ?? View);
        Add("page", (page ?? Page) <= 1 ? null : page ?? Page);
        Add("demo", Demo);

        return values;
    }

    public static readonly (string Key, string Title)[] Sorts =
    {
        ("new", "Найновіші"),
        ("old", "Найстаріші"),
        ("expensive", "Спочатку дорожчі"),
        ("cheap", "Спочатку дешевші"),
        ("deadline", "Найближчий термін"),
        ("bids", "Найменше пропозицій")
    };

    public static readonly (int? Km, string Title)[] Radii =
    {
        (null, "У межах міста"),
        (5, "В радіусі 5 км"),
        (10, "В радіусі 10 км"),
        (25, "В радіусі 25 км"),
        (50, "В радіусі 50 км")
    };

    public static readonly (string Key, string Title)[] Deadlines =
    {
        ("today", "Сьогодні або завтра"),
        ("3", "Протягом 3 днів"),
        ("7", "Протягом тижня"),
        ("30", "Протягом місяця"),
        ("none", "Без терміну")
    };
}

public static class JobsText
{
    public static string Bids(int value) => PublicText.Count(value, "пропозиція", "пропозиції", "пропозицій");

    public static string Found(int value) => PublicText.Count(value, "замовлення", "замовлення", "замовлень");

    public static string City(OrderBoardItem item)
    {
        if (item.IsRemote) return Tr.T("Онлайн");

        // Адреса з кабінету має вигляд «вулиця, будинок, місто» — місто стоїть останнім.
        var city = item.Address.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .LastOrDefault() ?? item.Address;

        return Tr.F("м. {0}", Tr.T(city));
    }

    public static string When(OrderBoardItem item)
    {
        if (item.IsRegular) return Tr.T("Регулярно");
        if (item.ExecutionAt is not { } date) return Tr.T("Без терміну");

        var days = (date.Date - DateTime.UtcNow.Date).TotalDays;
        return days <= 1 ? Tr.T("Сьогодні або завтра") : Tr.F("до {0}", CabinetFormat.DayMonth(date));
    }

    public static string PriceNote(OrderBoardItem item) =>
        Tr.T(item.Price > 0 ? "Бюджет замовника" : "Договірна ціна");
}
