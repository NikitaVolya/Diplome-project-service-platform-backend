using BLL.Admin.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace AdminPanel.Services
{
    public interface INavigationBadgeService
    {
        Task<ModerationQueue> GetAsync();
    }

    /// <summary>
    /// Supplies the counters shown next to the sidebar links. The layout renders on every request,
    /// so the numbers are cached briefly — a badge that is up to half a minute stale is a fair trade
    /// for not running five aggregate queries per page view.
    /// </summary>
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
                // The navigation must render even when the database is unavailable.
                _logger.LogWarning(ex, "Could not load navigation badges");
                return new ModerationQueue();
            }
        }
    }
}
