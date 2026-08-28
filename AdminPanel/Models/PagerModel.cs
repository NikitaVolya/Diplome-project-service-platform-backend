using BLL.Admin.Models;
using Microsoft.AspNetCore.Http;

namespace AdminPanel.Models
{
    /// <summary>
    /// Усе, що потрібно частковому представленню пагінації, разом із поточним рядком запиту, —
    /// щоб перехід між сторінками не втрачав застосовані адміністратором фільтри.
    /// </summary>
    public class PagerModel
    {
        public int Page { get; init; }
        public int TotalPages { get; init; }
        public int TotalCount { get; init; }
        public int FirstItemIndex { get; init; }
        public int LastItemIndex { get; init; }
        public string ItemNoun { get; init; } = "row";

        /// <summary>Параметри рядка запиту, які треба перенести на інші сторінки, окрім «page».</summary>
        public IReadOnlyDictionary<string, string?> Query { get; init; } =
            new Dictionary<string, string?>();

        public static PagerModel From<T>(PagedResult<T> result, HttpRequest request, string itemNoun = "row")
        {
            var query = request.Query
                .Where(pair => !string.Equals(pair.Key, "page", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(pair => pair.Key, pair => (string?)pair.Value.ToString());

            return new PagerModel
            {
                Page = result.Page,
                TotalPages = result.TotalPages,
                TotalCount = result.TotalCount,
                FirstItemIndex = result.FirstItemIndex,
                LastItemIndex = result.LastItemIndex,
                ItemNoun = itemNoun,
                Query = query
            };
        }

        /// <summary>Номери сторінок для відображення: вікно навколо поточної сторінки плюс перша й остання.</summary>
        public IEnumerable<int> WindowedPages(int window = 2)
        {
            if (TotalPages <= 0)
            {
                yield break;
            }

            var from = Math.Max(1, Page - window);
            var to = Math.Min(TotalPages, Page + window);

            if (from > 1)
            {
                yield return 1;
                if (from > 2)
                {
                    yield return -1; // ellipsis marker
                }
            }

            for (var i = from; i <= to; i++)
            {
                yield return i;
            }

            if (to < TotalPages)
            {
                if (to < TotalPages - 1)
                {
                    yield return -1;
                }

                yield return TotalPages;
            }
        }
    }
}
