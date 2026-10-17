using Domain.Models;

namespace DAL.Repositories.Interfaces
{
    public interface IApplicationRepository : IGenericRepository<Application>
    {
        Task<Application?> GetWithDetailsByIdAsync(int id);

        Task<IEnumerable<Application>> GetByOrderIdAsync(int orderId);

        Task<IEnumerable<Application>> GetByExecutorIdAsync(string executorId);

        Task<bool> HasUserAppliedAsync(int orderId, string executorId);

        Task UpdateStatusAsync(int applicationId, ApplicationStatus newStatus);
    }
}