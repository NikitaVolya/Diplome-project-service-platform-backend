using BLL.Public;

namespace AdminPanel.Models;

// Дані макета доступні тільки за ?demo=true у середовищі Development.
public static class CatalogDemo
{
    public static DirectoryData Create()
    {
        string[] names = ["Дім і ремонт", "Прибирання", "Доставка", "Краса та догляд", "Спорт та здоров’я", "IT та розробка", "Дизайн і творчість", "Фото та відео", "Бізнес-послуги", "Автопослуги", "Маркетинг і реклама", "Освіта і розвиток", "Послуги для тварин", "Події та допомога", "Діти та догляд", "Інше"];
        string[] services = ["Сантехнічні роботи", "Електромонтажні роботи", "Ремонт квартир і будинків", "Будівельні роботи", "Меблі та столярні роботи", "Вікна, двері та скло", "Покрівля та фасади", "Опалення та водопостачання", "Встановлення та ремонт техніки", "Системи безпеки та розумний дім", "Оздоблювальні роботи", "Ландшафтні та зовнішні роботи", "Домашній майстер", "Вентиляція та кондиціонування", "Інші роботи"];
        int[] counts = [256, 256, 189, 189, 156, 235, 112, 235, 77, 45, 235, 112, 235, 77, 45];
        var data = new DirectoryData();
        data.Categories = names.Select((name, i) => new DirectoryCategory
        {
            Id = i + 1, Name = name, Artwork = i + 1,
            Description = i == 0 ? "Будівельні роботи, монтаж та встановлення" : "Оберіть потрібний напрям послуг",
            Executors = i == 0 ? 1523 : 0
        }).ToList();
        data.Categories.AddRange(services.Select((name, i) => new DirectoryCategory
        {
            Id = 101 + i, ParentId = 1, Name = name, Executors = counts[i], Artwork = i + 1
        }));
        data.Executors =
        [
            // Світлини тут не вказані: їх роздає CatalogArtwork.Apply — за статтю імені,
            // тими самими правилами, що й для справжніх виконавців.
            new() { Name = "Олександр К.", Rating = 4.9, Reviews = 32, Completed = 522, FromPrice = 600 },
            new() { Name = "Ольга П.", Rating = 4.8, Reviews = 218, Completed = 412, FromPrice = 250 },
            new() { Name = "Сергій М.", Rating = 4.9, Reviews = 341, Completed = 687, FromPrice = 350 },
            new() { Name = "Анастасія К.", Rating = 4.8, Reviews = 172, Completed = 305, FromPrice = 400 }
        ];
        return data;
    }

    public static IReadOnlyList<PublicServiceItem> Popular() =>
    [
        new() { CategoryId = 101, Name = "Сантехнік", FromPrice = 600, Rating = 4.9, ReviewsCount = 32, Image = "/img/landing/service-1.jpg" },
        new() { CategoryId = 2, Name = "Прибирання квартири", FromPrice = 700, Rating = 5, ReviewsCount = 18, Image = "/img/landing/service-2.jpg" },
        new() { CategoryId = 7, Name = "Дизайн логотипу", FromPrice = 800, Rating = 4.8, ReviewsCount = 24, Image = "/img/landing/service-3.jpg" },
        new() { CategoryId = 102, Name = "Електрик", FromPrice = 600, Rating = 4.9, ReviewsCount = 27, Image = "/img/landing/service-4.jpg" }
    ];
}
