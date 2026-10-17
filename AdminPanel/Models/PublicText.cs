using System.Globalization;

namespace AdminPanel.Models;

public static class PublicText
{
    private static readonly CultureInfo Ukrainian = CultureInfo.GetCultureInfo("uk-UA");

    public static string Count(int value, string one, string few, string many)
    {
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
    public static string Price(decimal value) => $"від {value.ToString("N0", Ukrainian)} грн";
    public static string Rating(double value) => value.ToString("0.0", Ukrainian);
}
