using DAL.Context;
using DAL.Repositories.Interfaces;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class ApplicationRepository : GenericRepository<Application>, IApplicationRepository
    {
        public ApplicationRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Application?> GetWithDetailsByIdAsync(int id)
        {
            return await _dbSet
                .Include(a => a.Executor)
                .Include(a => a.Order)
                    .ThenInclude(o => o.Customer)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<IEnumerable<Application>> GetByOrderIdAsync(int orderId)
        {
            return await _dbSet
                .Include(a => a.Executor)
                .Where(a => a.OrderId == orderId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Application>> GetByExecutorIdAsync(string executorId)
        {
            return await _dbSet
                .Include(a => a.Order)
                    .ThenInclude(o => o.Category)
                .Where(a => a.ExecutorId == executorId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> HasUserAppliedAsync(int orderId, string executorId)
        {
            return await _dbSet.AnyAsync(a => a.OrderId == orderId && a.ExecutorId == executorId);
        }

        public async Task UpdateStatusAsync(int applicationId, ApplicationStatus newStatus)
        {
            var application = await _dbSet.FindAsync(applicationId);
            if (application != null)
            {
                application.Status = newStatus;
            }
        }
    }
}