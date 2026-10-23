using Domain.Models;

namespace BLL.Public
{
    public sealed class CabinetOwner
    {
        public string Id { get; set; } = string.Empty;

        public bool IsDemo { get; set; }
    }

    public sealed class CabinetOrder
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Parent { get; set; } = string.Empty;
        public OrderStatus Status { get; set; }
        public decimal Price { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExecutionAt { get; set; }

        public string? ExecutorId { get; set; }
        public string? ExecutorName { get; set; }

        public string Image { get; set; } = string.Empty;

        public string ExecutorPortrait { get; set; } = string.Empty;
    }

    public sealed class CabinetOrdersPage
    {
        public List<CabinetOrder> Items { get; set; } = new();
        public int Page { get; set; } = 1;
        public int Pages { get; set; } = 1;
        public int Total { get; set; }
    }

    public sealed class CabinetThread
    {
        public int OrderId { get; set; }
        public string OrderTitle { get; set; } = string.Empty;
        public string PartnerId { get; set; } = string.Empty;
        public string PartnerName { get; set; } = string.Empty;
        public string PartnerPortrait { get; set; } = string.Empty;
        public string Last { get; set; } = string.Empty;
        public DateTime LastAt { get; set; }
        public int Unread { get; set; }
    }

    public sealed class CabinetMessage
    {
        public int Id { get; set; }
        public bool Mine { get; set; }
        public string Author { get; set; } = string.Empty;
        public string Portrait { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }
    }

    public sealed class CabinetChat
    {
        public CabinetThread Head { get; set; } = new();
        public List<CabinetMessage> Messages { get; set; } = new();
    }

    public sealed class CabinetFavorite
    {
        public int Id { get; set; }
        public string ExecutorId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Portrait { get; set; } = string.Empty;

        public string Group { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public double Rating { get; set; }
        public int Reviews { get; set; }

        public List<string> Skills { get; set; } = new();
    }

    public sealed class CabinetFavoriteGroup
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public sealed class CabinetFavorites
    {
        public List<CabinetFavoriteGroup> Groups { get; set; } = new();
        public List<CabinetFavorite> Items { get; set; } = new();
    }

    public sealed class CabinetWallet
    {
        public decimal Balance { get; set; }
        public decimal Spent { get; set; }
        public decimal Pending { get; set; }
        public int PendingCount { get; set; }
        public int Total { get; set; }
    }

    public sealed class CabinetTransaction
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Title { get; set; } = string.Empty;
        public int OrderId { get; set; }
        public decimal Amount { get; set; }
        public PaymentStatus Status { get; set; }

        public bool Incoming { get; set; }
    }

    public sealed class CabinetTransactionsPage
    {
        public List<CabinetTransaction> Items { get; set; } = new();
        public int Page { get; set; } = 1;
        public int Pages { get; set; } = 1;
        public int Total { get; set; }
    }

    public sealed class CabinetBid
    {
        public int Id { get; set; }
        public string ExecutorId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Portrait { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public double Rating { get; set; }
        public int Reviews { get; set; }
        public decimal Price { get; set; }
        public int Days { get; set; }
        public ApplicationStatus Status { get; set; }
    }

    public sealed class CabinetEvent
    {
        /// <summary>Подія українською; якщо є <see cref="Arg"/> — це шаблон з {0}.</summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>Підстановка в шаблон (імʼя тощо) — щоб сайт міг перекласти сам шаблон.</summary>
        public string? Arg { get; set; }

        public DateTime At { get; set; }
    }

    public sealed class CabinetOrderDetails
    {
        public CabinetOrder Order { get; set; } = new();
        public string Description { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public List<CabinetBid> Bids { get; set; } = new();
        public List<CabinetEvent> Activity { get; set; } = new();
    }

    public sealed class CabinetReview
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string OrderTitle { get; set; } = string.Empty;
        public string TargetName { get; set; } = string.Empty;
        public string TargetPortrait { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public sealed class CabinetSummary
    {
        public int Active { get; set; }
        public int Completed { get; set; }
        public int AwaitingReview { get; set; }
        public decimal Spent { get; set; }
        public List<CabinetOrder> Orders { get; set; } = new();
        public List<CabinetThread> Threads { get; set; } = new();
        public List<PublicServiceItem> Recommended { get; set; } = new();
    }
}
