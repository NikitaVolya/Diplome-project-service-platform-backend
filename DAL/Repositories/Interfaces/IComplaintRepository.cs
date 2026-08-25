using Domain.Models;

namespace DAL.Repositories.Interfaces
{
    public interface IComplaintRepository : IGenericRepository<Complaint>
    {
        Task<Complaint?> GetWithDetailsByIdAsync(int id);

        Task<IEnumerable<Complaint>> GetByStatusAsync(ComplaintStatus status);

        Task<IEnumerable<Complaint>> GetBySenderIdAsync(string senderId);

        Task<IEnumerable<Complaint>> GetByTargetUserIdAsync(string targetUserId);
    }
}