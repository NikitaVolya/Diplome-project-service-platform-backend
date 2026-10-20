using System.Globalization;
using AdminPanel.Localization;

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

    /// <summary>
    /// «19 серпня» — день із місяцем у родовому відмінку. Для інших мов сайту назву місяця
    /// бере .NET із культури перемикача («19 August», «19. August», «19 sierpnia»).
    /// </summary>
    private static string DayAndMonth(DateTime value, string dayFormat = "d") =>
        Tr.IsUkrainian
            ? $"{value.Day.ToString(dayFormat == "dd" ? "00" : "0")} {Months[value.Month - 1]}"
            : value.ToString(Tr.Ui.DateTimeFormat.MonthDayPattern, Tr.Ui);

    /// <summary>«19 серпня 14.00» — підпис під назвою замовлення.</summary>
    public static string DateTimeShort(DateTime value) =>
        $"{DayAndMonth(value)} {value:HH.mm}";

    /// <summary>«19 серпня 2026» — у стрічці активності та деталях.</summary>
    public static string LongDate(DateTime value) =>
        $"{DayAndMonth(value)} {value.Year}";

    /// <summary>«19 серпня 2026 о 14.43».</summary>
    public static string LongDateTime(DateTime value) =>
        Tr.F("{0} о {1}", LongDate(value), value.ToString("HH.mm"));

    /// <summary>«26.08.2026 14.00» — два рядки таблиці транзакцій.</summary>
    public static string TableDate(DateTime value) =>
        $"{DayAndMonth(value, "dd")} {value.Year}";

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
            return Tr.T("Вчора");
        }

        if (days < 30)
        {
            return Tr.F("{0} тому", PublicText.Count(days, "день", "дні", "днів"));
        }

        var months = Math.Max(1, days / 30);

        return Tr.F("{0} тому", months < 12
            ? PublicText.Count(months, "місяць", "місяці", "місяців")
            : PublicText.Count(Math.Max(1, months / 12), "рік", "роки", "років"));
    }

    /// <summary>«2 450» — саме число, без одиниці виміру.</summary>
    public static string Number(decimal value) =>
        value.ToString("N0", Numbers);

    /// <summary>«8 545 грн.» — число з нерозривними пробілами, як у макеті.</summary>
    public static string Money(decimal value) =>
        Tr.F("{0} грн.", value.ToString("N0", Numbers));

    /// <summary>«8 545 грн» — те саме, але без крапки, як на плитках гаманця.</summary>
    public static string Uah(decimal value) =>
        Tr.F("{0} грн", value.ToString("N0", Numbers));

    /// <summary>«від 600 грн.» — ціна на картці замовлення.</summary>
    public static string From(decimal value) =>
        value <= 0 ? Tr.T("Договірна") : Tr.F("від {0}", Money(value));

    /// <summary>«-2 500 грн» чи «+1 500 грн» у списку транзакцій.</summary>
    public static string Signed(decimal value, bool incoming) =>
        (incoming ? "+" : "-") + Uah(value);

    /// <summary>
    /// Значення з BLL на кшталт «2450 грн»: одиницю перекладаємо, число лишаємо.
    /// </summary>
    public static string Units(string? value)
    {
        const string uah = " грн";
        return value != null && value.EndsWith(uah, StringComparison.Ordinal)
            ? Tr.F("{0} грн", value[..^uah.Length])
            : value ?? string.Empty;
    }

    /// <summary>«4.9» — рейтинг з одним знаком.</summary>
    public static string Rating(double value) =>
        value.ToString("0.0", CultureInfo.InvariantCulture);

    /// <summary>«1 серпня» — підпис дня під графіком.</summary>
    public static string DayMonth(DateTime value) =>
        DayAndMonth(value);

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
