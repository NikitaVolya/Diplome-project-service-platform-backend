using AdminPanel.Models;
using BLL.Public;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminPanel.Controllers;

[AllowAnonymous]
public sealed class SearchController(IPublicCatalogService catalog, ILogger<SearchController> logger) : Controller
{
    public async Task<IActionResult> Index(string? q, string? city, CancellationToken cancellationToken = default)
    {
        var model = new SearchViewModel
        {
            Query = Clean(q),
            City = Clean(city)
        };

        try
        {
            model.Categories = await catalog.GetPopularCategoriesAsync(11, cancellationToken);
            model.Services = await catalog.GetPopularServicesAsync(4, cancellationToken);

            var seen = RecentViews.Read(Request);
            var recent = await catalog.GetServicesByIdsAsync(seen, cancellationToken);

            if (recent.Count == 0)
            {
                recent = await catalog.GetMostOrderedServicesAsync(RecentViews.Shown, cancellationToken);
                model.RecentIsFallback = recent.Count > 0;
            }

            model.Recent = recent.Take(RecentViews.Shown).ToList();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Не вдалося зібрати сторінку пошуку");
        }

        return View(model);
    }

    private static string Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
}
