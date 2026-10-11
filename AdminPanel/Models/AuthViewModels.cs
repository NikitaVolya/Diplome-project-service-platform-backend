using System.ComponentModel.DataAnnotations;
using Domain.Common;
using BLL.Public;

namespace AdminPanel.Models;
public sealed class PublicLoginViewModel
{
    [Required(ErrorMessage = "Введіть e-mail")]
    [EmailAddress(ErrorMessage = "Перевірте адресу — схоже, в ній помилка")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введіть пароль")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; } = true;

    public string? ReturnUrl { get; set; }
}
public sealed class RegisterStepOneViewModel
{
    [Required(ErrorMessage = "Оберіть, ким ви будете на платформі")]
    public string? Role { get; set; }

    [Required(ErrorMessage = "Введіть e-mail")]
    [EmailAddress(ErrorMessage = "Перевірте адресу — схоже, в ній помилка")]
    public string Email { get; set; } = string.Empty;

    public bool IsExecutor => string.Equals(Role, AppRoles.Executor, StringComparison.Ordinal);
}

public sealed class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Введіть e-mail")]
    [EmailAddress(ErrorMessage = "Перевірте адресу — схоже, в ній помилка")]
    public string Email { get; set; } = string.Empty;
}
public sealed class VerifyCodeViewModel
{
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введіть код із листа")]
    public string Code { get; set; } = string.Empty;

    public int ResendAfterSeconds { get; set; }

    public int LifetimeMinutes { get; set; } = 5;
}

public sealed class ResetPasswordViewModel
{
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введіть новий пароль")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Повторіть новий пароль")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Паролі не збігаються")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class RegisterProfileViewModel
{
    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public bool IsExecutor => string.Equals(Role, AppRoles.Executor, StringComparison.Ordinal);

    public bool IsCompany { get; set; }

    [Required(ErrorMessage = "Введіть імʼя")]
    [StringLength(100, ErrorMessage = "Імʼя задовге")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введіть прізвище")]
    [StringLength(100, ErrorMessage = "Прізвище задовге")]
    public string LastName { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "Назва компанії задовга")]
    public string? CompanyName { get; set; }

    [Required(ErrorMessage = "Оберіть місто")]
    public string? City { get; set; }

    [StringLength(10, ErrorMessage = "Індекс складається з пʼяти цифр")]
    public string? PostalCode { get; set; }

    [Required(ErrorMessage = "Вкажіть дату народження")]
    [DataType(DataType.Date)]
    public DateTime? BirthDate { get; set; }

    public string? Gender { get; set; }

    public string? Experience { get; set; }

    public int? SpecializationCategoryId { get; set; }

    [StringLength(1000, ErrorMessage = "Опис задовгий — лишіть до 1000 символів")]
    public string? About { get; set; }

    [Required(ErrorMessage = "Придумайте пароль")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Повторіть пароль")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Паролі не збігаються")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public bool NotifyByEmail { get; set; } = true;
    public bool NotifyBySms { get; set; }
    public bool NotifyByPush { get; set; }

    [Range(typeof(bool), "true", "true", ErrorMessage = "Погодьтеся з умовами, щоб продовжити")]
    public bool AcceptTerms { get; set; }

    public IReadOnlyList<PublicCategoryItem> Directions { get; set; } = Array.Empty<PublicCategoryItem>();
}
