using Domain.Entities;
using Domain.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DAL.Context
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
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

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }

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

                entity.HasOne(o => o.Customer)
                      .WithMany(u => u.CreatedOrders)
                      .HasForeignKey(o => o.CustomerId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(o => o.Executor)
                      .WithMany(u => u.ExecutedOrders)
                      .HasForeignKey(o => o.ExecutorId)
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

                entity.HasOne(a => a.Executor)
                      .WithMany(u => u.Applications)
                      .HasForeignKey(a => a.ExecutorId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.Property(a => a.ProposedPrice)
                      .HasPrecision(18, 2);
            });

            builder.Entity<Complaint>(entity =>
            {
                entity.HasOne(c => c.Sender)
                      .WithMany(u => u.SentComplaints)
                      .HasForeignKey(c => c.SenderId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(c => c.TargetUser)
                      .WithMany(u => u.ReceivedComplaints)
                      .HasForeignKey(c => c.TargetUserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Favorite>(entity =>
            {
                entity.HasOne(f => f.User)
                      .WithMany(u => u.Favorites)
                      .HasForeignKey(f => f.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(f => f.TargetOrder)
                      .WithMany()
                      .HasForeignKey(f => f.TargetOrderId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(f => f.TargetExecutor)
                      .WithMany()
                      .HasForeignKey(f => f.TargetExecutorId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Notification>(entity =>
            {
                entity.HasOne(n => n.User)
                      .WithMany(u => u.Notifications)
                      .HasForeignKey(n => n.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Payment>(entity =>
            {
                entity.HasOne(p => p.Order)
                      .WithMany()
                      .HasForeignKey(p => p.OrderId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(p => p.User)
                      .WithMany(u => u.Payments)
                      .HasForeignKey(p => p.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.Property(p => p.Amount)
                      .HasPrecision(18, 2);
            });

            builder.Entity<Review>(entity =>
            {
                entity.HasOne(r => r.Order)
                      .WithMany()
                      .HasForeignKey(r => r.OrderId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.Author)
                      .WithMany(u => u.WrittenReviews)
                      .HasForeignKey(r => r.AuthorId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.TargetUser)
                      .WithMany(u => u.ReceivedReviews)
                      .HasForeignKey(r => r.TargetUserId)
                      .OnDelete(DeleteBehavior.Restrict);
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