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
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Complaint> Complaints { get; set; }
        public DbSet<Favorite> Favorites { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderMessage> OrderMessages { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Statistic> Statistics { get; set; }
        public DbSet<UserAddress> UserAddresses { get; set; }
        public DbSet<ExecutorService> ExecutorServices { get; set; }
        public DbSet<PortfolioWork> PortfolioWorks { get; set; }
        public DbSet<CalendarEvent> CalendarEvents { get; set; }
        public DbSet<ProfileVisit> ProfileVisits { get; set; }
        public DbSet<PayoutMethod> PayoutMethods { get; set; }
        public DbSet<Payout> Payouts { get; set; }
        public DbSet<SupportTicket> SupportTickets { get; set; }

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

                entity.Property(o => o.Status)
                      .HasConversion<string>();
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

                entity.Property(a => a.Status)
                      .HasConversion<string>();
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

                entity.Property(c => c.Status)
                      .HasConversion<string>();
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

                entity.Property(p => p.Provider)
                      .HasConversion<string>();

                entity.Property(p => p.Status)
                      .HasConversion<string>();
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

            builder.Entity<OrderMessage>(entity =>
            {
                entity.HasOne(m => m.Order)
                    .WithMany(o => o.OrderMessages)
                    .HasForeignKey(m => m.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(m => m.Sender)
                    .WithMany(u => u.SentMessages)
                    .HasForeignKey(m => m.SenderId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(m => m.Text)
                    .IsRequired()
                    .HasMaxLength(1000);
            });

            builder.Entity<AuditLog>(entity =>
            {
                entity.HasOne(a => a.User)
                      .WithMany()
                      .HasForeignKey(a => a.UserId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.Property(a => a.UserName)
                      .IsRequired()
                      .HasMaxLength(256);

                entity.Property(a => a.EntityName)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(a => a.EntityId)
                      .HasMaxLength(450);

                entity.Property(a => a.Description)
                      .HasMaxLength(1000);

                entity.Property(a => a.IpAddress)
                      .HasMaxLength(64);

                entity.Property(a => a.Action)
                      .HasConversion<string>()
                      .HasMaxLength(32);

                entity.Property(a => a.Severity)
                      .HasConversion<string>()
                      .HasMaxLength(32);

                entity.HasIndex(a => a.CreatedAt);
                entity.HasIndex(a => a.EntityName);
            });

            builder.Entity<UserAddress>(entity =>
            {
                entity.HasOne(a => a.User)
                      .WithMany(u => u.Addresses)
                      .HasForeignKey(a => a.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.Property(a => a.Title)
                      .IsRequired()
                      .HasMaxLength(60);

                entity.Property(a => a.Line)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(a => a.City)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(a => a.Region).HasMaxLength(100);
                entity.Property(a => a.PostalCode).HasMaxLength(10);
                entity.Property(a => a.Country).IsRequired().HasMaxLength(60);
                entity.Property(a => a.Note).HasMaxLength(200);

                entity.Property(a => a.Kind)
                      .HasConversion<string>()
                      .HasMaxLength(20);

                // Сторінка «Мої адреси» завжди читає список однієї людини.
                entity.HasIndex(a => a.UserId);
            });

            builder.Entity<ExecutorService>(entity =>
            {
                entity.HasOne(s => s.Owner)
                      .WithMany(u => u.Services)
                      .HasForeignKey(s => s.OwnerId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Категорії не видаляють, а ховають, тож послуга не має тягнути напрям за собою.
                entity.HasOne(s => s.Category)
                      .WithMany()
                      .HasForeignKey(s => s.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.Property(s => s.Title).IsRequired().HasMaxLength(120);
                entity.Property(s => s.Description).HasMaxLength(2000);
                entity.Property(s => s.Image).HasMaxLength(120);
                entity.Property(s => s.FromPrice).HasPrecision(18, 2);

                entity.Property(s => s.State)
                      .HasConversion<string>()
                      .HasMaxLength(20);

                entity.HasIndex(s => s.OwnerId);
            });

            builder.Entity<PortfolioWork>(entity =>
            {
                entity.HasOne(w => w.Owner)
                      .WithMany(u => u.Portfolio)
                      .HasForeignKey(w => w.OwnerId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.Property(w => w.Title).IsRequired().HasMaxLength(120);
                entity.Property(w => w.Group).IsRequired().HasMaxLength(60);
                entity.Property(w => w.Image).HasMaxLength(120);
                entity.Property(w => w.Link).HasMaxLength(300);

                entity.HasIndex(w => w.OwnerId);
            });

            builder.Entity<CalendarEvent>(entity =>
            {
                entity.HasOne(e => e.Owner)
                      .WithMany(u => u.Events)
                      .HasForeignKey(e => e.OwnerId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Замовлення може зникнути з кабінету раніше за подію — рве зв'язок, а не рядок.
                entity.HasOne(e => e.Order)
                      .WithMany()
                      .HasForeignKey(e => e.OrderId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.Property(e => e.Title).IsRequired().HasMaxLength(120);
                entity.Property(e => e.PartnerName).HasMaxLength(120);
                entity.Property(e => e.Note).HasMaxLength(500);

                entity.Property(e => e.Kind)
                      .HasConversion<string>()
                      .HasMaxLength(20);

                // Сторінка календаря завжди питає тиждень однієї людини.
                entity.HasIndex(e => new { e.OwnerId, e.StartAt });
            });

            builder.Entity<ProfileVisit>(entity =>
            {
                entity.HasOne(v => v.Owner)
                      .WithMany(u => u.Visits)
                      .HasForeignKey(v => v.OwnerId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Total рахується з п'яти колонок і в базі не зберігається.
                entity.Ignore(v => v.Total);

                // За добу на людину — один рядок; аналітика бере відрізок дат однієї людини.
                entity.HasIndex(v => new { v.OwnerId, v.Day }).IsUnique();
            });

            builder.Entity<PayoutMethod>(entity =>
            {
                entity.HasOne(m => m.Owner)
                      .WithMany(u => u.PayoutMethods)
                      .HasForeignKey(m => m.OwnerId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.Property(m => m.Label).IsRequired().HasMaxLength(120);
                entity.Property(m => m.Note).HasMaxLength(120);

                entity.Property(m => m.Kind)
                      .HasConversion<string>()
                      .HasMaxLength(20);

                entity.HasIndex(m => m.OwnerId);
            });

            builder.Entity<Payout>(entity =>
            {
                entity.HasOne(p => p.Owner)
                      .WithMany(u => u.Payouts)
                      .HasForeignKey(p => p.OwnerId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Спосіб можна прибрати — заявка в історії лишається з текстовим підписом.
                //
                // SET NULL тут ставити не можна: до Payouts вели б два шляхи видалення
                // (AspNetUsers → Payouts каскадом і AspNetUsers → PayoutMethods → Payouts),
                // а SQL Server такого не створює — «may cause cycles or multiple cascade paths».
                // Тому зв'язок рве застосунок: ExecutorInsightsService.DeleteMethodAsync
                // спершу обнуляє MethodId у заявках, і лише тоді прибирає спосіб.
                entity.HasOne(p => p.Method)
                      .WithMany(m => m.Payouts)
                      .HasForeignKey(p => p.MethodId)
                      .OnDelete(DeleteBehavior.ClientSetNull);

                entity.Property(p => p.Amount).HasPrecision(18, 2);
                entity.Property(p => p.MethodLabel).HasMaxLength(120);

                entity.Property(p => p.Status)
                      .HasConversion<string>()
                      .HasMaxLength(20);

                entity.HasIndex(p => new { p.OwnerId, p.CreatedAt });
            });

            builder.Entity<SupportTicket>(entity =>
            {
                entity.HasOne(t => t.Owner)
                      .WithMany(u => u.Tickets)
                      .HasForeignKey(t => t.OwnerId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.Property(t => t.Topic).IsRequired().HasMaxLength(120);
                entity.Property(t => t.Category).IsRequired().HasMaxLength(60);
                entity.Property(t => t.Message).IsRequired().HasMaxLength(4000);

                entity.Property(t => t.Status)
                      .HasConversion<string>()
                      .HasMaxLength(20);

                entity.HasIndex(t => new { t.OwnerId, t.CreatedAt });
            });

            builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        }


    }
}