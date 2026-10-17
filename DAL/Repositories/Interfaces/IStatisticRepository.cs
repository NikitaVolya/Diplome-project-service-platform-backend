using Domain.Models;

namespace DAL.Repositories.Interfaces
{
    public interface IStatisticRepository : IGenericRepository<Statistic>
    {
        Task<Statistic?> GetByDateAsync(DateTime date);

        Task<IEnumerable<Statistic>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);

        Task<Statistic?> GetLatestAsync();
    }
}