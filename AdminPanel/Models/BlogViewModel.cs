namespace AdminPanel.Models;

public sealed class BlogViewModel
{
    public string Category { get; set; } = string.Empty;

    public IReadOnlyList<Article> Articles { get; set; } = Array.Empty<Article>();

    public int Page { get; set; } = 1;

    public int Pages { get; set; } = 1;
}
