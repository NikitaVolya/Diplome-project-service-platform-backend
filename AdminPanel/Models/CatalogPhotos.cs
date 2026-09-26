using System.Collections.Concurrent;
using System.Text;

namespace AdminPanel.Models;

/// <summary>
/// Фотографії на плитках підкатегорій каталогу.
///
/// Файли лежать у wwwroot/img/catalog/tiles/&lt;напрям&gt;/&lt;підкатегорія&gt;.jpg, де імена —
/// транслітерація назв із бази: «Маркетинг і реклама» / «Контекстна реклама» →
/// tiles/marketynh-i-reklama/kontekstna-reklama.jpg.
///
/// Нічого в базі для цього не потрібно: щоб додати знімок, достатньо покласти файл
/// із правильним іменем. Поки файлу немає, плитка показує заглушку того ж розміру,
/// тож сторінка не чекає, доки дизайнер віддасть усі знімки, і нічого не стрибає.
/// </summary>
public sealed class CatalogPhotos
{
    private const string Folder = "img/catalog/tiles";

    /// <summary>Формати, у яких може бути знімок; перший знайдений і виграє.</summary>
    private static readonly string[] Extensions = { ".jpg", ".jpeg", ".png", ".webp" };

    private static readonly Dictionary<char, string> Translit = new()
    {
        ['а'] = "a", ['б'] = "b", ['в'] = "v", ['г'] = "h", ['ґ'] = "g", ['д'] = "d",
        ['е'] = "e", ['є'] = "ie", ['ж'] = "zh", ['з'] = "z", ['и'] = "y", ['і'] = "i",
        ['ї'] = "i", ['й'] = "i", ['к'] = "k", ['л'] = "l", ['м'] = "m", ['н'] = "n",
        ['о'] = "o", ['п'] = "p", ['р'] = "r", ['с'] = "s", ['т'] = "t", ['у'] = "u",
        ['ф'] = "f", ['х'] = "kh", ['ц'] = "ts", ['ч'] = "ch", ['ш'] = "sh", ['щ'] = "shch",
        ['ь'] = "", ['ю'] = "iu", ['я'] = "ia", ['ы'] = "y", ['э'] = "e", ['ё'] = "e", ['ъ'] = ""
    };

    private readonly IWebHostEnvironment _environment;

    // Пошук файлу на диску кешуємо: сторінка каталогу питає про півтори сотні плиток.
    private readonly ConcurrentDictionary<string, string?> _found = new(StringComparer.Ordinal);

    public CatalogPhotos(IWebHostEnvironment environment) => _environment = environment;

    /// <summary>Шлях до знімка для сторінки або null, якщо файлу ще немає.</summary>
    public string? Find(string category, string subcategory)
    {
        var key = $"{Slug(category)}/{Slug(subcategory)}";

        return _found.GetOrAdd(key, folder =>
        {
            foreach (var extension in Extensions)
            {
                var path = $"{Folder}/{folder}{extension}";

                if (_environment.WebRootFileProvider.GetFileInfo(path).Exists)
                {
                    return "/" + path;
                }
            }

            return null;
        });
    }

    /// <summary>Ім'я файлу, який треба покласти, щоб на плитці з'явився знімок.</summary>
    public static string Expected(string category, string subcategory) =>
        $"{Folder}/{Slug(category)}/{Slug(subcategory)}.jpg";

    /// <summary>
    /// Назва українською → ім'я файлу: маленькими латинськими, пробіли й розділові знаки
    /// стають дефісом, апострофи зникають. «Кур'єрська доставка» → kurierska-dostavka.
    /// </summary>
    public static string Slug(string value)
    {
        var builder = new StringBuilder(value.Length * 2);

        foreach (var symbol in value.ToLowerInvariant())
        {
            if (Translit.TryGetValue(symbol, out var latin))
            {
                builder.Append(latin);
            }
            else if (symbol is '\'' or '’' or 'ʼ')
            {
                // апостроф просто зникає, щоб не плодити дефіси всередині слова
            }
            else if (char.IsAsciiLetterOrDigit(symbol))
            {
                builder.Append(symbol);
            }
            else
            {
                builder.Append('-');
            }
        }

        var slug = builder.ToString();

        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        return slug.Trim('-');
    }
}
