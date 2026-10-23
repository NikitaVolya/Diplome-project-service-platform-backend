using AdminPanel.Localization;
using AdminPanel.Models;
using BLL.Admin.Interfaces;
using BLL.Public;
using BLL.Services.Interfaces;
using Domain.Common;
using Domain.Entities;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AdminPanel.Controllers;

[AllowAnonymous]
public sealed class AuthController : Controller
{
    
    public const int RegistrationSteps = 3;

    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPublicCatalogService _catalog;
    private readonly IVerificationCodes _codes;
    private readonly IAuditLogService _audit;
    private readonly ILogger<AuthController> _logger;
    private readonly IEmailService _emailService;

    public AuthController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IPublicCatalogService catalog,
        IVerificationCodes codes,
        IVerificationCodeSender sender,
        IAuditLogService audit,
        ILogger<AuthController> logger,
        IEmailService emailService)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _catalog = catalog;
        _codes = codes;
        _audit = audit;
        _logger = logger;
        _emailService = emailService;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Landing");
        }

        return View(new PublicLoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(PublicLoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);

        
        if (user == null || user.IsDeleted)
        {
            ModelState.AddModelError(string.Empty, Tr.T("Невірний e-mail або пароль."));
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty,
                Tr.T("Акаунт тимчасово заблоковано через невдалі спроби входу. Спробуйте за 15 хвилин."));
            return View(model);
        }

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, Tr.T("Невірний e-mail або пароль."));
            return View(model);
        }

        ThemePreference.Remember(Response, user.Theme);

        await _audit.WriteAsync(
            AuditAction.Login, "Account", user.Id, "Вхід на сайт",
            user.Id, user.Email ?? user.Id, HttpContext.Connection.RemoteIpAddress?.ToString());

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "Landing");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Landing");
    }

    [HttpGet]
    public IActionResult Register(string? role)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Landing");
        }

        // Роль приходить із вікна «Оберіть дію»; невідоме значення просто ігноруємо.
        var known = role == AppRoles.Customer || role == AppRoles.Executor;

        return View(new RegisterStepOneViewModel { Role = known ? role : null });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterStepOneViewModel model)
    {
        if (model.Role != AppRoles.Customer && model.Role != AppRoles.Executor)
        {
            ModelState.AddModelError(nameof(model.Role), Tr.T("Оберіть, ким ви будете на платформі"));
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var taken = await _userManager.FindByEmailAsync(model.Email);

        if (taken is { IsDeleted: false })
        {
            ModelState.AddModelError(nameof(model.Email),
                Tr.T("Такий e-mail уже зареєстрований. Спробуйте увійти або відновити пароль."));
            return View(model);
        }

        TempData["Auth.Email"] = model.Email;
        TempData["Auth.Role"] = model.Role;

        await IssueCodeAsync(model.Email, CodePurpose.Registration);

        return RedirectToAction(nameof(Confirm));
    }

    [HttpGet]
    public IActionResult Confirm()
    {
        var email = Pending("Auth.Email");

        if (email == null)
        {
            return RedirectToAction(nameof(Register));
        }

        return View(CodeModel(email));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Confirm(VerifyCodeViewModel model)
    {
        var email = Pending("Auth.Email");

        if (email == null)
        {
            return RedirectToAction(nameof(Register));
        }

        if (!_codes.Verify(email, CodePurpose.Registration, model.Code))
        {
            ModelState.AddModelError(nameof(model.Code), Tr.T("Код невірний або вже застарів."));
            Keep("Auth.Email", "Auth.Role");
            return View(CodeModel(email));
        }

        TempData["Auth.Email"] = email;
        TempData["Auth.Role"] = TempData.Peek("Auth.Role");
        TempData["Auth.EmailConfirmed"] = true;

        return RedirectToAction(nameof(About));
    }

    [HttpGet]
    public async Task<IActionResult> About()
    {
        var email = Pending("Auth.Email");

        if (email == null || TempData.Peek("Auth.EmailConfirmed") is not true)
        {
            return RedirectToAction(nameof(Register));
        }

        Keep("Auth.Email", "Auth.Role", "Auth.EmailConfirmed");

        var model = new RegisterProfileViewModel
        {
            Email = email,
            Role = TempData.Peek("Auth.Role") as string ?? AppRoles.Customer
        };

        return View(await WithDirectionsAsync(model));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> About(RegisterProfileViewModel model)
    {
        var email = Pending("Auth.Email");
        var role = TempData.Peek("Auth.Role") as string ?? AppRoles.Customer;

        if (email == null || TempData.Peek("Auth.EmailConfirmed") is not true)
        {
            return RedirectToAction(nameof(Register));
        }

        model.Email = email;
        model.Role = role;

        if (!RegistrationOptions.IsKnownCity(model.City))
        {
            ModelState.AddModelError(nameof(model.City), Tr.T("Оберіть місто зі списку"));
        }

        if (!RegistrationOptions.IsKnownGender(model.Gender))
        {
            ModelState.AddModelError(nameof(model.Gender), Tr.T("Оберіть стать зі списку"));
        }

        if (model.IsExecutor && !RegistrationOptions.IsKnownExperience(model.Experience))
        {
            ModelState.AddModelError(nameof(model.Experience), Tr.T("Оберіть досвід зі списку"));
        }

        if (model.BirthDate is { } born && (born > DateTime.UtcNow.AddYears(-14) || born < DateTime.UtcNow.AddYears(-100)))
        {
            ModelState.AddModelError(nameof(model.BirthDate), Tr.T("Перевірте дату народження"));
        }

        if (!ModelState.IsValid)
        {
            Keep("Auth.Email", "Auth.Role", "Auth.EmailConfirmed");
            return View(await WithDirectionsAsync(model));
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = model.FirstName.Trim(),
            LastName = model.LastName.Trim(),
            FullName = $"{model.FirstName.Trim()} {model.LastName.Trim()}",
            City = model.City,
            PostalCode = Clean(model.PostalCode),
            BirthDate = model.BirthDate,
            Gender = model.Gender,
            NotifyByEmail = model.NotifyByEmail,
            NotifyBySms = model.NotifyBySms,
            NotifyByPush = model.NotifyByPush
        };

        if (model.IsExecutor)
        {
            user.IsCompany = model.IsCompany;
            user.CompanyName = model.IsCompany ? Clean(model.CompanyName) : null;
            user.Experience = model.Experience;
            user.SpecializationCategoryId = model.SpecializationCategoryId;
            user.About = Clean(model.About);
        }

        var created = await _userManager.CreateAsync(user, model.Password);

        if (!created.Succeeded)
        {
            foreach (var error in created.Errors)
            {
                ModelState.AddModelError(string.Empty, Translate(error));
            }

            Keep("Auth.Email", "Auth.Role", "Auth.EmailConfirmed");
            return View(await WithDirectionsAsync(model));
        }

        await _userManager.AddToRoleAsync(user, role);

        await _audit.WriteAsync(
            AuditAction.Create, "Account", user.Id, $"Реєстрація на сайті ({role})",
            user.Id, user.Email ?? user.Id, HttpContext.Connection.RemoteIpAddress?.ToString());

        await _signInManager.SignInAsync(user, isPersistent: true);

        TempData["Auth.DoneRole"] = role;

        return RedirectToAction(nameof(Done));
    }

    [HttpGet]
    public IActionResult Done()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return RedirectToAction(nameof(Register));
        }

        var role = TempData.Peek("Auth.DoneRole") as string
                   ?? (User.IsInRole(AppRoles.Executor) ? AppRoles.Executor : AppRoles.Customer);

        TempData.Keep("Auth.DoneRole");

        return View((object)role);
    }

    private async Task<RegisterProfileViewModel> WithDirectionsAsync(RegisterProfileViewModel model)
    {
        if (model.IsExecutor)
        {
            model.Directions = await _catalog.GetPopularCategoriesAsync(24, HttpContext.RequestAborted);
        }

        return model;
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Resend(string purpose)
    {
        var email = Pending("Auth.Email");

        if (email == null)
        {
            return RedirectToAction(nameof(Register));
        }

        var kind = purpose == nameof(CodePurpose.PasswordReset)
            ? CodePurpose.PasswordReset
            : CodePurpose.Registration;

        await IssueCodeAsync(email, kind);

        return RedirectToAction(kind == CodePurpose.PasswordReset ? nameof(VerifyCode) : nameof(Confirm));
    }


    [HttpGet]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);

        if (user is { IsDeleted: false })
        {
            await IssueCodeAsync(model.Email, CodePurpose.PasswordReset);
        }
        else
        {
            _logger.LogInformation("Відновлення пароля для невідомої адреси {Email}", model.Email);
        }

        TempData["Auth.Email"] = model.Email;

        return RedirectToAction(nameof(VerifyCode));
    }

    [HttpGet]
    public IActionResult VerifyCode()
    {
        var email = Pending("Auth.Email");

        if (email == null)
        {
            return RedirectToAction(nameof(ForgotPassword));
        }

        return View(CodeModel(email));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName(nameof(VerifyCode))]
    public async Task<IActionResult> VerifyCodePost(VerifyCodeViewModel model)
    {
        var email = Pending("Auth.Email");

        if (email == null)
        {
            return RedirectToAction(nameof(ForgotPassword));
        }

        if (!_codes.Verify(email, CodePurpose.PasswordReset, model.Code))
        {
            ModelState.AddModelError(nameof(model.Code), Tr.T("Код невірний або вже застарів."));
            Keep("Auth.Email");
            return View(CodeModel(email));
        }

        var user = await _userManager.FindByEmailAsync(email);

        if (user == null || user.IsDeleted)
        {
            return RedirectToAction(nameof(ForgotPassword));
        }

        TempData["Auth.Email"] = email;
        TempData["Auth.ResetToken"] = await _userManager.GeneratePasswordResetTokenAsync(user);

        return RedirectToAction(nameof(ResetPassword));
    }

    [HttpGet]
    public IActionResult ResetPassword()
    {
        var email = Pending("Auth.Email");

        if (email == null || TempData.Peek("Auth.ResetToken") is not string)
        {
            return RedirectToAction(nameof(ForgotPassword));
        }

        Keep("Auth.Email", "Auth.ResetToken");

        return View(new ResetPasswordViewModel { Email = email });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName(nameof(ResetPassword))]
    public async Task<IActionResult> ResetPasswordPost(ResetPasswordViewModel model)
    {
        var email = Pending("Auth.Email");
        var token = TempData.Peek("Auth.ResetToken") as string;

        if (email == null || token == null)
        {
            return RedirectToAction(nameof(ForgotPassword));
        }

        if (!ModelState.IsValid)
        {
            Keep("Auth.Email", "Auth.ResetToken");
            return View(new ResetPasswordViewModel { Email = email });
        }

        var user = await _userManager.FindByEmailAsync(email);

        if (user == null || user.IsDeleted)
        {
            return RedirectToAction(nameof(ForgotPassword));
        }

        var result = await _userManager.ResetPasswordAsync(user, token, model.Password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, Translate(error));
            }

            Keep("Auth.Email", "Auth.ResetToken");
            return View(new ResetPasswordViewModel { Email = email });
        }

        await _userManager.ResetAccessFailedCountAsync(user);
        await _userManager.SetLockoutEndDateAsync(user, null);

        await _audit.WriteAsync(
            AuditAction.Update, "Account", user.Id, "Пароль змінено через відновлення",
            user.Id, user.Email ?? user.Id, HttpContext.Connection.RemoteIpAddress?.ToString());

        await _signInManager.SignInAsync(user, isPersistent: true);

        return RedirectToAction("Index", "Landing");
    }


    private async Task IssueCodeAsync(string email, CodePurpose purpose)
    {
        var ticket = _codes.Issue(email, purpose);

        if (!ticket.TooSoon)
        {
            await _emailService.SendCodeAsync(email, ticket.Code);
        }

        TempData["Auth.ResendAfter"] = (int)Math.Ceiling(ticket.ResendAfter.TotalSeconds);
        TempData["Auth.StubCode"] = ticket.Code;
    }

    private VerifyCodeViewModel CodeModel(string email)
    {
        var seconds = TempData.Peek("Auth.ResendAfter") as int? ?? 0;

        return new VerifyCodeViewModel
        {
            Email = email,
            ResendAfterSeconds = seconds
        };
    }


    [HttpGet]
    public IActionResult GoogleLogin(string? returnUrl = null)
    {
        var redirectUrl = Url.Action(
            nameof(GoogleResponse),
            "Auth",
            new { returnUrl }
        );

        var properties = _signInManager
            .ConfigureExternalAuthenticationProperties(
                "Google",
                redirectUrl
            );

        return Challenge(properties, "Google");
    }

    [HttpGet]
    public async Task<IActionResult> GoogleResponse(string? returnUrl = null)
    {
        var info = await _signInManager.GetExternalLoginInfoAsync();

        if (info == null || info.LoginProvider != "Google")
        {
            TempData["Auth.Error"] = "Помилка авторизації через Google.";
            return RedirectToAction(nameof(Login));
        }

        var user = await _userManager.FindByLoginAsync(
            info.LoginProvider,
            info.ProviderKey
        );

        if (user != null)
        {
            if (user.IsDeleted)
            {
                TempData["Auth.Error"] = "Обліковий запис видалено.";
                return RedirectToAction(nameof(Login));
            }

            var result = await _signInManager.ExternalLoginSignInAsync(
                info.LoginProvider,
                info.ProviderKey,
                isPersistent: false,
                bypassTwoFactor: false
            );

            if (result.IsLockedOut)
            {
                TempData["Auth.Error"] = "Обліковий запис заблоковано.";
                return RedirectToAction(nameof(Login));
            }

            if (result.RequiresTwoFactor)
            {
                TempData["Auth.Error"] =
                    "Потрібно завершити двофакторну перевірку.";
                return RedirectToAction(nameof(Login));
            }

            if (!result.Succeeded)
            {
                TempData["Auth.Error"] = "Не вдалося виконати вхід.";
                return RedirectToAction(nameof(Login));
            }

            await _audit.WriteAsync(
                AuditAction.Login,
                "Account",
                user.Id,
                "Вхід через Google",
                user.Id,
                user.Email ?? user.Id,
                HttpContext.Connection.RemoteIpAddress?.ToString()
            );

            return GoogleRedirect(returnUrl);
        }

        var email = info.Principal.FindFirstValue(ClaimTypes.Email);

        var verified = info.Principal.FindFirst("email_verified")?.Value;

        if (string.IsNullOrWhiteSpace(email))
        {
            TempData["Auth.Error"] =
                "Google не підтвердив електронну адресу.";
            return RedirectToAction(nameof(Login));
        }

        user = await _userManager.FindByEmailAsync(email);

        if (user != null)
        {
            if (user.IsDeleted || await _userManager.IsLockedOutAsync(user))
            {
                TempData["Auth.Error"] = "Обліковий запис недоступний.";
                return RedirectToAction(nameof(Login));
            }


            var linkштпResult = await _userManager.AddLoginAsync(user, info);

            if (!linkштпResult.Succeeded)
            {
                TempData["Auth.Error"] = "Не вдалося прив'язати Google.";
                return RedirectToAction(nameof(Login));
            }

            var signInResult = await _signInManager.ExternalLoginSignInAsync(
                info.LoginProvider,
                info.ProviderKey,
                isPersistent: false,
                bypassTwoFactor: false
            );

            if (signInResult.RequiresTwoFactor)
            {
                TempData["Auth.Error"] = "Потрібна двофакторна перевірка.";
                return RedirectToAction(nameof(Login));
            }

            if (!signInResult.Succeeded)
            {
                TempData["Auth.Error"] = "Не вдалося виконати вхід.";
                return RedirectToAction(nameof(Login));
            }

            await _audit.WriteAsync(
                AuditAction.Login,
                "Account",
                user.Id,
                "Google прив'язано автоматично. Вхід через Google",
                user.Id,
                user.Email ?? user.Id,
                HttpContext.Connection.RemoteIpAddress?.ToString()
            );

            return GoogleRedirect(returnUrl);
        }

        var firstName = info.Principal
            .FindFirstValue(ClaimTypes.GivenName) ?? "";

        var lastName = info.Principal
            .FindFirstValue(ClaimTypes.Surname) ?? "";

        user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName,
            FullName = $"{firstName} {lastName}".Trim(),
            CreatedAt = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(user);

        if (!createResult.Succeeded)
        {
            _logger.LogWarning(
                "Google registration failed: {Errors}",
                string.Join("; ", createResult.Errors.Select(e => e.Description))
            );

            TempData["Auth.Error"] = "Не вдалося створити обліковий запис.";
            return RedirectToAction(nameof(Login));
        }

        var roleResult = await _userManager.AddToRoleAsync(
            user,
            AppRoles.Customer
        );

        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);

            TempData["Auth.Error"] = "Не вдалося призначити роль.";
            return RedirectToAction(nameof(Login));
        }

        var linkResult = await _userManager.AddLoginAsync(user, info);

        if (!linkResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);

            TempData["Auth.Error"] = "Не вдалося прив'язати Google.";
            return RedirectToAction(nameof(Login));
        }

        if (!await _signInManager.CanSignInAsync(user) ||
            await _userManager.IsLockedOutAsync(user))
        {
            TempData["Auth.Error"] = "Вхід для цього акаунта недоступний.";
            return RedirectToAction(nameof(Login));
        }

        await _signInManager.SignInAsync(
            user,
            isPersistent: false,
            authenticationMethod: "Google"
        );

        await _audit.WriteAsync(
            AuditAction.Create,
            "Account",
            user.Id,
            "Реєстрація через Google (Customer)",
            user.Id,
            user.Email ?? user.Id,
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );

        return GoogleRedirect(returnUrl);
    }

    private IActionResult GoogleRedirect(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) &&
            Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction("Index", "Landing");
    }


    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult LinkGoogle()
    {
        var redirectUrl = Url.Action(
            nameof(LinkGoogleResponse),
            "Auth"
        );

        var properties = _signInManager
            .ConfigureExternalAuthenticationProperties(
                "Google",
                redirectUrl,
                _userManager.GetUserId(User)
            );

        return Challenge(properties, "Google");
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> LinkGoogleResponse()
    {
        var currentUser = await _userManager.GetUserAsync(User);

        if (currentUser == null || currentUser.IsDeleted ||
            await _userManager.IsLockedOutAsync(currentUser))
        {
            return Forbid();
        }

        var info = await _signInManager.GetExternalLoginInfoAsync(
            currentUser.Id
        );

        if (info == null || info.LoginProvider != "Google")
        {
            TempData["Auth.Error"] = "Помилка прив'язки Google.";
            return RedirectToAction("Index", "Landing");
        }

        var existingUser = await _userManager.FindByLoginAsync(
            info.LoginProvider,
            info.ProviderKey
        );

        if (existingUser != null)
        {
            TempData["Auth.Error"] =
                "Цей Google-акаунт уже прив'язаний.";

            return RedirectToAction("Index", "Landing");
        }

        var result = await _userManager.AddLoginAsync(
            currentUser,
            info
        );

        if (!result.Succeeded)
        {
            TempData["Auth.Error"] =
                "Не вдалося прив'язати Google.";

            return RedirectToAction("Index", "Landing");
        }

        await _signInManager.RefreshSignInAsync(currentUser);

        TempData["Auth.Success"] =
            "Google успішно прив'язаний до акаунта.";

        return RedirectToAction("Index", "Landing");
    }


    private string? Pending(string key) => TempData.Peek(key) as string;

    private void Keep(params string[] keys)
    {
        foreach (var key in keys)
        {
            TempData.Keep(key);
        }

        TempData.Keep("Auth.ResendAfter");
        TempData.Keep("Auth.StubCode");
    }

    private static string Translate(IdentityError error) => error.Code switch
    {
        "PasswordTooShort" => Tr.T("Пароль має містити щонайменше 8 символів."),
        "PasswordRequiresDigit" => Tr.T("Додайте до пароля хоча б одну цифру."),
        "PasswordRequiresUpper" => Tr.T("Додайте до пароля велику літеру."),
        "PasswordRequiresLower" => Tr.T("Додайте до пароля маленьку літеру."),
        "InvalidToken" => Tr.T("Посилання застаріло. Почніть відновлення спочатку."),
        _ => error.Description
    };
}
