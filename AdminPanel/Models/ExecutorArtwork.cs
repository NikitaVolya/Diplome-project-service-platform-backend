using BLL.Public;

namespace AdminPanel.Models;

/// <summary>
/// Знімки для екранів кабінету виконавця.
///
/// Портфоліо й власні послуги зберігають у базі тільки ім'я файлу, а самі файли лежать
/// у wwwroot/img/portfolio та wwwroot/img/services — так само, як 220 плиток каталогу.
/// Доки файлу немає, картка показує заглушку того ж розміру, тож сітка не стрибає.
/// </summary>
public static class ExecutorArtwork
{
    public const string PortfolioFolder = "img/portfolio";
    public const string ServicesFolder = "img/services";

    /// <summary>Заглушки для карток без власного знімка.</summary>
    private static readonly string[] Fallback =
    {
        "/img/landing/service-1.jpg",
        "/img/landing/service-2.jpg",
        "/img/landing/service-3.jpg",
        "/img/landing/service-4.jpg"
    };

    /// <summary>Фото замовлення — зі знімка плитки того напряму, що й у кабінеті замовника.</summary>
    public static void Apply(IEnumerable<ExecOrder> orders, CatalogPhotos photos)
    {
        foreach (var order in orders)
        {
            order.Image = photos.Find(order.Parent, order.Category)
                          ?? Fallback[Math.Abs(order.Id) % Fallback.Length];
        }
    }

    /// <summary>
    /// Знімок послуги: спершу власний файл виконавця, потім плитка каталогу того напряму,
    /// і лише тоді заглушка.
    /// </summary>
    public static void Apply(IEnumerable<ExecServiceCard> services, CatalogPhotos photos)
    {
        foreach (var service in services)
        {
            service.Image = photos.FindIn(ServicesFolder, service.Image)
                            ?? photos.Find(service.Parent, service.Category)
                            ?? Fallback[Math.Abs(service.Id) % Fallback.Length];
        }
    }

    public static void Apply(IEnumerable<ExecWork> works, CatalogPhotos photos)
    {
        foreach (var work in works)
        {
            // Для портфоліо запасного знімка з каталогу немає: показувати чужу плитку
            // замість роботи людини гірше, ніж відверта заглушка.
            work.Image = photos.FindIn(PortfolioFolder, work.Image)
                         ?? photos.FindIn(PortfolioFolder, work.Title)
                         ?? string.Empty;
        }
    }

    public static void Apply(IEnumerable<ExecReview> reviews, CatalogPhotos photos)
    {
        foreach (var review in reviews)
        {
            review.Portrait = CabinetArtwork.Portrait(review.PersonName, review.PersonId);
            // Знімок біля відгуку — плитка того напряму, у якому виконували замовлення:
            // так само, як на картці замовлення, щоб одна робота не виглядала двома різними.
            review.Image = photos.Find(review.Parent, review.Category) ?? string.Empty;
        }
    }

    /// <summary>Яким іменем назвати файл, щоб він підхопився сам.</summary>
    public static string Expected(string folder, string name) =>
        $"wwwroot/{folder}/{CatalogPhotos.Slug(name)}.jpg";
}
