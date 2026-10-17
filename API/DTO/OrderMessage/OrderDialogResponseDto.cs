namespace API.DTO.OrderMessage
{
    public class OrderDialogResponseDto
    {
        public int OrderId { get; set; }
        public string OrderTitle { get; set; } = string.Empty;
        public string? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? ExecutorId { get; set; }
        public string? ExecutorName { get; set; }
        public OrderMessageResponseDto? LastMessage { get; set; }
        public int UnreadCount { get; set; }
    }
}