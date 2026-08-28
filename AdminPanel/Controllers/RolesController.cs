using AdminPanel.Models;
using BLL.Admin.Interfaces;
using BLL.Admin.Models;
using DAL.Context;
using Domain.Common;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdminPanel.Controllers
{
    [Authorize(Policy = AppRoles.AdminOnlyPolicy)]
    public class RolesController : AdminControllerBase
    {
        private static readonly Dictionary<string, string> RoleDescriptions = new(StringComparer.OrdinalIgnoreCase)
        {
            [AppRoles.Admin] = "Full access: users, orders, payments, roles and the activity log.",
            [AppRoles.Moderator] = "Users, orders, categories, complaints and reviews. No payments or roles.",
            [AppRoles.Support] = "Read-only access plus the support chat.",
            [AppRoles.Customer] = "Platform user who places orders. No access to the admin panel.",
            [AppRoles.Executor] = "Platform user who performs orders. No access to the admin panel."
        };

        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _db;
        private readonly IAdminUserService _users;
        private readonly IAuditLogService _audit;

        public RolesController(
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext db,
            IAdminUserService users,
            IAuditLogService audit)
        {
            _roleManager = roleManager;
            _db = db;
            _users = users;
            _audit = audit;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var counts = await _db.UserRoles
                .GroupBy(ur => ur.RoleId)
                .Select(g => new { RoleId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.RoleId, x => x.Count, cancellationToken);

            var roles = await _roleManager.Roles
                .AsNoTracking()
                .OrderBy(r => r.Name)
                .ToListAsync(cancellationToken);

            var rows = roles.Select(r => new RoleRow
            {
                Name = r.Name ?? string.Empty,
                UsersCount = counts.TryGetValue(r.Id, out var count) ? count : 0,
                Description = RoleDescriptions.TryGetValue(r.Name ?? string.Empty, out var description)
                    ? description
                    : "Custom role.",
                IsStaffRole = AppRoles.StaffRoles.Contains(r.Name, StringComparer.OrdinalIgnoreCase)
            }).ToList();

            // Усі, хто має доступ до панелі, — щоб права можна було переглянути одним поглядом.
            var staff = new List<AdminUserListItem>();
            foreach (var role in AppRoles.StaffRoles)
            {
                var page = await _users.GetUsersAsync(
                    new UserFilter { Role = role, PageSize = 100 }, cancellationToken);

                staff.AddRange(page.Items.Where(u => staff.All(s => s.Id != u.Id)));
            }

            return View(new RolesViewModel
            {
                Roles = rows,
                Staff = staff.OrderBy(s => s.DisplayName).ToList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string name, CancellationToken cancellationToken)
        {
            name = (name ?? string.Empty).Trim();

            if (string.IsNullOrEmpty(name))
            {
                FlashError("Role name is required.");
                return RedirectToAction(nameof(Index));
            }

            if (await _roleManager.RoleExistsAsync(name))
            {
                FlashError($"Role \"{name}\" already exists.");
                return RedirectToAction(nameof(Index));
            }

            var result = await _roleManager.CreateAsync(new IdentityRole(name));

            if (result.Succeeded)
            {
                FlashSuccess($"Role \"{name}\" created.");
                await AuditAsync(_audit, AuditAction.Create, "Role", name,
                    $"Created role \"{name}\"", AuditSeverity.Warning);
            }
            else
            {
                FlashError(string.Join(" ", result.Errors.Select(e => e.Description)));
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string name, CancellationToken cancellationToken)
        {
            if (AppRoles.All.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                FlashError($"\"{name}\" is a built-in role and cannot be deleted.");
                return RedirectToAction(nameof(Index));
            }

            var role = await _roleManager.FindByNameAsync(name);
            if (role == null)
            {
                FlashError("Role not found.");
                return RedirectToAction(nameof(Index));
            }

            var inUse = await _db.UserRoles.AnyAsync(ur => ur.RoleId == role.Id, cancellationToken);
            if (inUse)
            {
                FlashError($"Role \"{name}\" is still assigned to at least one user.");
                return RedirectToAction(nameof(Index));
            }

            var result = await _roleManager.DeleteAsync(role);

            if (result.Succeeded)
            {
                FlashSuccess($"Role \"{name}\" deleted.");
                await AuditAsync(_audit, AuditAction.Delete, "Role", name,
                    $"Deleted role \"{name}\"", AuditSeverity.Critical);
            }
            else
            {
                FlashError(string.Join(" ", result.Errors.Select(e => e.Description)));
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
