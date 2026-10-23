using AdminPanel.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminPanel.Controllers;

[AllowAnonymous]
public sealed class PagesController : Controller
{
    private const int PerPage = 6;

    [HttpGet]
    [Route("Security")]
    public IActionResult Security() => View();

    [HttpGet]
    [Route("Help")]
    public IActionResult Help() => View();

    [HttpGet]
    [Route("Blog")]
    public IActionResult Blog(string? category, int page = 1)
    {
        var all = PageContent.Articles.OrderByDescending(a => a.Date).ToList();

        var known = category != null && PageContent.BlogCategories.Skip(1).Contains(category);
        var filtered = known ? all.Where(a => a.Category == category).ToList() : all;

        var pages = Math.Max(1, (int)Math.Ceiling(filtered.Count / (double)PerPage));
        page = Math.Clamp(page, 1, pages);

        return View(new BlogViewModel
        {
            Category = known ? category! : PageContent.BlogCategories[0],
            Articles = filtered.Skip((page - 1) * PerPage).Take(PerPage).ToList(),
            Page = page,
            Pages = pages
        });
    }

    [HttpGet]
    [Route("Blog/{slug}")]
    public IActionResult Article(string slug)
    {
        var article = PageContent.FindArticle(slug);

        return article == null ? NotFound() : View(article);
    }
}
