using AdminPanel.Localization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminPanel.Controllers;

/// <summary>Перемикач мови з випадаючого списку внизу сторінки.</summary>
[AllowAnonymous]
public sealed class LanguageController : Controller
{
    [HttpGet]
    public IActionResult Set(string code, string? returnUrl)
    {
        var language = PublicCulture.Find(code) ?? PublicCulture.Languages[0];

        PublicCulture.Remember(Response, language.Code);

        // Повертаємо лише на сторінки цього ж сайту — інакше посилання можна було б
        // використати для переадресації на сторонній ресурс.
        return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : RedirectToAction("Index", "Landing");
    }
}
