using BLL.Services.Interfaces;
using DAL.UnitOfWork.Interfaces;
using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class ComplaintService : IComplaintService
    {
        private readonly IUnitOfWork _unitOfWork;
        
        public ComplaintService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Complaint?> GetByIdAsync(int id)
        {
            return await _unitOfWork.Complaints.GetWithDetailsByIdAsync(id);
        }

        public async Task<IEnumerable<Complaint>> GetMyComplaintsAsync(string senderId)
        {
            if (string.IsNullOrEmpty(senderId))
            {
                throw new ArgumentException("Sender ID cannot be null or empty.", nameof(senderId));
            }

            return await _unitOfWork.Complaints.GetBySenderIdAsync(senderId);
        }

        public async Task<IEnumerable<Complaint>> GetComplaintsAgainstUserAsync(string targetUserId)
        {
            if (string.IsNullOrEmpty(targetUserId))
            {
                throw new ArgumentException("Target user ID cannot be null or empty.", nameof(targetUserId));
            }
            return await _unitOfWork.Complaints.GetByTargetUserIdAsync(targetUserId);
        }

        public async Task<IEnumerable<Complaint>> GetComplaintsByStatusAsync(ComplaintStatus status)
        {
            return await _unitOfWork.Complaints.GetByStatusAsync(status);
        }

        public async Task<IEnumerable<Complaint>> GetAllComplaintsAsync()
        {
            return await _unitOfWork.Complaints.GetAllAsync();
        }

        public async Task<Complaint> CreateComplaintAsync(Complaint complaint)
        {
            if (string.IsNullOrEmpty(complaint.SenderId))
            {
                throw new ArgumentException("Sender ID cannot be null or empty.", nameof(complaint.SenderId));
            }

            if (string.IsNullOrEmpty(complaint.Reason))
            {
                throw new ArgumentException("Reason cannot be null or empty.", nameof(complaint.Reason));
            }

            if (string.IsNullOrEmpty(complaint.Description))
            {
                throw new ArgumentException("Description cannot be null or empty.", nameof(complaint.Description));
            }

            if (!string.IsNullOrEmpty(complaint.TargetUserId) && complaint.TargetUserId == complaint.SenderId)
            {
                throw new ArgumentException("Sender cannot file a complaint against themselves.");
            }

            complaint.CreatedAt = DateTime.UtcNow;
            complaint.Status = ComplaintStatus.Pending;

            await _unitOfWork.Complaints.AddAsync(complaint);
            await _unitOfWork.SaveChangesAsync();
            return complaint;
        }

        public async Task ChangeStatusAsync(int complaintId, ComplaintStatus newStatus)
        {
            var complaint = await _unitOfWork.Complaints.GetByIdAsync(complaintId);
            if (complaint == null)
            {
                throw new ArgumentException($"Complaint with ID {complaintId} does not exist.", nameof(complaintId));
            }
            complaint.Status = newStatus;
            await _unitOfWork.Complaints.UpdateAsync(complaint);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
