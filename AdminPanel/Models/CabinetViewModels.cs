using System.ComponentModel.DataAnnotations;
using BLL.Public;
using Domain.Models;

namespace AdminPanel.Models;

public abstract class CabinetPageModel
{
    public string Active { get; set; } = "home";

    public string Title { get; set; } = string.Empty;

    public bool IsDemo { get; set; }

    public int Unread { get; set; }
}

public sealed class CabinetPagerModel
{
    public int Page { get; set; } = 1;
    public int Pages { get; set; } = 1;

    public string Action { get; set; } = "Orders";

    public Dictionary<string, string?> Route { get; set; } = new();

    public Dictionary<string, string?> For(int page)
    {
        var values = new Dictionary<string, string?>(Route) { ["page"] = page.ToString() };
        return values;
    }
}

public sealed class CabinetHomeViewModel : CabinetPageModel
{
    public string FirstName { get; set; } = string.Empty;
    public CabinetSummary Summary { get; set; } = new();

    public string Tab { get; set; } = "active";
}

public sealed class CabinetOrdersViewModel : CabinetPageModel
{
    public CabinetOrdersPage Orders { get; set; } = new();
    public string Tab { get; set; } = "all";
    public string Query { get; set; } = string.Empty;
}

public sealed class CabinetOrderViewModel : CabinetPageModel
{
    public CabinetOrderDetails Details { get; set; } = new();

    public List<(string Name, string Value)> Facts { get; set; } = new();
}

public sealed class CabinetMessagesViewModel : CabinetPageModel
{
    public List<CabinetThread> Threads { get; set; } = new();
    public CabinetChat? Chat { get; set; }
    public string Filter { get; set; } = "all";
    public string Query { get; set; } = string.Empty;
}

public sealed class CabinetFavoritesViewModel : CabinetPageModel
{
    public CabinetFavorites Data { get; set; } = new();
    public string Query { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;

    public string Tab { get; set; } = "executors";

    public List<PublicServiceItem> Services { get; set; } = new();
}

public sealed class CabinetAddressesViewModel : CabinetPageModel
{
    public List<UserAddress> Addresses { get; set; } = new();

    public CabinetAddressForm? Form { get; set; }
}

public sealed class CabinetAddressForm
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Назвіть адресу — наприклад, «Дім»")]
    [StringLength(60)]
    [Display(Name = "Назва")]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Тип")]
    public string Kind { get; set; } = "Home";

    [Required(ErrorMessage = "Вкажіть вулицю та будинок")]
    [StringLength(200)]
    [Display(Name = "Вулиця, будинок, квартира")]
    public string Line { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вкажіть населений пункт")]
    [StringLength(100)]
    [Display(Name = "Місто")]
    public string City { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Область")]
    public string? Region { get; set; }

    [StringLength(10)]
    [Display(Name = "Індекс")]
    public string? PostalCode { get; set; }

    [StringLength(200)]
    [Display(Name = "Підказка для виконавця")]
    public string? Note { get; set; }

    [Display(Name = "Основна адреса")]
    public bool IsPrimary { get; set; }
}

public sealed class CabinetPaymentsViewModel : CabinetPageModel
{
    public CabinetWallet Wallet { get; set; } = new();
    public CabinetTransactionsPage Transactions { get; set; } = new();
    public string Type { get; set; } = "all";
    public string Query { get; set; } = string.Empty;

    public string Tab { get; set; } = "transactions";
}

public sealed class CabinetReviewsViewModel : CabinetPageModel
{
    public List<CabinetReview> Reviews { get; set; } = new();

    public List<CabinetOrder> Awaiting { get; set; } = new();
}

public sealed class CabinetSupportViewModel : CabinetPageModel
{
    public string Query { get; set; } = string.Empty;
    public List<FaqItem> Faq { get; set; } = new();
}

public sealed class CabinetSettingsViewModel : CabinetPageModel
{
    [Required(ErrorMessage = "Вкажіть імʼя")]
    [StringLength(100)]
    [Display(Name = "Імʼя")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вкажіть прізвище")]
    [StringLength(100)]
    [Display(Name = "Прізвище")]
    public string LastName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Схоже, в адресі помилка")]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Схоже, у номері помилка")]
    [Display(Name = "Номер телефону")]
    public string? Phone { get; set; }

    public string? Avatar { get; set; }

    public bool TwoFactor { get; set; }

    public bool NotifyByEmail { get; set; }
    public bool NotifyByPush { get; set; }
    public bool NotifyBySms { get; set; }
    public bool NotifyMarketing { get; set; }

    public string ProfileVisibility { get; set; } = "Registered";
    public bool ShowOnlineStatus { get; set; }
    public bool ShareOrderHistory { get; set; }

    public string PaymentCard { get; set; } = string.Empty;
    public string Currency { get; set; } = "UAH";

    public string Language { get; set; } = "uk";
    public string TimeZoneName { get; set; } = "Europe/Kyiv";
    public string DateFormat { get; set; } = "dd.MM.yyyy";
    public string TimeFormat { get; set; } = "24";
    public string Theme { get; set; } = "light";
}

public sealed class CabinetCreateOrderViewModel : CabinetPageModel
{
    [Required(ErrorMessage = "Назвіть замовлення")]
    [StringLength(120, MinimumLength = 5, ErrorMessage = "Назва має бути від 5 до 120 символів")]
    [Display(Name = "Назва замовлення")]
    public string OrderTitle { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Оберіть категорію послуг")]
    [Display(Name = "Категорія")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Опишіть завдання")]
    [StringLength(2000, MinimumLength = 20, ErrorMessage = "Опис має бути від 20 до 2000 символів")]
    [Display(Name = "Опис завдання")]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Бюджет")]
    public string Budget { get; set; } = "fixed";

    [Range(0, 1_000_000, ErrorMessage = "Сума має бути від 0 до 1 000 000")]
    [Display(Name = "від")]
    public decimal PriceFrom { get; set; }

    [Range(0, 1_000_000, ErrorMessage = "Сума має бути від 0 до 1 000 000")]
    [Display(Name = "до")]
    public decimal PriceTo { get; set; }

    [Display(Name = "Термін виконання")]
    public string Deadline { get; set; } = "asap";

    public string Place { get; set; } = "online";

    public string Kind { get; set; } = "once";

    public string Executors { get; set; } = "1";

    public string Level { get; set; } = "any";

    [Display(Name = "Адреса")]
    public int? AddressId { get; set; }

    public bool SharePhone { get; set; }

    public List<Choice> Categories { get; set; } = new();

    public List<UserAddress> Addresses { get; set; } = new();
}
