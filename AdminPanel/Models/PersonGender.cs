namespace AdminPanel.Models;

public enum Gender
{
    Unknown,
    Male,
    Female
}

/// <summary>
/// Стать за іменем — щоб під жіночим іменем не стояла чоловіча світлина.
///
/// Фотографій у профілях немає, тож картки виконавців беруть знімки з
/// wwwroot/img/executors. Ім'я в базі буває і кирилицею, і латиницею
/// (демо-користувачів сидер створює як Olena, Andrii), тому обидва написання
/// в одному списку.
///
/// Порядок перевірок: спершу відомі імена, потім чоловічі винятки на -а/-я,
/// і лише тоді закінчення. Саме закінчення помиляється на Миколі та Іллі,
/// тому воно останнє.
/// </summary>
public static class PersonGender
{
    private static readonly HashSet<string> Female = new(StringComparer.OrdinalIgnoreCase)
    {
        // кирилицею
        "олена", "ірина", "катерина", "наталія", "наталя", "юлія", "анна", "ганна",
        "софія", "марія", "дарія", "дарина", "вікторія", "оксана", "тетяна", "світлана",
        "людмила", "лариса", "галина", "валентина", "надія", "віра", "любов", "ольга",
        "аліна", "анастасія", "христина", "олександра", "євгенія", "інна", "яна",
        "марина", "поліна", "діана", "аліса", "злата", "мирослава", "ярослава",
        "богдана", "василина", "зоряна", "леся", "олеся", "уляна", "соломія", "роксолана",
        "ніна", "алла", "елла", "емілія", "єва", "ліна", "маргарита", "нінель", "адель",
        "жанна", "римма", "сара", "ірма", "лідія", "зінаїда", "таїсія", "аделіна",

        // латиницею — так імена пишуть у демо-даних
        "olena", "iryna", "kateryna", "nataliia", "yuliia", "anna", "hanna", "sofiia",
        "mariia", "daria", "daryna", "viktoriia", "oksana", "tetiana", "svitlana",
        "liudmyla", "larysa", "halyna", "valentyna", "nadiia", "vira", "liubov", "olha",
        "alina", "anastasiia", "khrystyna", "oleksandra", "yevheniia", "inna", "yana",
        "maryna", "polina", "diana", "alisa", "zlata", "myroslava", "yaroslava",
        "bohdana", "vasylyna", "zoriana", "lesia", "olesia", "uliana", "solomiia",
        "nina", "alla", "ella", "emiliia", "yeva", "lina", "marharyta", "sara", "irma", "lidiia"
    };

    private static readonly HashSet<string> Male = new(StringComparer.OrdinalIgnoreCase)
    {
        // чоловічі імена, що закінчуються на -а/-я: сам лише суфікс тут помиляється
        "микола", "ілля", "сава", "лука", "кузьма", "хома", "мина", "данила", "никита",
        "mykola", "illia", "sava", "luka", "kuzma", "khoma", "nikita",

        // решта — для певності, бо ці трапляються найчастіше
        "андрій", "дмитро", "сергій", "олег", "максим", "віталій", "роман", "тарас",
        "ігор", "богдан", "олександр", "іван", "петро", "василь", "михайло", "юрій",
        "володимир", "анатолій", "валерій", "артем", "денис", "євген", "назар", "остап",
        "ярослав", "мирослав", "степан", "павло", "григорій", "костянтин", "руслан",
        "andrii", "dmytro", "serhii", "oleh", "maksym", "vitalii", "roman", "taras",
        "ihor", "bohdan", "oleksandr", "ivan", "petro", "vasyl", "mykhailo", "yurii",
        "volodymyr", "anatolii", "valerii", "artem", "denys", "yevhen", "nazar", "ostap",
        "yaroslav", "myroslav", "stepan", "pavlo", "hryhorii", "kostiantyn", "ruslan"
    };

    /// <summary>Закінчення, після яких ім'я вважаємо жіночим.</summary>
    private static readonly string[] FemaleEndings = { "а", "я", "a" };

    public static Gender Detect(string? name)
    {
        var first = FirstWord(name);

        if (first.Length == 0)
        {
            return Gender.Unknown;
        }

        if (Female.Contains(first))
        {
            return Gender.Female;
        }

        if (Male.Contains(first))
        {
            return Gender.Male;
        }

        foreach (var ending in FemaleEndings)
        {
            if (first.EndsWith(ending, StringComparison.OrdinalIgnoreCase))
            {
                return Gender.Female;
            }
        }

        return Gender.Male;
    }

    /// <summary>Із «Андрій Ш.» або «Andrii S.» лишає саме ім'я.</summary>
    private static string FirstWord(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var trimmed = name.Trim();
        var space = trimmed.IndexOf(' ');

        return (space < 0 ? trimmed : trimmed[..space]).Trim('.', ',', '\'', '’');
    }
}
