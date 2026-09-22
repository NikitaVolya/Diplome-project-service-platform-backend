using AdminPanel.Models;
using BLL.Public;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminPanel.Controllers;

[AllowAnonymous]
public sealed class CatalogController(ServiceDirectory directory, IPublicCatalogService popular,
    IWebHostEnvironment environment, ILogger<CatalogController> logger) : Controller
{
    public async Task<IActionResult> Index(int? categoryId, string? q, bool all = false, bool demo = false,
        CancellationToken cancellationToken = default, string? city = null)
    {
        var model = new CatalogViewModel
        {
            Query = CleanQuery(q), City = CleanQuery(city), ShowAll = all,
            IsGlobalSearch = !categoryId.HasValue && !string.IsNullOrWhiteSpace(q)
        };
        DirectoryData data;
        if (demo && environment.IsDevelopment())
        {
            data = CatalogDemo.Create();
            model.IsDemo = true;
            model.Services = CatalogDemo.Popular();
        }
        else
        {
            try { data = await directory.ReadAsync(categoryId, cancellationToken, model.City); }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                logger.LogError(ex, "Не вдалося завантажити каталог послуг");
                model.Unavailable = true;
                return View(model);
            }
            // Старий сервіс підставляє демонстраційні послуги, якщо замовлень немає.
            // Тому використовуємо його лише коли є справжні завершені замовлення.
            if (data.Executors.Count > 0 && model.City.Length == 0 && !model.IsGlobalSearch)
            {
                var services = await popular.GetPopularServicesAsync(4, cancellationToken);
                model.Services = services.Where(s => data.Categories.Any(c => c.Id == s.CategoryId)).ToList();
            }
        }
        CatalogArtwork.Apply(data.Categories);
        data.Categories = CatalogArtwork.InDesignOrder(data.Categories);
        model.Categories = data.Categories;
        model.Selected = categoryId.HasValue ? data.Categories.FirstOrDefault(c => c.Id == categoryId)
            : data.Categories.FirstOrDefault(c => c.ParentId == null);
        if (categoryId.HasValue && model.Selected == null) return NotFound();
        model.Items = data.Categories.Where(c => model.IsGlobalSearch || (c.ParentId == model.Selected?.Id && c.ParentId != null))
            .Where(c => model.Query.Length == 0 || c.Name.Contains(model.Query, StringComparison.OrdinalIgnoreCase)).ToList();
        model.Executors = model.IsDemo && RootIdForDemo(model.Selected) != 1 ? new() : data.Executors;
        CatalogArtwork.Apply(model.Executors);
        if (model.IsGlobalSearch)
        {
            model.Executors = new();
            model.Services = Array.Empty<PublicServiceItem>();
        }
        return View(model);
    }

    private static string CleanQuery(string? value)
    {
        var text = (value ?? "").Trim();
        return text[..Math.Min(text.Length, 100)];
    }

    private static int? RootIdForDemo(DirectoryCategory? category) => category?.ParentId ?? category?.Id;
}
