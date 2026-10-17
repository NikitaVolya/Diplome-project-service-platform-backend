using AdminPanel.Models;
using BLL.Admin.Interfaces;
using BLL.Admin.Models;
using Domain.Common;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminPanel.Controllers
{
    [Authorize(Policy = AppRoles.ModerationPolicy)]
    public class UsersController : AdminControllerBase
    {
        private readonly IAdminUserService _users;
        private readonly IExcelExportService _excel;
        private readonly IAuditLogService _audit;

        public UsersController(IAdminUserService users, IExcelExportService excel, IAuditLogService audit)
        {
            _users = users;
            _excel = excel;
            _audit = audit;
        }

        public async Task<IActionResult> Index([FromQuery] UserFilter filter, CancellationToken cancellationToken)
        {
            var model = new UsersViewModel
            {
                Filter = filter,
                Result = await _users.GetUsersAsync(filter, cancellationToken),
                Roles = await _users.GetAllRolesAsync(cancellationToken)
            };

            return View(model);
        }

        public async Task<IActionResult> Details(string id, CancellationToken cancellationToken)
        {
            var user = await _users.GetUserAsync(id, cancellationToken);
            if (user == null)
            {
                FlashError("User not found.");
                return RedirectToAction(nameof(Index));
            }

            var model = new UserDetailsViewModel
            {
                User = user,
                AllRoles = await _users.GetAllRolesAsync(cancellationToken),
                CanManageRoles = User.IsInRole(AppRoles.Admin)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Block(string id, int? days, string? returnUrl, CancellationToken cancellationToken)
        {
            DateTimeOffset? until = days is > 0 ? DateTimeOffset.UtcNow.AddDays(days.Value) : null;

            var result = await _users.BlockAsync(id, until, cancellationToken);
            Flash(result);
            await AuditAsync(_audit, AuditAction.Block, "User", id, result, AuditSeverity.Warning);

            return RedirectBack(returnUrl, id);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unblock(string id, string? returnUrl, CancellationToken cancellationToken)
        {
            var result = await _users.UnblockAsync(id, cancellationToken);
            Flash(result);
            await AuditAsync(_audit, AuditAction.Unblock, "User", id, result);

            return RedirectBack(returnUrl, id);
        }

        [HttpPost]
        [Authorize(Policy = AppRoles.AdminOnlyPolicy)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id, string? returnUrl, CancellationToken cancellationToken)
        {
            if (string.Equals(id, CurrentUserId, StringComparison.Ordinal))
            {
                FlashError("You cannot delete your own account.");
                return RedirectBack(returnUrl, id);
            }

            var result = await _users.SoftDeleteAsync(id, cancellationToken);
            Flash(result);
            await AuditAsync(_audit, AuditAction.Delete, "User", id, result, AuditSeverity.Critical);

            return RedirectBack(returnUrl, id);
        }

        [HttpPost]
        [Authorize(Policy = AppRoles.AdminOnlyPolicy)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(string id, string? returnUrl, CancellationToken cancellationToken)
        {
            var result = await _users.RestoreAsync(id, cancellationToken);
            Flash(result);
            await AuditAsync(_audit, AuditAction.Update, "User", id, result);

            return RedirectBack(returnUrl, id);
        }

        [HttpPost]
        [Authorize(Policy = AppRoles.AdminOnlyPolicy)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetRoles(string id, string[]? roles, CancellationToken cancellationToken)
        {
            var requested = roles ?? Array.Empty<string>();

            // Захист від ситуації, коли адміністратор сам забирає в себе доступ до панелі.
            if (string.Equals(id, CurrentUserId, StringComparison.Ordinal) &&
                !requested.Contains(AppRoles.Admin, StringComparer.OrdinalIgnoreCase))
            {
                FlashError("You cannot remove the Admin role from your own account.");
                return RedirectToAction(nameof(Details), new { id });
            }

            var result = await _users.SetRolesAsync(id, requested, cancellationToken);
            Flash(result);
            await AuditAsync(_audit, AuditAction.RoleChange, "User", id, result, AuditSeverity.Warning);

            return RedirectToAction(nameof(Details), new { id });
        }

        public async Task<IActionResult> Export([FromQuery] UserFilter filter, CancellationToken cancellationToken)
        {
            var users = await _users.GetUsersForExportAsync(filter, cancellationToken);

            var headers = new[]
            {
                "Id", "Name", "Email", "Phone", "Roles", "Status",
                "Orders as customer", "Orders as executor", "Email confirmed", "Registered"
            };

            var rows = users.Select(u => (IReadOnlyList<object?>)new object?[]
            {
                u.Id,
                u.DisplayName,
                u.Email,
                u.PhoneNumber,
                string.Join(", ", u.Roles),
                u.StatusLabel,
                u.OrdersAsCustomer,
                u.OrdersAsExecutor,
                u.EmailConfirmed,
                u.CreatedAt
            });

            var file = _excel.Build("Users", headers, rows);

            await AuditAsync(_audit, AuditAction.Export, "User", null,
                $"Exported {users.Count} user(s) to Excel");

            return Xlsx(file, "servicehub-users");
        }

        private IActionResult RedirectBack(string? returnUrl, string id)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
