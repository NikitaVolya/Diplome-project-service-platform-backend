namespace API.DTO.Statistic
{
    public class StatisticResponseDto
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public int TotalOrders { get; set; }
        public int CompletedOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public int NewUsersCount { get; set; }
    }
}