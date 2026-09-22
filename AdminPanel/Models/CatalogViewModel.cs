using BLL.Public;

namespace AdminPanel.Models;

public sealed class CatalogViewModel
{
    public List<DirectoryCategory> Categories { get; set; } = new();
    public DirectoryCategory? Selected { get; set; }
    public List<DirectoryCategory> Items { get; set; } = new();
    public List<DirectoryExecutor> Executors { get; set; } = new();
    public IReadOnlyList<PublicServiceItem> Services { get; set; } = Array.Empty<PublicServiceItem>();
    public bool IsDemo { get; set; }
    public bool Unavailable { get; set; }
    public bool ShowAll { get; set; }
    public string Query { get; set; } = "";
    public string City { get; set; } = "";
    public bool IsGlobalSearch { get; set; }
}
