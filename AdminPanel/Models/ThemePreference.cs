using AdminPanel.Services;

namespace AdminPanel.Models;

/// <summary>
/// Світла чи темна тема публічного сайту й кабінету.
/// Увійшовши, користувач отримує тему зі свого профілю (claim у cookie входу);
/// гість — з окремого cookie, яке ставимо під час входу й збереження налаштувань,
/// щоб після виходу сайт не «спалахував» світлим.
/// </summary>
public static class ThemePreference
{
    public const string CookieName = "Vybir.Theme";

    public const string Dark = "dark";

    public const string Light = "light";

    public static bool IsDark(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            return context.User.FindFirst(AppUserClaimsPrincipalFactory.ThemeClaim)?.Value == Dark;
        }

        return context.Request.Cookies[CookieName] == Dark;
    }

    public static void Remember(HttpResponse response, string? theme)
    {
        response.Cookies.Append(
            CookieName,
            theme == Dark ? Dark : Light,
            new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddYears(1)
            });
    }
}
