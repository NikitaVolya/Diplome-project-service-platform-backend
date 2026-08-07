using Microsoft.EntityFrameworkCore;
using Domain.Models;

namespace DAL.Context
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<Admin> Admins { get; set; }
        public DbSet<Application> Applications { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Complaint> Complaints { get; set; }
        public DbSet<Favorite> Favorites { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Statistic> Statistics { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Category>(entity =>
            {
                entity.HasOne(c => c.ParentCategory)
                      .WithMany(c => c.SubCategories)
                      .HasForeignKey(c => c.ParentCategoryId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Order>(entity =>
            {
                entity.HasOne(o => o.Category)
                      .WithMany(c => c.Orders)
                      .HasForeignKey(o => o.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.Property(o => o.Price)
                      .HasPrecision(18, 2);
            });


            builder.Entity<Application>(entity =>
            {
                entity.HasOne(a => a.Order)
                      .WithMany(o => o.Applications)
                      .HasForeignKey(a => a.OrderId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.Property(a => a.ProposedPrice)
                      .HasPrecision(18, 2);
            });

            builder.Entity<Favorite>(entity =>
            {
                entity.HasOne(f => f.TargetOrder)
                      .WithMany()
                      .HasForeignKey(f => f.TargetOrderId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Payment>(entity =>
            {
                entity.HasOne(p => p.Order)
                      .WithMany()
                      .HasForeignKey(p => p.OrderId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.Property(p => p.Amount)
                      .HasPrecision(18, 2);
            });

            builder.Entity<Review>(entity =>
            {
                entity.HasOne(r => r.Order)
                      .WithMany()
                      .HasForeignKey(r => r.OrderId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Statistic>(entity =>
            {
                entity.Property(s => s.TotalRevenue)
                      .HasPrecision(18, 2);
            });

            builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        }
    }
}