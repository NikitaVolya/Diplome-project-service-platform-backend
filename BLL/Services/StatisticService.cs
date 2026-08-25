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
    public class StatisticService : IStatisticService
    {
        private readonly IUnitOfWork _unitOfWork;

        public StatisticService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Statistic?> GetByDateAsync(DateTime date)
        {
            return await _unitOfWork.Statistics.GetByDateAsync(date);
        }

        public async Task<IEnumerable<Statistic>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            if (startDate > endDate)
            {
                throw new ArgumentException("Start date must be less than or equal to end date.");
            }

            return await _unitOfWork.Statistics.GetByDateRangeAsync(startDate, endDate);
        }

        public async Task<Statistic?> GetLatestAsync()
        {
            return await _unitOfWork.Statistics.GetLatestAsync();
        }

        public async Task<Statistic> RecalculateDailyStatisticAsync(DateTime date)
        {
            var targetDate = date.Date;
            var nextDate = targetDate.AddDays(1);

            var allOrders = await _unitOfWork.Orders.GetAllAsync();
            var totalOrders = allOrders.Count(o => o.CreatedAt >= targetDate && o.CreatedAt < nextDate);

            var completedOrders = allOrders.Count(o => o.Status == OrderStatus.Completed
                                                    && o.CreatedAt >= targetDate
                                                    && o.CreatedAt < nextDate);

            var allPayments = await _unitOfWork.Payments.GetAllAsync();
            var totalRevenue = allPayments
                .Where(p => p.Status == PaymentStatus.Completed
                         && p.PaidAt.HasValue
                         && p.PaidAt.Value >= targetDate
                         && p.PaidAt.Value < nextDate)
                .Sum(p => p.Amount);

            var statistic = await _unitOfWork.Statistics.GetByDateAsync(targetDate);

            if (statistic == null)
            {
                statistic = new Statistic
                {
                    Date = targetDate,
                    TotalOrders = totalOrders,
                    CompletedOrders = completedOrders,
                    TotalRevenue = totalRevenue,
                    NewUsersCount = 0 
                };

                await _unitOfWork.Statistics.AddAsync(statistic);
            }
            else
            {
                statistic.TotalOrders = totalOrders;
                statistic.CompletedOrders = completedOrders;
                statistic.TotalRevenue = totalRevenue;

                await _unitOfWork.Statistics.UpdateAsync(statistic);
            }

            await _unitOfWork.SaveChangesAsync();
            return statistic;
        }

        public async Task<Statistic> AggregatePeriodStatisticAsync(DateTime startDate, DateTime endDate)
        {
            var records = await GetByDateRangeAsync(startDate, endDate);

            return new Statistic
            {
                Date = startDate.Date,
                TotalOrders = records.Sum(s => s.TotalOrders),
                CompletedOrders = records.Sum(s => s.CompletedOrders),
                TotalRevenue = records.Sum(s => s.TotalRevenue),
                NewUsersCount = records.Sum(s => s.NewUsersCount)
            };
        }

        public async Task<Statistic> GetMasterStatisticAsync(string masterId, DateTime? startDate = null, DateTime? endDate = null)
        {
            if (string.IsNullOrEmpty(masterId))
            {
                throw new ArgumentException("Master ID cannot be null or empty.", nameof(masterId));
            }

            var allOrders = await _unitOfWork.Orders.GetAllAsync();
            var masterOrders = allOrders.Where(o => o.ExecutorId == masterId);

            if (startDate.HasValue)
            {
                masterOrders = masterOrders.Where(o => o.CreatedAt >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                masterOrders = masterOrders.Where(o => o.CreatedAt <= endDate.Value);
            }

            var ordersList = masterOrders.ToList();

            var completedOrderIds = ordersList
                .Where(o => o.Status == OrderStatus.Completed)
                .Select(o => o.Id)
                .ToList();

            var allPayments = await _unitOfWork.Payments.GetAllAsync();
            var totalEarnings = allPayments
                .Where(p => completedOrderIds.Contains(p.OrderId) && p.Status == PaymentStatus.Completed)
                .Sum(p => p.Amount);

            return new Statistic
            {
                Date = startDate ?? DateTime.UtcNow.Date,
                TotalOrders = ordersList.Count,
                CompletedOrders = ordersList.Count(o => o.Status == OrderStatus.Completed),
                TotalRevenue = totalEarnings,
                NewUsersCount = 0 
            };
        }
    }
}
