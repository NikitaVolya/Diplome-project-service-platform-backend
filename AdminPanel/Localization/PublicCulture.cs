using System.Globalization;

namespace AdminPanel.Localization;

public sealed record SiteLanguage(string Code, string Title);

/// <summary>
/// Мови сайту та вибір поточної. Вибір зберігається в cookie на рік;
/// без cookie сайт показується українською.
/// Міняємо лише CurrentUICulture (мову текстів). CurrentCulture — формат дат і чисел
/// в адмінпанелі — лишається таким, як був, щоб перемикач не зачіпав звіти та таблиці.
/// </summary>
public static class PublicCulture
{
    public const string CookieName = "Vybir.Language";
    public const string DefaultCode = "uk";

    /// <summary>Порядок — як у випадаючому списку в макеті.</summary>
    public static readonly IReadOnlyList<SiteLanguage> Languages = new SiteLanguage[]
    {
        new("uk", "Українська"),
        new("en", "English"),
        new("da", "Dansk"),
        new("et", "Eesti"),
        new("it", "Italiano"),
        new("cs", "Čeština"),
        new("de", "Deutsch"),
        new("fr", "Français"),
        new("pl", "Polski"),
        new("nb", "Norsk"),
        new("nl", "Nederlands")
    };

    public static SiteLanguage Current =>
        Find(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName) ?? Languages[0];

    public static SiteLanguage? Find(string? code) =>
        Languages.FirstOrDefault(l => string.Equals(l.Code, code, StringComparison.OrdinalIgnoreCase));

    public static bool IsDefault(CultureInfo culture) =>
        Find(culture.TwoLetterISOLanguageName) is not { } language || language.Code == DefaultCode;

    /// <summary>Запамʼятовує мову сайту в cookie на рік.</summary>
    public static void Remember(HttpResponse response, string code)
    {
        var language = Find(code) ?? Languages[0];

        response.Cookies.Append(CookieName, language.Code, new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            IsEssential = true,
            HttpOnly = true,
            SameSite = SameSiteMode.Lax
        });
    }

    public static IApplicationBuilder UsePublicCulture(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            var language = Find(context.Request.Cookies[CookieName]) ?? Languages[0];
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(language.Code);
            await next();
        });
}
