using BLL.Admin.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace AdminPanel.Services
{
    public interface INavigationBadgeService
    {
        Task<ModerationQueue> GetAsync();
    }

    // Дає лічильники, які видно біля пунктів бічного меню. Шаблон малюється на кожному запиті,
    // тому числа ненадовго кешуються: значок, застарілий максимум на пів хвилини, — прийнятна ціна
    // за те, щоб не робити п'ять агрегатних запитів на кожне відкриття сторінки.
    public class NavigationBadgeService : INavigationBadgeService
    {
        private static readonly TimeSpan CacheFor = TimeSpan.FromSeconds(30);
        private const string CacheKey = "nav-badges";

        private readonly IAdminModerationService _moderation;
        private readonly IMemoryCache _cache;
        private readonly ILogger<NavigationBadgeService> _logger;

        public NavigationBadgeService(
            IAdminModerationService moderation,
            IMemoryCache cache,
            ILogger<NavigationBadgeService> logger)
        {
            _moderation = moderation;
            _cache = cache;
            _logger = logger;
        }

        public async Task<ModerationQueue> GetAsync()
        {
            if (_cache.TryGetValue(CacheKey, out ModerationQueue? cached) && cached != null)
            {
                return cached;
            }

            try
            {
                var queue = await _moderation.GetQueueAsync();
                _cache.Set(CacheKey, queue, CacheFor);
                return queue;
            }
            catch (Exception ex)
            {
                // Меню має малюватися навіть тоді, коли база недоступна.
                _logger.LogWarning(ex, "Could not load navigation badges");
                return new ModerationQueue();
            }
        }
    }
}
