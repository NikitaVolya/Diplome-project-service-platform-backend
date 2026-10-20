using AdminPanel.Localization;
using AdminPanel.Services;
using Microsoft.AspNetCore.Mvc;

namespace AdminPanel.Controllers;

/// <summary>Зміна фото профілю — спільна для кабінету замовника й виконавця.</summary>
public sealed partial class CabinetController
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(10 * 1024 * 1024)] // з запасом, щоб про завелике фото сказати по-людськи, а не помилкою 413
    public async Task<IActionResult> Photo(IFormFile? photo, [FromServices] AvatarStorage avatars,
        CancellationToken cancellationToken = default)
    {
        var me = await MeAsync();

        var saved = await avatars.SaveAsync(me.Id, photo, cancellationToken);

        if (!saved.Succeeded)
        {
            TempData["Cabinet.Error"] = saved.Error;
            return RedirectToAction(nameof(Settings));
        }

        var previous = me.AvatarUrl;

        me.AvatarUrl = saved.Url;
        me.UpdatedAt = DateTime.UtcNow;

        var result = await _users.UpdateAsync(me);

        if (!result.Succeeded)
        {
            avatars.Delete(saved.Url);
            _logger.LogWarning("Не вдалося зберегти фото профілю користувача {UserId}", me.Id);
            TempData["Cabinet.Error"] = Tr.T("Не вдалося зберегти фото. Спробуйте ще раз.");
            return RedirectToAction(nameof(Settings));
        }

        // Старий файл більше нікому не потрібен. Чужі адреси (наприклад, із пошти)
        // сховище не чіпає — видаляє лише те, що само й зберегло.
        avatars.Delete(previous);

        // Фото лежить і в cookie входу (його показує шапка сайту), тож оновлюємо її.
        await _signIn.RefreshSignInAsync(me);

        TempData["Cabinet.Flash"] = Tr.T("Фото профілю оновлено.");
        return RedirectToAction(nameof(Settings));
    }
}
