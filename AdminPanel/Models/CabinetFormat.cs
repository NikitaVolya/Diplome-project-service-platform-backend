using System.Globalization;

namespace AdminPanel.Models;

/// <summary>
/// Дати й суми так, як вони написані в макетах кабінету: «19 серпня 14.00»,
/// «2 дні тому», «8 545 грн.». Українська культура в контейнері може бути неповною,
/// тому назви місяців беремо зі свого списку, а не з CultureInfo.
/// </summary>
public static class CabinetFormat
{
    /// <summary>
    /// Числа в макеті розділені пробілом: «8 545 грн.». Культуру контейнера брати не можна —
    /// у мінімальному образі ICU неповний, і та сама сторінка показує то пробіл, то кому.
    /// Тому формат задаємо самі.
    /// </summary>
    private static readonly NumberFormatInfo Numbers = new()
    {
        NumberGroupSeparator = "\u00A0",
        NumberDecimalSeparator = ",",
        NumberGroupSizes = new[] { 3 }
    };

    private static readonly string[] Months =
    {
        "січня", "лютого", "березня", "квітня", "травня", "червня",
        "липня", "серпня", "вересня", "жовтня", "листопада", "грудня"
    };

    /// <summary>«19 серпня 14.00» — підпис під назвою замовлення.</summary>
    public static string DateTimeShort(DateTime value) =>
        $"{value.Day} {Months[value.Month - 1]} {value:HH.mm}";

    /// <summary>«19 серпня 2026» — у стрічці активності та деталях.</summary>
    public static string LongDate(DateTime value) =>
        $"{value.Day} {Months[value.Month - 1]} {value.Year}";

    /// <summary>«19 серпня 2026 о 14.43».</summary>
    public static string LongDateTime(DateTime value) =>
        $"{LongDate(value)} о {value:HH.mm}";

    /// <summary>«26.08.2026 14.00» — два рядки таблиці транзакцій.</summary>
    public static string TableDate(DateTime value) =>
        $"{value.Day:00} {Months[value.Month - 1]} {value.Year}";

    /// <summary>
    /// «10.30», «Вчора», «2 дні тому», «3 місяці тому» — так підписані рядки
    /// списку повідомлень у макеті.
    /// </summary>
    public static string Ago(DateTime value)
    {
        var now = DateTime.UtcNow;
        var days = (now.Date - value.Date).Days;

        if (days <= 0)
        {
            return value.ToString("HH.mm");
        }

        if (days == 1)
        {
            return "Вчора";
        }

        if (days < 30)
        {
            return PublicText.Count(days, "день", "дні", "днів") + " тому";
        }

        var months = Math.Max(1, days / 30);

        return months < 12
            ? PublicText.Count(months, "місяць", "місяці", "місяців") + " тому"
            : PublicText.Count(Math.Max(1, months / 12), "рік", "роки", "років") + " тому";
    }

    /// <summary>«2 450» — саме число, без одиниці виміру.</summary>
    public static string Number(decimal value) =>
        value.ToString("N0", Numbers);

    /// <summary>«8 545 грн.» — число з нерозривними пробілами, як у макеті.</summary>
    public static string Money(decimal value) =>
        value.ToString("N0", Numbers) + " грн.";

    /// <summary>«від 600 грн.» — ціна на картці замовлення.</summary>
    public static string From(decimal value) =>
        value <= 0 ? "Договірна" : "від " + Money(value);

    /// <summary>«-2 500 грн» чи «+1 500 грн» у списку транзакцій.</summary>
    public static string Signed(decimal value, bool incoming) =>
        (incoming ? "+" : "-") + value.ToString("N0", Numbers) + " грн";

    /// <summary>«4.9» — рейтинг з одним знаком.</summary>
    public static string Rating(double value) =>
        value.ToString("0.0", CultureInfo.InvariantCulture);

    /// <summary>«1 серпня» — підпис дня під графіком.</summary>
    public static string DayMonth(DateTime value) =>
        $"{value.Day} {Months[value.Month - 1]}";

    /// <summary>«31.08.2026» — дата в таблиці транзакцій.</summary>
    public static string Digits(DateTime value) =>
        value.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);

    /// <summary>«6.2%» — з крапкою, як у макеті, незалежно від культури сервера.</summary>
    public static string Percent(double value) =>
        value.ToString("0.#", CultureInfo.InvariantCulture) + "%";

    /// <summary>«+18%» чи «-4%» — підпис зміни під числом на плитці.</summary>
    public static string Delta(double value) =>
        (value > 0 ? "+" : string.Empty) + Percent(value);
}
