using Domain.Models;

namespace BLL.Services.Interfaces
{
    public interface IComplaintService
    {
        Task<Complaint?> GetByIdAsync(int id);
        Task<IEnumerable<Complaint>> GetMyComplaintsAsync(string senderId);
        Task<IEnumerable<Complaint>> GetComplaintsAgainstUserAsync(string targetUserId);

        Task<IEnumerable<Complaint>> GetComplaintsByStatusAsync(ComplaintStatus status);
        Task<IEnumerable<Complaint>> GetAllComplaintsAsync();

        Task<Complaint> CreateComplaintAsync(Complaint complaint);
        Task ChangeStatusAsync(int complaintId, ComplaintStatus newStatus);
    }
}