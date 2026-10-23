using BLL.Public;

namespace AdminPanel.Models;

/// <summary>
/// Світлини для екранів кабінету. Сервіс у BLL навмисно нічого не знає про wwwroot:
/// він повертає назви категорій та імена людей, а шлях до файлу добирається тут.
/// </summary>
public static class CabinetArtwork
{
    private const int MenPortraits = 6;
    private const int WomenPortraits = 6;

    /// <summary>Заглушки для карток замовлень, для яких ще немає знімка плитки.</summary>
    private static readonly string[] Fallback =
    {
        "/img/landing/service-1.jpg",
        "/img/landing/service-2.jpg",
        "/img/landing/service-3.jpg",
        "/img/landing/service-4.jpg"
    };

    /// <summary>Фото послуги й портрет виконавця для карток замовлень.</summary>
    public static void Apply(IEnumerable<CabinetOrder> orders, CatalogPhotos photos)
    {
        foreach (var order in orders)
        {
            order.Image = photos.Find(order.Parent, order.Category)
                          ?? Fallback[Math.Abs(order.Id) % Fallback.Length];

            if (!string.IsNullOrEmpty(order.ExecutorName))
            {
                order.ExecutorPortrait = Portrait(order.ExecutorName, order.ExecutorId);
            }
        }
    }

    public static void Apply(IEnumerable<CabinetThread> threads)
    {
        foreach (var thread in threads)
        {
            thread.PartnerPortrait = Portrait(thread.PartnerName, thread.PartnerId);
        }
    }

    public static void Apply(IEnumerable<CabinetFavorite> favorites)
    {
        foreach (var favorite in favorites)
        {
            favorite.Portrait = Portrait(favorite.Name, favorite.ExecutorId);
        }
    }

    public static void Apply(IEnumerable<CabinetBid> bids)
    {
        foreach (var bid in bids)
        {
            bid.Portrait = Portrait(bid.Name, bid.ExecutorId);
        }
    }

    public static void Apply(IEnumerable<CabinetReview> reviews)
    {
        foreach (var review in reviews)
        {
            review.TargetPortrait = Portrait(review.TargetName, review.OrderId.ToString());
        }
    }

    /// <summary>
    /// Світлина за іменем: жіночі імена отримують жіночі знімки, чоловічі — чоловічі.
    /// Номер у межах статі беремо з ідентифікатора, щоб та сама людина на всіх екранах
    /// була з тим самим обличчям.
    /// </summary>
    public static string Portrait(string? name, string? key)
    {
        var slot = Math.Abs(StableHash(key ?? name ?? string.Empty));

        return PersonGender.Detect(name ?? string.Empty) == Gender.Female
            ? $"/img/executors/women/{slot % WomenPortraits + 1}.jpg"
            : $"/img/executors/men/{slot % MenPortraits + 1}.jpg";
    }

    /// <summary>
    /// string.GetHashCode у .NET різний від запуску до запуску, і людина міняла б обличчя
    /// після кожного перезапуску сайту. Тут — проста стала функція.
    /// </summary>
    private static int StableHash(string value)
    {
        var hash = 17;

        foreach (var symbol in value)
        {
            hash = unchecked(hash * 31 + symbol);
        }

        return hash;
    }
}
