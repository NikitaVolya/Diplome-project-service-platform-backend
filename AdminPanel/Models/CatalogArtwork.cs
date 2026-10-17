using BLL.Public;
using DAL.Seed;

namespace AdminPanel.Models;

public static class CatalogArtwork
{
    // Скільки світлин лежить у wwwroot/img/executors/men та .../women.
    private const int MenPortraits = 6;
    private const int WomenPortraits = 6;

    /// <summary>
    /// Порядок напрямів у бічному меню — той, що в макеті. За Id його не відтворити:
    /// частину категорій додано пізніше, тому вони отримали більші номери.
    /// </summary>
    public static List<DirectoryCategory> InDesignOrder(List<DirectoryCategory> categories)
    {
        var parents = categories
            .Where(c => c.ParentId == null)
            .ToDictionary(c => c.Id, c => c.Name);

        return categories
            .OrderBy(c => c.ParentId == null ? CatalogTree.RootOrder(c.Name) : int.MaxValue)
            // Плитки всередині напряму — теж у порядку макета. Ті, яких у дереві немає,
            // стають після них, у порядку створення.
            .ThenBy(c => c.ParentId.HasValue && parents.TryGetValue(c.ParentId.Value, out var parent)
                ? CatalogTree.ChildOrder(parent, c.Name)
                : int.MaxValue)
            .ThenBy(c => c.Id)
            .ToList();
    }

    /// <summary>Апостроф у макеті ’, у базі ' — для порівняння зводимо до одного.</summary>
    private static string Normalize(string value) => value.Replace('\u2019', '\'').Trim();

    // Справжнім виконавцям світлини роздаємо по колу: у базі фотографій немає,
    // а порожні кружечки з літерою замість карток виглядають як недоробка.
    // Чоловічі й жіночі знімки — два окремі кола, інакше під іменем «Софія»
    // опинявся бородань, і картка читалася як помилка.
    public static void Apply(IReadOnlyList<DirectoryExecutor> executors)
    {
        int men = 0, women = 0;

        foreach (var executor in executors)
        {
            executor.Portrait = PersonGender.Detect(executor.Name) == Gender.Female
                ? $"/img/executors/women/{women++ % WomenPortraits + 1}.jpg"
                : $"/img/executors/men/{men++ % MenPortraits + 1}.jpg";
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
