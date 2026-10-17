using BLL.Admin.Interfaces;
using BLL.Admin.Models;
using DAL.Context;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BLL.Admin.Services
{
    // Журнал дій, у який можна лише дописувати. Запис у журнал ніколи не має ламати дію, що його
    // спричинила, тому помилки свідомо ковтаються: краще втратити рядок журналу, ніж рішення модератора.
    public class AuditLogService : IAuditLogService
    {
        private const int MaxDescriptionLength = 1000;

        private readonly ApplicationDbContext _db;

        public AuditLogService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task WriteAsync(
            AuditAction action,
            string entityName,
            string? entityId,
            string? description,
            string? userId,
            string userName,
            string? ipAddress,
            AuditSeverity severity = AuditSeverity.Information,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Зовнішній ключ необов'язковий: застарілий ідентифікатор (видалений адміністратор) не має заважати запису.
                var actorExists = userId != null &&
                                  await _db.Users.AnyAsync(u => u.Id == userId, cancellationToken);

                var entry = new AuditLog
                {
                    CreatedAt = DateTime.UtcNow,
                    Action = action,
                    EntityName = Truncate(entityName, 100),
                    EntityId = Truncate(entityId, 450),
                    Description = Truncate(description, MaxDescriptionLength),
                    UserId = actorExists ? userId : null,
                    UserName = Truncate(string.IsNullOrWhiteSpace(userName) ? "system" : userName, 256)!,
                    IpAddress = Truncate(ipAddress, 64),
                    Severity = severity
                };

                _db.AuditLogs.Add(entry);
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception)
            {
                // Від'єднуємо невдалий запис, щоб він не зіпсував наступний SaveChanges викликаючого коду.
                foreach (var tracked in _db.ChangeTracker.Entries<AuditLog>().ToList())
                {
                    tracked.State = EntityState.Detached;
                }
            }
        }

        public async Task<PagedResult<AdminAuditLogListItem>> GetLogsAsync(
            AuditLogFilter filter,
            CancellationToken cancellationToken = default)
        {
            return await Project(BuildQuery(filter), filter)
                .ToPagedResultAsync(filter.Page, filter.PageSize, cancellationToken);
        }

        public async Task<IReadOnlyList<AdminAuditLogListItem>> GetLogsForExportAsync(
            AuditLogFilter filter,
            CancellationToken cancellationToken = default)
        {
            return await Project(BuildQuery(filter), filter)
                .Take(10_000)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<string>> GetEntityNamesAsync(CancellationToken cancellationToken = default)
        {
            return await _db.AuditLogs
                .AsNoTracking()
                .Select(a => a.EntityName)
                .Distinct()
                .OrderBy(name => name)
                .ToListAsync(cancellationToken);
        }

        // -----------------------------------------------------------------

        private IQueryable<AuditLog> BuildQuery(AuditLogFilter filter)
        {
            var query = _db.AuditLogs.AsNoTracking();

            if (filter.Action.HasValue)
            {
                query = query.Where(a => a.Action == filter.Action.Value);
            }

            if (filter.Severity.HasValue)
            {
                query = query.Where(a => a.Severity == filter.Severity.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.EntityName))
            {
                query = query.Where(a => a.EntityName == filter.EntityName);
            }

            if (filter.From.HasValue)
            {
                query = query.Where(a => a.CreatedAt >= filter.From.Value.Date);
            }

            if (filter.ToInclusive.HasValue)
            {
                query = query.Where(a => a.CreatedAt < filter.ToInclusive.Value);
            }

            if (filter.HasSearch)
            {
                var term = filter.NormalizedSearch;
                query = query.Where(a =>
                    a.UserName.Contains(term) ||
                    (a.Description != null && a.Description.Contains(term)) ||
                    (a.EntityId != null && a.EntityId.Contains(term)) ||
                    a.EntityName.Contains(term));
            }

            return query;
        }

        private static IQueryable<AdminAuditLogListItem> Project(IQueryable<AuditLog> query, AuditLogFilter filter)
        {
            var sorted = filter.SortDesc
                ? query.OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.Id)
                : query.OrderBy(a => a.CreatedAt).ThenBy(a => a.Id);

            return sorted.Select(a => new AdminAuditLogListItem
            {
                Id = a.Id,
                CreatedAt = a.CreatedAt,
                UserId = a.UserId,
                UserName = a.UserName,
                Action = a.Action,
                EntityName = a.EntityName,
                EntityId = a.EntityId,
                Description = a.Description,
                IpAddress = a.IpAddress,
                Severity = a.Severity
            });
        }

        private static string? Truncate(string? value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return value.Length <= maxLength ? value : value[..maxLength];
        }
    }
}
