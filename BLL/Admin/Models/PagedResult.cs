using Microsoft.EntityFrameworkCore;

namespace BLL.Admin.Models
{
    // Одна сторінка більшого набору результатів разом з усім, що потрібно для малювання пагінації.
    public class PagedResult<T>
    {
        public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 20;

        public int TotalCount { get; set; }

        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

        public bool HasPrevious => Page > 1;

        public bool HasNext => Page < TotalPages;

        // Номер першого рядка на цій сторінці (від 1) — для підпису «показано 21–40 зі 137». 
        public int FirstItemIndex => TotalCount == 0 ? 0 : ((Page - 1) * PageSize) + 1;

        public int LastItemIndex => Math.Min(Page * PageSize, TotalCount);

        public static PagedResult<T> Empty(int page = 1, int pageSize = 20) =>
            new() { Items = Array.Empty<T>(), Page = page, PageSize = pageSize, TotalCount = 0 };
    }

    public static class PagedResultExtensions
    { 
        // Спочатку рахує загальну кількість, потім вибирає одну сторінку. Номер сторінки обмежується,
        // щоб застаріле посилання «?page=99» не показало порожній екран. 
        public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
            this IQueryable<T> query,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            if (pageSize <= 0)
            {
                pageSize = 20;
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            if (page < 1)
            {
                page = 1;
            }

            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<T>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
    }
}
