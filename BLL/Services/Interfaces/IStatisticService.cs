using Domain.Models;

namespace BLL.Services.Interfaces
{
    public interface IStatisticService
    {
        Task<Statistic?> GetByDateAsync(DateTime date);
        Task<IEnumerable<Statistic>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<Statistic?> GetLatestAsync();

        Task<Statistic> RecalculateDailyStatisticAsync(DateTime date);
        Task<Statistic> AggregatePeriodStatisticAsync(DateTime startDate, DateTime endDate);

        Task<Statistic> GetMasterStatisticAsync(string masterId, DateTime? startDate = null, DateTime? endDate = null);
    }
}