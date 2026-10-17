using DAL.Context;
using DAL.Repositories.Interfaces;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class StatisticRepository : GenericRepository<Statistic>, IStatisticRepository
    {
        public StatisticRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Statistic?> GetByDateAsync(DateTime date)
        {
            var targetDate = date.Date;
            return await _dbSet.FirstOrDefaultAsync(s => s.Date == targetDate);
        }

        public async Task<IEnumerable<Statistic>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            var start = startDate.Date;
            var end = endDate.Date;

            return await _dbSet
                .Where(s => s.Date >= start && s.Date <= end)
                .OrderBy(s => s.Date)
                .ToListAsync();
        }

        public async Task<Statistic?> GetLatestAsync()
        {
            return await _dbSet
                .OrderByDescending(s => s.Date)
                .FirstOrDefaultAsync();
        }
    }
}