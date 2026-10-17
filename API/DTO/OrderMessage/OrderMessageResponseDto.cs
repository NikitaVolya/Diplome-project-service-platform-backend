namespace API.DTO.OrderMessage
{
    public class OrderMessageResponseDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public string? SenderName { get; set; }
        public string Text { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }
    }
}