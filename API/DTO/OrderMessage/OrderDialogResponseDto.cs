namespace API.DTO.OrderMessage
{
    /// <summary>
    /// One conversation: an order plus ONE pair of participants. A customer who talks to two executors
    /// about the same order gets two entries. To open a conversation call
    /// GET api/OrderMessages/orders/{orderId}?participantId={ParticipantId}.
    /// </summary>
    public class OrderDialogResponseDto
    {
        public int OrderId { get; set; }
        public string OrderTitle { get; set; } = string.Empty;

        public string? CustomerId { get; set; }
        public string? CustomerName { get; set; }

        /// <summary>The executor of THIS conversation (not necessarily the one assigned to the order).</summary>
        public string? ExecutorId { get; set; }
        public string? ExecutorName { get; set; }

        /// <summary>The other side of the conversation from the point of view of the current user.</summary>
        public string? ParticipantId { get; set; }
        public string? ParticipantName { get; set; }

        public OrderMessageResponseDto? LastMessage { get; set; }
        public int UnreadCount { get; set; }
    }
}
