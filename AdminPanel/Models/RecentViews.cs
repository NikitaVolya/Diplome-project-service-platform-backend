namespace AdminPanel.Models;

public static class RecentViews
{
    private const string CookieName = "vybir_recent";

    public const int Shown = 6;

    private const int Capacity = 12;

    private static readonly TimeSpan Lifetime = TimeSpan.FromDays(90);

    public static IReadOnlyList<int> Read(HttpRequest request)
    {
        var raw = request.Cookies[CookieName];

        if (string.IsNullOrEmpty(raw))
        {
            return Array.Empty<int>();
        }

        var ids = new List<int>(Capacity);

        foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            if (int.TryParse(part, out var id) && id > 0 && !ids.Contains(id))
            {
                ids.Add(id);
            }

            if (ids.Count == Capacity)
            {
                break;
            }
        }

        return ids;
    }

    public static void Remember(HttpContext context, int categoryId)
    {
        if (categoryId <= 0)
        {
            return;
        }

        var ids = new List<int> { categoryId };
        ids.AddRange(Read(context.Request).Where(id => id != categoryId));

        context.Response.Cookies.Append(
            CookieName,
            string.Join(',', ids.Take(Capacity)),
            new CookieOptions
            {
                HttpOnly = true,
                IsEssential = false,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow + Lifetime
            });
    }
}
