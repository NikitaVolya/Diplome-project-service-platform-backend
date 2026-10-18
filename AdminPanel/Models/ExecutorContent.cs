using Domain.Models;

namespace AdminPanel.Models;

/// <summary>
/// Сталі кабінету виконавця: меню, підписи станів, значення списків на формах.
/// Поряд із <see cref="CabinetContent"/>, за тим самим принципом — це частина інтерфейсу,
/// а не дані платформи.
/// </summary>
public static class ExecutorContent
{
    /// <summary>Бічне меню виконавця в порядку макета.</summary>
    public static readonly CabinetNavItem[] Menu =
    {
        new("home", "Головна", "bi-house", "Index"),
        new("orders", "Замовлення", "bi-bell", "Orders"),
        new("services", "Мої послуги", "bi-gear-wide-connected", "Services"),
        new("calendar", "Календар", "bi-calendar3", "Calendar"),
        new("messages", "Повідомлення", "bi-chat-dots", "Messages"),
        new("reviews", "Відгуки", "bi-star", "Reviews"),
        new("portfolio", "Портфоліо", "bi-journal-album", "Portfolio"),
        new("analytics", "Аналітика", "bi-graph-up", "Analytics"),
        new("finance", "Фінанси", "bi-credit-card-2-front", "Finance"),
        new("support", "Підтримка", "bi-headset", "Support"),
        new("settings", "Налаштування", "bi-gear", "Settings")
    };

    /// <summary>Вкладки списку замовлень виконавця.</summary>
    public static readonly Choice[] OrderTabs =
    {
        new("all", "Усі замовлення"),
        new("fresh", "Нові"),
        new("work", "В роботі"),
        new("completed", "Завершені"),
        new("cancelled", "Скасовані")
    };

    /// <summary>Вкладки на головній: там замість «Усіх» є «Заплановані».</summary>
    public static readonly Choice[] HomeTabs =
    {
        new("fresh", "Нові"),
        new("work", "В роботі"),
        new("planned", "Заплановані"),
        new("completed", "Завершені")
    };

    public static readonly Choice[] ReviewTabs =
    {
        new("received", "Отримані"),
        new("given", "Надані"),
        new("torate", "Потрібно оцінити")
    };

    public static readonly Choice[] ReviewSorts =
    {
        new("new", "Найновіші"),
        new("old", "Найдавніші"),
        new("best", "Спочатку високі оцінки"),
        new("worst", "Спочатку низькі оцінки")
    };

    /// <summary>
    /// Типи подій календаря: <c>Title</c> — підпис легенди під сіткою,
    /// <c>Single</c> — мітка на одній події у списку «Найближчі».
    /// </summary>
    public static readonly (EventKind Kind, string Key, string Title, string Single)[] EventKinds =
    {
        (EventKind.Meeting, "meeting", "Зустрічі", "Зустріч"),
        (EventKind.Deadline, "deadline", "Дедлайни", "Дедлайн"),
        (EventKind.Personal, "personal", "Особисті справи", "Особисте"),
        (EventKind.Busy, "busy", "Недоступний", "Недоступний")
    };

    /// <summary>Короткі назви днів у шапці календаря.</summary>
    public static readonly string[] WeekDays = { "Пн.", "Вт.", "Ср.", "Чт.", "Пт.", "Сб.", "Нд." };

    /// <summary>Назви місяців у називному відмінку — для підпису «Серпень 2026».</summary>
    public static readonly string[] Months =
    {
        "Січень", "Лютий", "Березень", "Квітень", "Травень", "Червень",
        "Липень", "Серпень", "Вересень", "Жовтень", "Листопад", "Грудень"
    };

    /// <summary>Перша й остання година, які показує сітка календаря.</summary>
    public const int FirstHour = 8;
    public const int LastHour = 19;

    /// <summary>Розділи портфоліо, які пропонуємо у фільтрі, навіть коли робіт ще немає.</summary>
    public static readonly string[] PortfolioGroups =
    {
        "Логотипи", "Ілюстрації", "Web-дизайн", "Реклама",
        "Соцмережі", "Презентації", "Поліграфія", "Переклади", "Інше"
    };

    /// <summary>
    /// Періоди, за які можна подивитися аналітику й фінанси.
    ///
    /// Найдовший — тридцять днів: сторінка завжди порівнює період із таким самим
    /// попереднім, тож база тримає рівно вдвічі більше днів, ніж показує.
    /// </summary>
    public static readonly (int Days, string Title)[] Periods =
    {
        (7, "Останні 7 днів"),
        (14, "Останні 14 днів"),
        (30, "Останні 30 днів")
    };

    /// <summary>Кроки шкали на графіках — підписи ліворуч від сітки.</summary>
    public static readonly int[] ChartSteps = { 100, 80, 60, 40, 20, 0 };

    /// <summary>Кольори сегментів кільця «Джерела переглядів» у порядку списку.</summary>
    public static string SourceColor(string key) => key switch
    {
        "search" => "#7B61FF",
        "catalog" => "#00DDA5",
        "direct" => "#00F0FF",
        "referral" => "#B8AAFF",
        _ => "#FFD6B8"
    };

    // ── Підтримка ───────────────────────────────────────────────────────────

    /// <summary>Шість плиток «Популярні теми»: іконка, назва, пояснення.</summary>
    public static readonly InfoCard[] SupportTopics =
    {
        new("bi-rocket-takeoff", "Початок роботи", "Як почати працювати на платформі"),
        new("bi-bag-check", "Замовлення та оплата", "Відповіді на питання про замовлення"),
        new("bi-person-badge", "Послуги та профіль", "Як створити та просувати послуги"),
        new("bi-credit-card-2-front", "Виплата та фінанси", "Інформація про виплати та комісії"),
        new("bi-shield-lock", "Обліковий запис", "Налаштування та безпека профілю"),
        new("bi-journal-text", "Правила та політики", "Правила платформи та угоди")
    };

    /// <summary>Категорії звернення — другий список на формі.</summary>
    public static readonly Choice[] TicketKinds =
    {
        new("Питання", "Питання"),
        new("Проблема", "Проблема"),
        new("Пропозиція", "Пропозиція"),
        new("Скарга", "Скарга")
    };

    /// <summary>Картка «Корисні ресурси» праворуч від форми.</summary>
    public static readonly InfoCard[] SupportLinks =
    {
        new("bi-globe2", "База знань", "Інструкції та відповіді на часті питання"),
        new("bi-play-btn", "Відеоуроки", "Навчальні відео та гайди"),
        new("bi-people", "Спільнота виконавців", "Обговорюйте та обмінюйтеся досвідом")
    };

    /// <summary>Картка «Інші способи звʼязку».</summary>
    public static readonly InfoCard[] SupportContacts =
    {
        new("bi-chat-dots", "Online - chat", "Пн-Пт: 9.00 - 18.00"),
        new("bi-envelope", "Email", "support@vybir.ua"),
        new("bi-telephone", "Телефон", "+38 (067) 123-45-67")
    };

    // ── Налаштування ────────────────────────────────────────────────────────

    /// <summary>Перемикачі сповіщень: ключ поля, назва, пояснення.</summary>
    public static readonly (string Key, string Title, string Text)[] Notices =
    {
        ("NotifyOrders", "Нові замовлення", "отримуйте сповіщення про нові замовлення"),
        ("NotifyMessages", "Повідомлення", "отримуйте сповіщення про нові повідомлення"),
        ("NotifyReviews", "Відгуки та оцінки", "отримуйте сповіщення про нові відгуки та оцінки"),
        ("NotifyReminders", "Нагадування", "нагадування про події та дедлайни"),
        ("NotifyMarketing", "Маркетингові розсилки", "отримуйте новини та пропозиції від Вибір")
    };

    /// <summary>
    /// Підключені акаунти. Самого входу через них ще немає — показуємо стан,
    /// який платформа може підтвердити: пошта підтверджена, решта ні.
    /// </summary>
    public static readonly (string Key, string Title, string Icon)[] Accounts =
    {
        ("google", "Google", "bi-google"),
        ("facebook", "Facebook", "bi-facebook"),
        ("apple", "Apple", "bi-apple"),
        ("linkedin", "Linkedin", "bi-linkedin")
    };

    /// <summary>Підпис способу виведення коштів.</summary>
    public static string PayoutTitle(PayoutKind kind) => kind switch
    {
        PayoutKind.PayPal => "Pay Pal",
        PayoutKind.Iban => "Рахунок IBAN",
        _ => "Банківська картка"
    };

    public static string PayoutIcon(PayoutKind kind) => kind switch
    {
        PayoutKind.PayPal => "bi-paypal",
        PayoutKind.Iban => "bi-bank",
        _ => "bi-credit-card-2-front"
    };

    /// <summary>Підпис стану замовлення на картці виконавця.</summary>
    public static string OrderState(OrderStatus status, bool onReview) => status switch
    {
        OrderStatus.Pending => "Нове",
        OrderStatus.InProgress => onReview ? "На перевірці" : "В роботі",
        OrderStatus.Completed => "Завершене",
        _ => "Скасоване"
    };

    public static string ServiceTitle(ServiceState state) => state switch
    {
        ServiceState.Active => "Активна",
        ServiceState.Paused => "На паузі",
        _ => "Чернетка"
    };

    /// <summary>Підпис присутності у віджеті під меню.</summary>
    public static string PresenceTitle(string? presence) => presence switch
    {
        "busy" => "Зайнятий",
        "away" => "Відійшов",
        _ => "Online"
    };
}
