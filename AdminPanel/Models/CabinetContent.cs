using Domain.Models;

namespace AdminPanel.Models;

public sealed record CabinetNavItem(string Key, string Title, string Icon, string Action);

public sealed record SupportTopic(string Title, string Anchor);

public sealed record Choice(string Key, string Title);

public static class CabinetContent
{
    public static readonly CabinetNavItem[] Menu =
    {
        new("home", "Головна", "bi-house", "Index"),
        new("orders", "Мої замовлення", "bi-journal-text", "Orders"),
        new("messages", "Повідомлення", "bi-chat-dots", "Messages"),
        new("favorites", "Обране", "bi-heart", "Favorites"),
        new("addresses", "Мої адреси", "bi-geo-alt", "Addresses"),
        new("payments", "Платежі та рахунки", "bi-credit-card-2-front", "Payments"),
        new("reviews", "Відгуки", "bi-star", "Reviews"),
        new("support", "Підтримка", "bi-headset", "Support"),
        new("settings", "Налаштування", "bi-gear", "Settings")
    };

    public static readonly Choice[] OrderTabs =
    {
        new("all", "Усі замовлення"),
        new("active", "Активні"),
        new("progress", "В процесі"),
        new("completed", "Завершені"),
        new("cancelled", "Скасовані")
    };

    public static readonly Choice[] HomeTabs = OrderTabs[1..];

    public static readonly SupportTopic[] SupportTopics =
    {
        new("Як створити замовлення?", "faq-0"),
        new("Оплата та комісії", "faq-2"),
        new("Повернення коштів", "faq-2"),
        new("Безпека акаунту", "faq-4"),
        new("Спірні ситуації", "faq-3"),
        new("Робота з виконавцями", "faq-1")
    };

    public static readonly InfoCard[] AddressSteps =
    {
        new("bi-geo-alt", "Додайте адресу",
            "Вкажіть точну адресу та деталі для зручності виконавця."),
        new("bi-hand-index", "Оберіть при замовленні",
            "Вибирайте потрібну адресу під час створення замовлення"),
        new("bi-shield-lock", "Швидко та безпечно",
            "Ваші адреси зберігаються та захищені")
    };

    public static readonly Choice[] AddressKinds =
    {
        new("Home", "Дім"),
        new("Office", "Офіс"),
        new("Country", "Дача")
    };

    public static readonly Choice[] Visibility =
    {
        new("Registered", "Тільки зареєстровані користувачі"),
        new("Everyone", "Усі відвідувачі сайту"),
        new("Nobody", "Ніхто, крім виконавців моїх замовлень")
    };

    public static readonly Choice[] Languages =
    {
        new("uk", "Українська"),
        new("en", "English")
    };

    public static readonly Choice[] TimeZones =
    {
        new("Europe/Kyiv", "GMT+03:00 Київ"),
        new("Europe/Warsaw", "GMT+02:00 Варшава"),
        new("Europe/London", "GMT+01:00 Лондон"),
        new("Europe/Berlin", "GMT+02:00 Берлін")
    };

    public static readonly Choice[] DateFormats =
    {
        new("dd.MM.yyyy", "ДД.ММ.РРРР"),
        new("yyyy-MM-dd", "РРРР-ММ-ДД"),
        new("MM/dd/yyyy", "ММ/ДД/РРРР")
    };

    public static readonly Choice[] TimeFormats =
    {
        new("24", "24-годинний (13:30)"),
        new("12", "12-годинний (1:30 PM)")
    };

    public static readonly Choice[] Currencies =
    {
        new("UAH", "Українська гривня (UAH, ₴)"),
        new("USD", "Долар США (USD, $)"),
        new("EUR", "Євро (EUR, €)")
    };

    public static readonly Choice[] Budgets =
    {
        new("fixed", "Фіксована сума"),
        new("range", "Діапазон"),
        new("hourly", "Погодинно"),
        new("ask", "Договірна")
    };

    public static readonly Choice[] Deadlines =
    {
        new("asap", "Якнайшвидше"),
        new("3", "До 3 днів"),
        new("7", "До тижня"),
        new("30", "До місяця"),
        new("free", "Терміни не горять")
    };

    public static readonly Choice[] ExecutorCounts =
    {
        new("1", "Один виконавець"),
        new("2", "Два виконавці"),
        new("3", "Три і більше")
    };

    public static readonly Choice[] ExperienceLevels =
    {
        new("any", "Будь-який"),
        new("1", "Від 1 року"),
        new("3", "Від 3 років"),
        new("5", "Понад 5 років")
    };

    public static string StatusTitle(OrderStatus status, bool hasExecutor) => status switch
    {
        OrderStatus.Pending => hasExecutor ? "Підтверджено" : "Очікує виконавця",
        OrderStatus.InProgress => "В процесі",
        OrderStatus.Completed => "Виконано",
        _ => "Скасовано"
    };

    public static string StatusTone(OrderStatus status, bool hasExecutor) => status switch
    {
        OrderStatus.Pending => hasExecutor ? "ok" : "wait",
        OrderStatus.InProgress => "work",
        OrderStatus.Completed => "done",
        _ => "off"
    };

    public static string PaymentTitle(PaymentStatus status) => status switch
    {
        PaymentStatus.Completed => "Успішно",
        PaymentStatus.Pending => "В очікуванні",
        PaymentStatus.Refunded => "Повернено",
        _ => "Не вдалося"
    };

    public static string PaymentTone(PaymentStatus status) => status switch
    {
        PaymentStatus.Completed => "ok",
        PaymentStatus.Pending => "wait",
        PaymentStatus.Refunded => "back",
        _ => "fail"
    };

    public static string PaymentKind(PaymentStatus status) =>
        status == PaymentStatus.Refunded ? "Повернення" : "Оплата замовлення";

    public static string Title(Choice[] options, string? key, string fallback) =>
        options.FirstOrDefault(o => string.Equals(o.Key, key, StringComparison.OrdinalIgnoreCase))?.Title ?? fallback;
}
