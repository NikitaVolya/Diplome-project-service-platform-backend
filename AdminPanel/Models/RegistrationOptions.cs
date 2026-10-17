namespace AdminPanel.Models;

public static class RegistrationOptions
{
    public static readonly string[] Cities =
    {
        "Київ", "Харків", "Одеса", "Дніпро", "Львів", "Запоріжжя", "Кривий Ріг",
        "Миколаїв", "Вінниця", "Полтава", "Чернігів", "Черкаси", "Житомир", "Суми",
        "Хмельницький", "Чернівці", "Рівне", "Івано-Франківськ", "Тернопіль", "Луцьк",
        "Ужгород", "Кропивницький", "Кременчук", "Біла Церква", "Бровари", "Ірпінь"
    };

    public static readonly (string Value, string Label)[] Genders =
    {
        ("Female", "Жіноча"),
        ("Male", "Чоловіча"),
        ("Other", "Інша")
    };

    public static readonly (string Value, string Label)[] Experience =
    {
        ("LessThanYear", "Менше року"),
        ("OneToThree", "1–3 роки"),
        ("ThreeToFive", "3–5 років"),
        ("FiveToTen", "5–10 років"),
        ("MoreThanTen", "Понад 10 років")
    };

    public static bool IsKnownCity(string? value) =>
        value != null && Cities.Contains(value, StringComparer.Ordinal);

    public static bool IsKnownGender(string? value) =>
        value == null || Genders.Any(g => g.Value == value);

    public static bool IsKnownExperience(string? value) =>
        value == null || Experience.Any(e => e.Value == value);
}
