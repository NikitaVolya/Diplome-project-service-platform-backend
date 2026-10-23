using System.Globalization;
using System.Resources;

namespace AdminPanel.Localization;

/// <summary>
/// Переклад публічної частини сайту.
/// Ключ перекладу — сам український текст: українська є мовою за замовчуванням, тож
/// для неї рядок повертається як є, а для інших мов шукається в
/// <c>Resources/SharedResource.{мова}.resx</c>. Якщо перекладу немає — показуємо оригінал,
/// щоб сторінка ніколи не ламалася через пропущений рядок.
/// У представленнях підключено через <c>@using static</c>, тому досить писати <c>@T("Текст")</c>.
/// </summary>
public static class Tr
{
    private static readonly ResourceManager Resources =
        new("AdminPanel.Resources.SharedResource", typeof(Tr).Assembly);

    /// <summary>Поточна мова інтерфейсу (виставляє <see cref="PublicCulture"/>).</summary>
    public static CultureInfo Ui => CultureInfo.CurrentUICulture;

    public static bool IsUkrainian => PublicCulture.IsDefault(Ui);

    public static string T(string? text)
    {
        if (string.IsNullOrEmpty(text) || IsUkrainian)
        {
            return text ?? string.Empty;
        }

        return Find(text) ?? Find(Normalize(text)) ?? OtherCase(text) ?? text;
    }

    /// <summary>
    /// Resx не дозволяє двох ключів, що різняться лише регістром («Виконавець» і «виконавець»),
    /// тому у файлі лежить один із них. Якщо точного ключа немає, шукаємо слово з іншою
    /// першою літерою і повертаємо переклад у тому ж регістрі, що й запит.
    /// </summary>
    private static string? OtherCase(string text)
    {
        var first = text[0];
        if (!char.IsLetter(first))
        {
            return null;
        }

        var upper = char.IsUpper(first);
        var flipped = (upper ? char.ToLowerInvariant(first) : char.ToUpperInvariant(first)) + text[1..];
        var found = Find(flipped) ?? Find(Normalize(flipped));

        if (string.IsNullOrEmpty(found))
        {
            return null;
        }

        // У німецькій іменники завжди з великої — їх не зменшуємо.
        if (!upper && Ui.TwoLetterISOLanguageName == "de")
        {
            return found;
        }

        var head = upper ? char.ToUpper(found[0], Ui) : char.ToLower(found[0], Ui);
        return head + found[1..];
    }

    /// <summary>Переклад шаблону з підстановками: <c>F("від {0} грн", ціна)</c>.</summary>
    public static string F(string format, params object?[] args) =>
        string.Format(Ui, T(format), args);

    /// <summary>
    /// Опис категорії з бази. Сидер складає його за шаблонами — перекладаємо шаблон
    /// і назви всередині окремо; довільний текст адміністратора шукаємо цілком.
    /// </summary>
    public static string CategoryDescription(string? text)
    {
        if (string.IsNullOrWhiteSpace(text) || IsUkrainian)
        {
            return text ?? string.Empty;
        }

        const string direction = " — напрям «";
        var split = text.IndexOf(direction, StringComparison.Ordinal);
        if (split > 0 && text.EndsWith('»'))
        {
            var child = text[..split];
            var root = text[(split + direction.Length)..^1];
            return F("{0} — напрям «{1}»", T(child), T(root));
        }

        const string services = "Послуги напряму «";
        if (text.StartsWith(services, StringComparison.Ordinal) && text.EndsWith('»'))
        {
            return F("Послуги напряму «{0}»", T(text[services.Length..^1]));
        }

        return T(text);
    }

    private static string? Find(string key)
    {
        try
        {
            return Resources.GetString(key, Ui);
        }
        catch (MissingManifestResourceException)
        {
            return null;
        }
    }

    // У макеті апостроф ’ або ʼ, у базі — '. Ключі перекладів записані з ',
    // тому другу спробу робимо з уніфікованим апострофом.
    private static string Normalize(string text) =>
        text.Replace('’', '\'').Replace('ʼ', '\'');
}
