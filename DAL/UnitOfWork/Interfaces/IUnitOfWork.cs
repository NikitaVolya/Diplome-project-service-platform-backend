

using DAL.Repositories.Interfaces;

namespace DAL.UnitOfWork.Interfaces
{
    public interface IUnitOfWork
    {
        IOrderRepository Orders { get; }
        IApplicationRepository Applications { get; }
        ICategoryRepository Categories { get; }
        IComplaintRepository Complaints { get; }
        IFavoriteRepository Favorites { get; }
        INotificationRepository Notifications { get; }
        IPaymentRepository Payments { get; }
        IReviewRepository Reviews { get; }
        IStatisticRepository Statistics { get; }
        IOrderMessageRepository OrderMessages { get; }
        Task<int> SaveChangesAsync();
    }
}
