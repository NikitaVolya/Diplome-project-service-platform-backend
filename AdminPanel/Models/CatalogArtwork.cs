using BLL.Public;

namespace AdminPanel.Models;

public static class CatalogArtwork
{
    // Скільки фотографій виконавців лежить у wwwroot/img/executors.
    private const int Portraits = 4;

    /// <summary>
    /// Порядок напрямів у бічному меню — той, що в макеті. За Id його не відтворити:
    /// частину категорій додано пізніше, тому вони отримали більші номери.
    /// </summary>
    public static List<DirectoryCategory> InDesignOrder(List<DirectoryCategory> categories)
    {
        var design = CatalogDemo.Create().Categories
            .Where(c => c.ParentId == null)
            .Select((c, i) => (c.Name, Index: i))
            .ToDictionary(x => Normalize(x.Name), x => x.Index, StringComparer.OrdinalIgnoreCase);

        return categories
            .OrderBy(c => c.ParentId == null
                ? design.GetValueOrDefault(Normalize(c.Name), int.MaxValue)
                : int.MaxValue)
            // Підкатегорії — у порядку макета: Artwork це номер плитки на ньому.
            // Ті, яких у макеті немає, лишаються після них у порядку створення.
            .ThenBy(c => c.Artwork == 0 ? int.MaxValue : c.Artwork)
            .ThenBy(c => c.Id)
            .ToList();
    }

    /// <summary>Апостроф у макеті ’, у базі ' — для порівняння зводимо до одного.</summary>
    private static string Normalize(string value) => value.Replace('\u2019', '\'').Trim();

    // Справжнім виконавцям світлини роздаємо по колу: у базі фотографій немає,
    // а порожні кружечки з літерою замість карток виглядають як недоробка.
    public static void Apply(IReadOnlyList<DirectoryExecutor> executors)
    {
        for (var i = 0; i < executors.Count; i++)
        {
            executors[i].Artwork = i % Portraits + 1;
        }
    }

    public static void Apply(IEnumerable<DirectoryCategory> categories)
    {
        var design = CatalogDemo.Create().Categories;
        var aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Дизайн та творчість"] = "Дизайн і творчість", ["Авто і транспорт"] = "Автопослуги",
            ["IT та технології"] = "IT та розробка", ["Освіта та репетиторство"] = "Освіта і розвиток",
            ["Здоров'я та спорт"] = "Спорт та здоров’я", ["Тварини"] = "Послуги для тварин",
            ["Сантехнік"] = "Сантехнічні роботи", ["Електрик"] = "Електромонтажні роботи",
            ["Малярні роботи"] = "Оздоблювальні роботи", ["Збірка меблів"] = "Меблі та столярні роботи"
        };
        foreach (var category in categories)
        {
            var icon = PublicCatalogService.GetCategoryIcon(category.Name);
            // Для тем без окремого SVG зберігаємо наявну іконку з експорту макета.
            category.Icon = icon.EndsWith("/cat-more.svg", StringComparison.Ordinal) ? "" : icon;
            var name = aliases.GetValueOrDefault(category.Name, category.Name);
            category.Artwork = design.FirstOrDefault(c => (c.ParentId == null) == (category.ParentId == null)
                && string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase))?.Artwork ?? 0;
        }
    }
}
