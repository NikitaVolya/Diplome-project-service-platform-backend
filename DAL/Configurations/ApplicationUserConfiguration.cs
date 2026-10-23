using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace DAL.Configurations
{
    public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            builder.Property(x => x.FirstName)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.LastName)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.FullName)
                .HasMaxLength(200);

            builder.Property(x => x.AvatarUrl)
                .HasMaxLength(500);

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.UpdatedAt);

            builder.Property(x => x.IsDeleted)
                .HasDefaultValue(false);

            builder.Property(x => x.CompanyName).HasMaxLength(200);
            builder.Property(x => x.City).HasMaxLength(100);
            builder.Property(x => x.PostalCode).HasMaxLength(10);
            builder.Property(x => x.Gender).HasMaxLength(20);
            builder.Property(x => x.Experience).HasMaxLength(40);
            builder.Property(x => x.About).HasMaxLength(1000);
            builder.Property(x => x.WalletBalance).HasPrecision(18, 2);
            builder.Property(x => x.ProfileVisibility).HasMaxLength(20);
            builder.Property(x => x.Language).HasMaxLength(10);
            builder.Property(x => x.TimeZoneName).HasMaxLength(60);
            builder.Property(x => x.DateFormat).HasMaxLength(20);
            builder.Property(x => x.TimeFormat).HasMaxLength(5);
            builder.Property(x => x.Theme).HasMaxLength(10);
            builder.Property(x => x.PaymentCard).HasMaxLength(40);
            builder.Property(x => x.Currency).HasMaxLength(5);
            builder.Property(x => x.ShowOnlineStatus).HasDefaultValue(true);
            builder.Property(x => x.NotifyOrders).HasDefaultValue(true);
            builder.Property(x => x.NotifyMessages).HasDefaultValue(true);
            builder.Property(x => x.NotifyReviews).HasDefaultValue(true);
            builder.Property(x => x.NotifyReminders).HasDefaultValue(true);

            builder.Property(x => x.Headline).HasMaxLength(120);
            builder.Property(x => x.Nickname).HasMaxLength(60);
            builder.Property(x => x.Presence).HasMaxLength(20);
        }
    }
}
