using DAL.Context;
using DAL.Repositories.Interfaces;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class ComplaintRepository : GenericRepository<Complaint>, IComplaintRepository
    {
        public ComplaintRepository(ApplicationDbContext context) : base(context) { }

        public async Task<Complaint?> GetWithDetailsByIdAsync(int id)
        {
            return await _dbSet
                .Include(c => c.Sender)
                .Include(c => c.TargetUser)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<IEnumerable<Complaint>> GetByStatusAsync(ComplaintStatus status)
        {
            return await _dbSet
                .Include(c => c.Sender)
                .Include(c => c.TargetUser)
                .Where(c => c.Status == status)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Complaint>> GetBySenderIdAsync(string senderId)
        {
            return await _dbSet
                .Include(c => c.TargetUser)
                .Where(c => c.SenderId == senderId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Complaint>> GetByTargetUserIdAsync(string targetUserId)
        {
            return await _dbSet
                .Include(c => c.Sender)
                .Where(c => c.TargetUserId == targetUserId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }
    }
}