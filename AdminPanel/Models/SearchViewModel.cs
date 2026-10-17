using BLL.Public;

namespace AdminPanel.Models;

public sealed class SearchViewModel
{
    public IReadOnlyList<PublicCategoryItem> Categories { get; set; } = Array.Empty<PublicCategoryItem>();

    public IReadOnlyList<PublicServiceItem> Services { get; set; } = Array.Empty<PublicServiceItem>();

    public IReadOnlyList<PublicServiceItem> Recent { get; set; } = Array.Empty<PublicServiceItem>();

    public bool RecentIsFallback { get; set; }

    public string Query { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;
}
