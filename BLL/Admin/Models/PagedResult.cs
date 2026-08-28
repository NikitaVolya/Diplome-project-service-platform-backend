using Microsoft.EntityFrameworkCore;

namespace BLL.Admin.Models
{
    /// <summary>
    /// One page of a larger result set, together with everything a pager needs to render itself.
    /// </summary>
    public class PagedResult<T>
    {
        public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 20;

        public int TotalCount { get; set; }

        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

        public bool HasPrevious => Page > 1;

        public bool HasNext => Page < TotalPages;

        /// <summary>1-based index of the first row on this page, for "showing 21-40 of 137" captions.</summary>
        public int FirstItemIndex => TotalCount == 0 ? 0 : ((Page - 1) * PageSize) + 1;

        public int LastItemIndex => Math.Min(Page * PageSize, TotalCount);

        public static PagedResult<T> Empty(int page = 1, int pageSize = 20) =>
            new() { Items = Array.Empty<T>(), Page = page, PageSize = pageSize, TotalCount = 0 };
    }

    public static class PagedResultExtensions
    {
        /// <summary>
        /// Counts the query, then materialises a single page of it. Page numbers are clamped so that
        /// a stale "?page=99" link cannot produce an empty screen.
        /// </summary>
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
