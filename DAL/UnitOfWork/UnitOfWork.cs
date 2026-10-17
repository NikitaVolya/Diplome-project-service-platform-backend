using DAL.Context;
using DAL.Repositories;
using DAL.Repositories.Interfaces;
using DAL.UnitOfWork.Interfaces;

namespace DAL.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;

        public IOrderRepository Orders { get; }
        public IApplicationRepository Applications { get; }
        public ICategoryRepository Categories { get; }
        public IComplaintRepository Complaints { get; }
        public IFavoriteRepository Favorites { get; }
        public INotificationRepository Notifications { get; }
        public IPaymentRepository Payments { get; }
        public IReviewRepository Reviews { get; }
        public IStatisticRepository Statistics { get; }
        public IOrderMessageRepository OrderMessages { get; }

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;

            Orders = new OrderRepository(_context);
            Applications = new ApplicationRepository(_context);
            Categories = new CategoryRepository(_context);
            Complaints = new ComplaintRepository(_context);
            Favorites = new FavoriteRepository(_context);
            Notifications = new NotificationRepository(_context);
            Payments = new PaymentRepository(_context);
            Reviews = new ReviewRepository(_context);
            Statistics = new StatisticRepository(_context);
            OrderMessages = new OrderMessageRepository(_context);
        }

        public Task<int> SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }
    }
}