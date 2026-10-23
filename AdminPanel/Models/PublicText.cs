using System.Globalization;
using AdminPanel.Localization;

namespace AdminPanel.Models;

public static class PublicText
{
    private static readonly CultureInfo Ukrainian = CultureInfo.GetCultureInfo("uk-UA");

    /// <summary>
    /// Число з правильною формою слова. Форми передаються українською
    /// (одна / кілька / багато). Для інших мов кожна форма — окремий шаблон
    /// перекладу «{0} слово», а яку з них узяти, вирішують правила множини мови сайту.
    /// </summary>
    public static string Count(int value, string one, string few, string many)
    {
        if (!Tr.IsUkrainian)
        {
            var key = PluralForm(value, Tr.Ui.TwoLetterISOLanguageName) switch
            {
                0 => "{0} " + one,
                // В українській «1 замовлення» і «2 замовлення» пишуться однаково, а в польській
                // чи чеській — ні. Щоб у файлі перекладу це були різні рядки, форму «кілька»
                // в такому разі позначаємо окремо.
                1 => few == one ? "{0} " + few + " (2–4)" : "{0} " + few,
                _ => "{0} " + many
            };
            return Tr.F(key, value.ToString("N0", Tr.Ui));
        }

        var absolute = Math.Abs((long)value);
        var form = absolute % 100 is >= 11 and <= 14 ? many : (absolute % 10) switch
        {
            1 => one,
            2 or 3 or 4 => few,
            _ => many
        };
        return $"{value.ToString("N0", Ukrainian)} {form}";
    }

    public static string Executors(int value) => Count(value, "виконавець", "виконавці", "виконавців");
    public static string Reviews(int value) => Count(value, "відгук", "відгуки", "відгуків");
    public static string Categories(int value) => Count(value, "категорія", "категорії", "категорій");

    public static string Price(decimal value) => Tr.IsUkrainian
        ? $"від {value.ToString("N0", Ukrainian)} грн"
        : Tr.F("від {0} грн", value.ToString("N0", Tr.Ui));

    public static string Rating(double value) =>
        value.ToString("0.0", Tr.IsUkrainian ? Ukrainian : Tr.Ui);

    // 0 — «один», 1 — «кілька», 2 — «багато».
    // Польська й чеська мають три форми, як українська; решта мов перемикача — дві.
    private static int PluralForm(int value, string language)
    {
        var n = Math.Abs((long)value);
        return language switch
        {
            "pl" => n == 1 ? 0 : n % 10 is >= 2 and <= 4 && n % 100 is < 12 or > 14 ? 1 : 2,
            "cs" => n == 1 ? 0 : n is >= 2 and <= 4 ? 1 : 2,
            "fr" => n <= 1 ? 0 : 2,
            _ => n == 1 ? 0 : 2
        };
    }
}
