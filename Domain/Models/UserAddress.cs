using Domain.Entities;

namespace Domain.Models
{
    public class UserAddress
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        public string Title { get; set; } = string.Empty;

        public AddressKind Kind { get; set; } = AddressKind.Home;

        public string Line { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;

        public string? Region { get; set; }

        public string? PostalCode { get; set; }

        public string Country { get; set; } = "Україна";

        public string? Note { get; set; }

        public bool IsPrimary { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum AddressKind
    {
        Home,
        Office,
        Country
    }
}
