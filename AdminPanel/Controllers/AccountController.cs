using AdminPanel.Models;
using BLL.Admin.Interfaces;
using Domain.Common;
using Domain.Entities;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AdminPanel.Controllers
{
    [AllowAnonymous]
    public class AccountController : AdminControllerBase
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditLogService _audit;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            IAuditLogService audit)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _audit = audit;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null || user.IsDeleted)
            {
                // Повідомлення однакове в обох випадках: не можна підказувати, що саме введено неправильно.
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                await LogFailureAsync(model.Email, "Unknown or deleted account");
                return View(model);
            }

            var isStaff = false;
            foreach (var role in AppRoles.StaffRoles)
            {
                if (await _userManager.IsInRoleAsync(user, role))
                {
                    isStaff = true;
                    break;
                }
            }

            if (!isStaff)
            {
                ModelState.AddModelError(string.Empty, "This account does not have access to the admin panel.");
                await LogFailureAsync(model.Email, "Account without a staff role");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty,
                    "This account is temporarily locked. Try again later or contact another administrator.");
                await LogFailureAsync(model.Email, "Locked out");
                return View(model);
            }

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                await LogFailureAsync(model.Email, "Wrong password");
                return View(model);
            }

            await _audit.WriteAsync(
                AuditAction.Login, "Account", user.Id, "Signed in to the admin panel",
                user.Id, user.Email ?? user.UserName ?? user.Id, ClientIp);

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectToAction("Index", "Dashboard");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _audit.WriteAsync(
                AuditAction.Logout, "Account", CurrentUserId, "Signed out",
                CurrentUserId, CurrentUserName, ClientIp);

            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult AccessDenied(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        private Task LogFailureAsync(string email, string reason) =>
            _audit.WriteAsync(
                AuditAction.LoginFailed, "Account", null,
                $"Failed sign-in for \"{email}\": {reason}",
                null, email, ClientIp, AuditSeverity.Warning);
    }
}
