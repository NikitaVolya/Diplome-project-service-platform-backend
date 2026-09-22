using AdminPanel.Models;
using BLL.Public;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminPanel.Controllers
{
    // Публічна головна сторінка порталу — єдина сторінка застосунку, відкрита для всіх.
    // Саме через неї відвідувач потрапляє до адмінпанелі: кнопка «Увійти» веде на форму входу,
    // а після входу службовий акаунт опиняється на дашборді.
    [AllowAnonymous]
    public class LandingController : Controller
    {
        private readonly IPublicCatalogService _catalog;

        public LandingController(IPublicCatalogService catalog)
        {
            _catalog = catalog;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            // Сервіс сам підставляє дані з макета, якщо база порожня або недоступна,
            // тому головна сторінка відкривається за будь-яких умов.
            var model = new LandingViewModel
            {
                Categories = await _catalog.GetPopularCategoriesAsync(11, cancellationToken),
                Services = await _catalog.GetPopularServicesAsync(4, cancellationToken)
            };

            return View(model);
        }
    }
}
