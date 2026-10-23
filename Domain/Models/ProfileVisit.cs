using Domain.Entities;

namespace Domain.Models
{
    public class ProfileVisit
    {
        public int Id { get; set; }

        public string OwnerId { get; set; } = string.Empty;
        public ApplicationUser Owner { get; set; } = null!;

        public DateTime Day { get; set; }

        public int FromSearch { get; set; }

        public int FromCatalog { get; set; }

        public int FromDirect { get; set; }

        public int FromReferral { get; set; }

        public int FromOther { get; set; }

        public int PortfolioViews { get; set; }

        public int Total => FromSearch + FromCatalog + FromDirect + FromReferral + FromOther;
    }
}
