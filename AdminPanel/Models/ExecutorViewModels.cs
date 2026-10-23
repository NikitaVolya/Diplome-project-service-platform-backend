using System.ComponentModel.DataAnnotations;
using BLL.Public;
using Domain.Models;

namespace AdminPanel.Models;

/// <summary>
/// Спільна частина сторінок кабінету виконавця. Окремо від <see cref="CabinetPageModel"/>,
/// бо в меню виконавця інші пункти, а під меню стоїть віджет присутності.
/// </summary>
public abstract class ExecutorPageModel
{
    public string Active { get; set; } = "home";

    public string Title { get; set; } = string.Empty;

    public int Unread { get; set; }

    /// <summary>Скільки нових замовлень чекають відповіді — бейдж у меню.</summary>
    public int Fresh { get; set; }

    /// <summary>«Online», «Зайнятий» — підпис у віджеті під меню.</summary>
    public string Presence { get; set; } = "Online";

    public int ProfileViews { get; set; }

    /// <summary>Людина має обидві ролі, тож у меню показуємо перемикач.</summary>
    public bool CanSwitch { get; set; }
}

public sealed class ExecHomeViewModel : ExecutorPageModel
{
    public string FullName { get; set; } = string.Empty;
    public string Headline { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Avatar { get; set; } = string.Empty;
    public double Rating { get; set; }
    public int Reviews { get; set; }

    public ExecSummary Summary { get; set; } = new();
    public string Tab { get; set; } = "fresh";
}

public sealed class ExecOrdersViewModel : ExecutorPageModel
{
    public ExecCounters Counters { get; set; } = new();
    public ExecOrdersPage Orders { get; set; } = new();
    public string Tab { get; set; } = "all";
    public string Query { get; set; } = string.Empty;
}

public sealed class ExecServicesViewModel : ExecutorPageModel
{
    public ExecServicesPage Services { get; set; } = new();
    public string Query { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public List<Choice> Categories { get; set; } = new();

    /// <summary>Форма додавання чи редагування послуги; null — форма закрита.</summary>
    public ExecServiceForm? Form { get; set; }
}

public sealed class ExecServiceForm
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Назвіть послугу")]
    [StringLength(120, MinimumLength = 3, ErrorMessage = "Назва має бути від 3 до 120 символів")]
    [Display(Name = "Назва послуги")]
    public string Title { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Оберіть напрям каталогу")]
    [Display(Name = "Категорія")]
    public int CategoryId { get; set; }

    [StringLength(2000)]
    [Display(Name = "Опис")]
    public string? Description { get; set; }

    [Range(0, 1_000_000, ErrorMessage = "Ціна має бути від 0 до 1 000 000")]
    [Display(Name = "Ціна від")]
    public decimal FromPrice { get; set; }

    [StringLength(120)]
    [Display(Name = "Ім'я файлу знімка")]
    public string? Image { get; set; }

    [Display(Name = "Опублікувати одразу")]
    public bool Publish { get; set; } = true;
}

public sealed class ExecCalendarViewModel : ExecutorPageModel
{
    public ExecCalendar Calendar { get; set; } = new();
    public string Query { get; set; } = string.Empty;

    /// <summary>Понеділок показаного тижня у форматі yyyy-MM-dd — для кнопок «вперед/назад».</summary>
    public string Week { get; set; } = string.Empty;

    public string Previous { get; set; } = string.Empty;
    public string Next { get; set; } = string.Empty;

    /// <summary>Форма «Додати подію»; null — закрита.</summary>
    public ExecEventForm? Form { get; set; }
}

public sealed class ExecEventForm
{
    [Required(ErrorMessage = "Назвіть подію")]
    [StringLength(120)]
    [Display(Name = "Назва")]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Тип")]
    public string Kind { get; set; } = "Meeting";

    [Required(ErrorMessage = "Вкажіть дату")]
    [Display(Name = "Дата")]
    public string Date { get; set; } = string.Empty;

    [Range(0, 23, ErrorMessage = "Година має бути від 0 до 23")]
    [Display(Name = "Початок")]
    public int Hour { get; set; } = 9;

    [Range(1, 12, ErrorMessage = "Тривалість має бути від 1 до 12 годин")]
    [Display(Name = "Тривалість, год")]
    public int Hours { get; set; } = 1;

    [StringLength(120)]
    [Display(Name = "З ким")]
    public string? PartnerName { get; set; }

    [StringLength(500)]
    [Display(Name = "Нотатка")]
    public string? Note { get; set; }
}

public sealed class ExecReviewsViewModel : ExecutorPageModel
{
    public ExecReviews Reviews { get; set; } = new();
    public string Tab { get; set; } = "received";
    public string Sort { get; set; } = "new";
}

public sealed class ExecPortfolioViewModel : ExecutorPageModel
{
    public ExecPortfolio Portfolio { get; set; } = new();
    public string Group { get; set; } = string.Empty;

    /// <summary>Форма «Додати роботу»; null — закрита.</summary>
    public ExecWorkForm? Form { get; set; }
}

public sealed class ExecWorkForm
{
    [Required(ErrorMessage = "Назвіть роботу")]
    [StringLength(120, MinimumLength = 2, ErrorMessage = "Назва має бути від 2 до 120 символів")]
    [Display(Name = "Назва роботи")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Оберіть розділ")]
    [StringLength(60)]
    [Display(Name = "Розділ")]
    public string Group { get; set; } = "Логотипи";

    [StringLength(120)]
    [Display(Name = "Ім'я файлу знімка")]
    public string? Image { get; set; }

    [StringLength(300)]
    [Url(ErrorMessage = "Схоже, в посиланні помилка")]
    [Display(Name = "Посилання на роботу")]
    public string? Link { get; set; }
}

public sealed class ExecMessagesViewModel : ExecutorPageModel
{
    public List<CabinetThread> Threads { get; set; } = new();
    public CabinetChat? Chat { get; set; }
    public string Filter { get; set; } = "all";
    public string Query { get; set; } = string.Empty;
    public int TotalThreads { get; set; }
    public int UnreadThreads { get; set; }
}

/// <summary>Сторінка-заглушка для розділів, яких дизайнер ще не намалював.</summary>
public sealed class ExecSoonViewModel : ExecutorPageModel
{
    public string Heading { get; set; } = string.Empty;
    public string Lead { get; set; } = string.Empty;

    /// <summary>Що саме тут зʼявиться — показуємо списком, щоб сторінка не була порожньою.</summary>
    public List<string> Plans { get; set; } = new();
}

// ───────────────────────────────────────────────────────────────────────────
// Аналітика
// ───────────────────────────────────────────────────────────────────────────

public sealed class ExecAnalyticsViewModel : ExecutorPageModel
{
    public ExecAnalytics Data { get; set; } = new();

    /// <summary>Обраний період у днях — значення зі списку <see cref="ExecutorContent.Periods"/>.</summary>
    public int Days { get; set; } = 30;
}

// ───────────────────────────────────────────────────────────────────────────
// Фінанси
// ───────────────────────────────────────────────────────────────────────────

public sealed class ExecFinanceViewModel : ExecutorPageModel
{
    public ExecFinance Data { get; set; } = new();
    public int Days { get; set; } = 30;

    /// <summary>Яка форма розгорнута: «method» — новий спосіб, «pay» — заявка на виплату.</summary>
    public string? Open { get; set; }

    /// <summary>Форма «Додати спосіб виведення»; null — закрита.</summary>
    public ExecMethodForm? Form { get; set; }

    /// <summary>Мінімальна сума виведення — показує і картка «Інформація», і форма.</summary>
    public decimal Minimum { get; set; }

    /// <summary>Комісія платформи у відсотках.</summary>
    public decimal Commission { get; set; }
}

public sealed class ExecMethodForm
{
    /// <summary>0 — новий спосіб, інакше правимо наявний.</summary>
    public int Id { get; set; }

    [Required(ErrorMessage = "Оберіть спосіб")]
    [Display(Name = "Спосіб виведення")]
    public string Kind { get; set; } = nameof(PayoutKind.Card);

    [Required(ErrorMessage = "Вкажіть номер картки або пошту гаманця")]
    [StringLength(120, MinimumLength = 4, ErrorMessage = "Від 4 до 120 символів")]
    [Display(Name = "Реквізити")]
    public string Label { get; set; } = string.Empty;

    [StringLength(120)]
    [Display(Name = "Підпис")]
    public string? Note { get; set; }
}

public sealed class ExecWithdrawForm
{
    [Range(1, 1000000, ErrorMessage = "Вкажіть суму")]
    [Display(Name = "Сума")]
    public decimal Amount { get; set; }

    public int? MethodId { get; set; }
}

// ───────────────────────────────────────────────────────────────────────────
// Підтримка
// ───────────────────────────────────────────────────────────────────────────

public sealed class ExecSupportViewModel : ExecutorPageModel
{
    public string Query { get; set; } = string.Empty;

    /// <summary>Знайдені питання з довідки — під пошуком, коли щось шукали.</summary>
    public List<FaqItem> Faq { get; set; } = new();

    /// <summary>Останні звернення цієї людини.</summary>
    public List<ExecTicket> Tickets { get; set; } = new();

    public ExecTicketForm Form { get; set; } = new();
}

public sealed class ExecTicketForm
{
    [Required(ErrorMessage = "Оберіть тему")]
    [StringLength(120)]
    [Display(Name = "Тема звернення")]
    public string Topic { get; set; } = string.Empty;

    [Required(ErrorMessage = "Оберіть категорію")]
    [StringLength(60)]
    [Display(Name = "Категорія")]
    public string Category { get; set; } = string.Empty;

    [Required(ErrorMessage = "Опишіть, що сталося")]
    [StringLength(4000, MinimumLength = 10, ErrorMessage = "Опис має бути від 10 до 4000 символів")]
    [Display(Name = "Опис проблеми")]
    public string Message { get; set; } = string.Empty;
}

// ───────────────────────────────────────────────────────────────────────────
// Налаштування
// ───────────────────────────────────────────────────────────────────────────

public sealed class ExecSettingsViewModel : ExecutorPageModel
{
    public string Avatar { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вкажіть імʼя")]
    [StringLength(100)]
    [Display(Name = "Імʼя")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вкажіть прізвище")]
    [StringLength(100)]
    [Display(Name = "Прізвище")]
    public string LastName { get; set; } = string.Empty;

    [StringLength(60)]
    [Display(Name = "Псевдонім (відображається публічно)")]
    public string? Nickname { get; set; }

    [StringLength(120)]
    [Display(Name = "Професія")]
    public string? Headline { get; set; }

    [StringLength(500, ErrorMessage = "До 500 символів")]
    [Display(Name = "Про себе")]
    public string? About { get; set; }

    [EmailAddress(ErrorMessage = "Схоже, в пошті помилка")]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Схоже, в номері помилка")]
    [Display(Name = "Номер телефону")]
    public string? Phone { get; set; }

    [StringLength(100)]
    [Display(Name = "Місто")]
    public string? City { get; set; }

    [Display(Name = "Двоетапна перевірка")]
    public bool TwoFactor { get; set; }

    public bool NotifyOrders { get; set; }
    public bool NotifyMessages { get; set; }
    public bool NotifyReviews { get; set; }
    public bool NotifyReminders { get; set; }
    public bool NotifyMarketing { get; set; }

    public string Language { get; set; } = "uk";
    public string TimeZoneName { get; set; } = "Europe/Kyiv";
    public string DateFormat { get; set; } = "dd.MM.yyyy";
    public string TimeFormat { get; set; } = "24";
    public string Theme { get; set; } = "light";

    /// <summary>Пошта підтверджена — єдиний «підключений акаунт», який платформа справді знає.</summary>
    public bool MailConfirmed { get; set; }
}
