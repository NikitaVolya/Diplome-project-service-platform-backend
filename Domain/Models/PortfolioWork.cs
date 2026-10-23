using Domain.Entities;

namespace Domain.Models
{
    public class PortfolioWork
    {
        public int Id { get; set; }

        public string OwnerId { get; set; } = string.Empty;
        public ApplicationUser Owner { get; set; } = null!;

        public string Title { get; set; } = string.Empty;

        public string Group { get; set; } = string.Empty;

        public string? Image { get; set; }

        public string? Link { get; set; }

        public int Sort { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public static class PortfolioSections
    {
        public static readonly string[] Order =
        {
            "Логотипи", "Ілюстрації", "Реклама", "Web-дизайн",
            "Соцмережі", "Презентації", "Поліграфія", "Переклади"
        };

        public static int Rank(string group, int inside)
        {
            var at = Array.IndexOf(Order, group);
            return (at < 0 ? Order.Length : at) * 100 + Math.Clamp(inside, 0, 99);
        }

        public static int Place(string group)
        {
            var at = Array.IndexOf(Order, group);
            return at < 0 ? int.MaxValue : at;
        }
    }
}
