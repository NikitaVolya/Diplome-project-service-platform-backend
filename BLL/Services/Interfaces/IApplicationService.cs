using Domain.Models;

namespace BLL.Services.Interfaces
{
    public interface IApplicationService
    {
        Task<Application?> GetByIdAsync(int id);
        Task<IEnumerable<Application>> GetByOrderIdAsync(int orderId);
        Task<IEnumerable<Application>> GetByExecutorIdAsync(string executorId);

        Task<Application> CreateAsync(int orderId, string executorId, decimal proposedPrice, string? comment);
        Task AcceptApplicationAsync(int applicationId, string currentUserId);
        Task RejectApplicationAsync(int applicationId, string currentUserId);
        Task DeleteAsync(int applicationId, string currentUserId);
    }
}