using BLL.Admin.Interfaces;
using BLL.Admin.Models;
using DAL.Context;
using Domain.Entities;
using Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BLL.Admin.Services
{
    // Адміністрування користувачів. Блокування зроблено через поля блокування Identity, а не через
    // власний прапорець, тому заблокований користувач не зайде й через мобільний API.
    public class AdminUserService : IAdminUserService
    {
        // Прийнятий в Identity спосіб позначити «заблоковано назавжди».
        private static readonly DateTimeOffset PermanentLockout = new(new DateTime(9999, 12, 31, 0, 0, 0, DateTimeKind.Utc));

        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminUserService(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<PagedResult<AdminUserListItem>> GetUsersAsync(
            UserFilter filter,
            CancellationToken cancellationToken = default)
        {
            var query = BuildQuery(filter);
            var page = await Project(query, filter).ToPagedResultAsync(filter.Page, filter.PageSize, cancellationToken);

            await AttachRolesAsync(page.Items, cancellationToken);
            return page;
        }

        public async Task<IReadOnlyList<AdminUserListItem>> GetUsersForExportAsync(
            UserFilter filter,
            CancellationToken cancellationToken = default)
        {
            var items = await Project(BuildQuery(filter), filter)
                .Take(10_000)
                .ToListAsync(cancellationToken);

            await AttachRolesAsync(items, cancellationToken);
            return items;
        }

        public async Task<AdminUserDetails?> GetUserAsync(string id, CancellationToken cancellationToken = default)
        {
            var user = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

            if (user == null)
            {
                return null;
            }

            var details = new AdminUserDetails
            {
                Id = user.Id,
                DisplayName = BuildDisplayName(user.FirstName, user.LastName, user.FullName, user.Email),
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                AvatarUrl = user.AvatarUrl,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                IsDeleted = user.IsDeleted,
                EmailConfirmed = user.EmailConfirmed,
                LockoutEnd = user.LockoutEnd,
                OrdersAsCustomer = await _db.Orders.CountAsync(o => o.CustomerId == id, cancellationToken),
                OrdersAsExecutor = await _db.Orders.CountAsync(o => o.ExecutorId == id, cancellationToken),
                ReviewsWritten = await _db.Reviews.CountAsync(r => r.AuthorId == id, cancellationToken),
                ReviewsReceived = await _db.Reviews.CountAsync(r => r.TargetUserId == id, cancellationToken),
                ComplaintsAgainst = await _db.Complaints.CountAsync(c => c.TargetUserId == id, cancellationToken)
            };

            details.TotalSpent = await _db.Payments
                .Where(p => p.UserId == id && p.Status == PaymentStatus.Completed)
                .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;

            details.TotalEarned = await _db.Orders
                .Where(o => o.ExecutorId == id && o.Status == OrderStatus.Completed)
                .SumAsync(o => (decimal?)o.Price, cancellationToken) ?? 0m;

            details.AverageRating = details.ReviewsReceived == 0
                ? null
                : Math.Round(await _db.Reviews
                    .Where(r => r.TargetUserId == id)
                    .AverageAsync(r => (double)r.Rating, cancellationToken), 2);

            details.Roles = await GetRolesOfUserAsync(id, cancellationToken);

            details.RecentOrders = await _db.Orders
                .AsNoTracking()
                .Where(o => o.CustomerId == id || o.ExecutorId == id)
                .OrderByDescending(o => o.CreatedAt)
                .Take(10)
                .Select(o => new AdminOrderListItem
                {
                    Id = o.Id,
                    Title = o.Title,
                    Price = o.Price,
                    Status = o.Status,
                    CategoryId = o.CategoryId,
                    CategoryName = o.Category.Name,
                    CustomerId = o.CustomerId,
                    CustomerName = o.Customer.FirstName + " " + o.Customer.LastName,
                    ExecutorId = o.ExecutorId,
                    ExecutorName = o.Executor == null ? null : o.Executor.FirstName + " " + o.Executor.LastName,
                    CreatedAt = o.CreatedAt
                })
                .ToListAsync(cancellationToken);

            details.RecentReviews = await _db.Reviews
                .AsNoTracking()
                .Where(r => r.TargetUserId == id)
                .OrderByDescending(r => r.CreatedAt)
                .Take(5)
                .Select(r => new AdminReviewListItem
                {
                    Id = r.Id,
                    OrderId = r.OrderId,
                    OrderTitle = r.Order.Title,
                    AuthorId = r.AuthorId,
                    AuthorName = r.Author.FirstName + " " + r.Author.LastName,
                    TargetUserId = r.TargetUserId,
                    TargetUserName = r.TargetUser.FirstName + " " + r.TargetUser.LastName,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync(cancellationToken);

            details.RecentComplaints = await _db.Complaints
                .AsNoTracking()
                .Where(c => c.TargetUserId == id)
                .OrderByDescending(c => c.CreatedAt)
                .Take(5)
                .Select(c => new AdminComplaintListItem
                {
                    Id = c.Id,
                    SenderId = c.SenderId,
                    SenderName = c.Sender.FirstName + " " + c.Sender.LastName,
                    TargetUserId = c.TargetUserId,
                    TargetUserName = c.TargetUser == null ? null : c.TargetUser.FirstName + " " + c.TargetUser.LastName,
                    Reason = c.Reason,
                    Description = c.Description,
                    Status = c.Status,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync(cancellationToken);

            return details;
        }

        public async Task<AdminOperationResult> BlockAsync(
            string id,
            DateTimeOffset? until,
            CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return AdminOperationResult.Fail("User not found.");
            }

            await _userManager.SetLockoutEnabledAsync(user, true);
            var result = await _userManager.SetLockoutEndDateAsync(user, until ?? PermanentLockout);

            return result.Succeeded
                ? AdminOperationResult.Ok(until.HasValue
                    ? $"User blocked until {until.Value.UtcDateTime:yyyy-MM-dd HH:mm} UTC."
                    : "User blocked indefinitely.")
                : AdminOperationResult.Fail(Describe(result));
        }

        public async Task<AdminOperationResult> UnblockAsync(string id, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return AdminOperationResult.Fail("User not found.");
            }

            var result = await _userManager.SetLockoutEndDateAsync(user, null);
            if (!result.Succeeded)
            {
                return AdminOperationResult.Fail(Describe(result));
            }

            await _userManager.ResetAccessFailedCountAsync(user);
            return AdminOperationResult.Ok("User unblocked.");
        }

        public async Task<AdminOperationResult> SoftDeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
            if (user == null)
            {
                return AdminOperationResult.Fail("User not found.");
            }

            if (user.IsDeleted)
            {
                return AdminOperationResult.Fail("User is already deleted.");
            }

            user.IsDeleted = true;
            user.UpdatedAt = DateTime.UtcNow;
            user.LockoutEnabled = true;
            user.LockoutEnd = PermanentLockout;

            await _db.SaveChangesAsync(cancellationToken);
            return AdminOperationResult.Ok("User deleted. Their orders and reviews were kept.");
        }

        public async Task<AdminOperationResult> RestoreAsync(string id, CancellationToken cancellationToken = default)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
            if (user == null)
            {
                return AdminOperationResult.Fail("User not found.");
            }

            user.IsDeleted = false;
            user.UpdatedAt = DateTime.UtcNow;
            user.LockoutEnd = null;

            await _db.SaveChangesAsync(cancellationToken);
            return AdminOperationResult.Ok("User restored.");
        }

        public async Task<IReadOnlyList<string>> GetAllRolesAsync(CancellationToken cancellationToken = default)
        {
            return await _roleManager.Roles
                .AsNoTracking()
                .Select(r => r.Name!)
                .OrderBy(name => name)
                .ToListAsync(cancellationToken);
        }

        public async Task<AdminOperationResult> SetRolesAsync(
            string id,
            IEnumerable<string> roles,
            CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return AdminOperationResult.Fail("User not found.");
            }

            var requested = roles
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Select(r => r.Trim())
                .Distinct()
                .ToList();

            var known = await GetAllRolesAsync(cancellationToken);
            var unknown = requested.Except(known, StringComparer.OrdinalIgnoreCase).ToList();
            if (unknown.Count > 0)
            {
                return AdminOperationResult.Fail($"Unknown role(s): {string.Join(", ", unknown)}.");
            }

            var current = await _userManager.GetRolesAsync(user);

            var toRemove = current.Except(requested, StringComparer.OrdinalIgnoreCase).ToList();
            if (toRemove.Count > 0)
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, toRemove);
                if (!removeResult.Succeeded)
                {
                    return AdminOperationResult.Fail(Describe(removeResult));
                }
            }

            var toAdd = requested.Except(current, StringComparer.OrdinalIgnoreCase).ToList();
            if (toAdd.Count > 0)
            {
                var addResult = await _userManager.AddToRolesAsync(user, toAdd);
                if (!addResult.Succeeded)
                {
                    return AdminOperationResult.Fail(Describe(addResult));
                }
            }

            return AdminOperationResult.Ok(requested.Count == 0
                ? "All roles removed."
                : $"Roles updated: {string.Join(", ", requested)}.");
        }

        // -----------------------------------------------------------------
        // Внутрішня кухня
        // -----------------------------------------------------------------

        private IQueryable<ApplicationUser> BuildQuery(UserFilter filter)
        {
            var query = _db.Users.AsNoTracking();
            var now = DateTimeOffset.UtcNow;

            query = filter.State switch
            {
                UserState.Active => query.Where(u => !u.IsDeleted && (u.LockoutEnd == null || u.LockoutEnd <= now)),
                UserState.Blocked => query.Where(u => !u.IsDeleted && u.LockoutEnd != null && u.LockoutEnd > now),
                UserState.Deleted => query.Where(u => u.IsDeleted),
                _ => query
            };

            if (filter.HasSearch)
            {
                var term = filter.NormalizedSearch;
                query = query.Where(u =>
                    (u.Email != null && u.Email.Contains(term)) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(term)) ||
                    u.FirstName.Contains(term) ||
                    u.LastName.Contains(term) ||
                    (u.FullName != null && u.FullName.Contains(term)));
            }

            if (filter.From.HasValue)
            {
                query = query.Where(u => u.CreatedAt >= filter.From.Value.Date);
            }

            if (filter.ToInclusive.HasValue)
            {
                query = query.Where(u => u.CreatedAt < filter.ToInclusive.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Role))
            {
                var role = filter.Role;
                query = query.Where(u => _db.UserRoles
                    .Where(ur => ur.UserId == u.Id)
                    .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
                    .Any(name => name == role));
            }

            return query;
        }

        private static IQueryable<AdminUserListItem> Project(IQueryable<ApplicationUser> query, UserFilter filter)
        {
            var sorted = (filter.SortBy?.ToLowerInvariant(), filter.SortDesc) switch
            {
                ("name", false) => query.OrderBy(u => u.FirstName).ThenBy(u => u.LastName),
                ("name", true) => query.OrderByDescending(u => u.FirstName).ThenByDescending(u => u.LastName),
                ("email", false) => query.OrderBy(u => u.Email),
                ("email", true) => query.OrderByDescending(u => u.Email),
                (_, false) => query.OrderBy(u => u.CreatedAt),
                _ => query.OrderByDescending(u => u.CreatedAt)
            };

            return sorted.Select(u => new AdminUserListItem
            {
                Id = u.Id,
                DisplayName =
                    (u.FirstName + " " + u.LastName).Trim() == string.Empty
                        ? (u.FullName ?? u.Email ?? u.Id)
                        : (u.FirstName + " " + u.LastName),
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                AvatarUrl = u.AvatarUrl,
                CreatedAt = u.CreatedAt,
                IsDeleted = u.IsDeleted,
                EmailConfirmed = u.EmailConfirmed,
                LockoutEnd = u.LockoutEnd,
                OrdersAsCustomer = u.CreatedOrders.Count,
                OrdersAsExecutor = u.ExecutedOrders.Count
            });
        }

        // Ролі для поточної сторінки завантажуються одним запитом, а не окремо для кожного рядка,
        // тому список користувачів завжди коштує два звернення до бази, скільки б рядків не було.
        private async Task AttachRolesAsync(IReadOnlyList<AdminUserListItem> items, CancellationToken cancellationToken)
        {
            if (items.Count == 0)
            {
                return;
            }

            var ids = items.Select(i => i.Id).ToList();

            var pairs = await _db.UserRoles
                .Where(ur => ids.Contains(ur.UserId))
                .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, RoleName = r.Name! })
                .ToListAsync(cancellationToken);

            var byUser = pairs
                .GroupBy(p => p.UserId)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(x => x.RoleName).OrderBy(x => x).ToList());

            foreach (var item in items)
            {
                item.Roles = byUser.TryGetValue(item.Id, out var roles) ? roles : Array.Empty<string>();
            }
        }

        private async Task<IReadOnlyList<string>> GetRolesOfUserAsync(string userId, CancellationToken cancellationToken)
        {
            return await _db.UserRoles
                .Where(ur => ur.UserId == userId)
                .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name!)
                .OrderBy(name => name)
                .ToListAsync(cancellationToken);
        }

        private static string BuildDisplayName(string firstName, string lastName, string? fullName, string? email)
        {
            var name = $"{firstName} {lastName}".Trim();
            if (!string.IsNullOrWhiteSpace(name))
            {
                return name;
            }

            return !string.IsNullOrWhiteSpace(fullName) ? fullName! : email ?? "—";
        }

        private static string Describe(IdentityResult result) =>
            string.Join(" ", result.Errors.Select(e => e.Description));
    }
}
