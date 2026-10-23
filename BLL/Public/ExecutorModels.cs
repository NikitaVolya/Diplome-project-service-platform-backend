using Domain.Models;

namespace BLL.Public
{
    public sealed class ExecOrder
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Parent { get; set; } = string.Empty;
        public OrderStatus Status { get; set; }
        public bool OnReview { get; set; }

        public decimal Price { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExecutionAt { get; set; }
        public string Address { get; set; } = string.Empty;

        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
    }

    public sealed class ExecOrdersPage
    {
        public List<ExecOrder> Items { get; set; } = new();
        public int Page { get; set; } = 1;
        public int Pages { get; set; } = 1;
        public int Total { get; set; }
    }
    public sealed class ExecCounters
    {
        public int Fresh { get; set; }
        public int Active { get; set; }
        public int Completed { get; set; }
        public decimal Earned { get; set; }
        public int Cancelled { get; set; }
    }
    public sealed class ExecServiceCard
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Parent { get; set; } = string.Empty;
        public decimal FromPrice { get; set; }
        public ServiceState State { get; set; }
        public DateTime CreatedAt { get; set; }

        public int Orders { get; set; }

        public double Rating { get; set; }
        public int Reviews { get; set; }

        public string Image { get; set; } = string.Empty;
    }

    public sealed class ExecServicesPage
    {
        public List<ExecServiceCard> Items { get; set; } = new();
        public int Active { get; set; }
        public int Paused { get; set; }
        public int Drafts { get; set; }
        public int OrdersTotal { get; set; }
        public int Page { get; set; } = 1;
        public int Pages { get; set; } = 1;
    }

    public sealed class ExecWork
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;
        public string? Link { get; set; }
        public string Image { get; set; } = string.Empty;
    }

    public sealed class ExecPortfolio
    {
        public List<string> Groups { get; set; } = new();
        public List<ExecWork> Items { get; set; } = new();
        public int Page { get; set; } = 1;
        public int Pages { get; set; } = 1;
    }

    public sealed class ExecEvent
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public EventKind Kind { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public string? PartnerName { get; set; }
        public int? OrderId { get; set; }
        public bool FromOrder { get; set; }
        public int Day { get; set; }
        public int Hour { get; set; }
        public int Span { get; set; } = 1;
    }

    public sealed class ExecCalendar
    {
        public DateTime Monday { get; set; }

        public List<ExecEvent> Events { get; set; } = new();

        public List<ExecEvent> Upcoming { get; set; } = new();
    }

    public sealed class ExecReview
    {
        public int Id { get; set; }
        public int OrderId { get; set; }

        public string OrderTitle { get; set; } = string.Empty;

        public string Parent { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;

        public string PersonId { get; set; } = string.Empty;
        public string PersonName { get; set; } = string.Empty;
        public string Portrait { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Image { get; set; } = string.Empty;
    }

    public sealed class ExecReviews
    {
        public List<ExecReview> Items { get; set; } = new();

        public List<ExecOrder> Awaiting { get; set; } = new();

        public int Total { get; set; }
        public double Average { get; set; }
        public int WithPhoto { get; set; }

        public int Recommend { get; set; }

        public int Page { get; set; } = 1;
        public int Pages { get; set; } = 1;
    }

    public sealed class ExecMonth
    {
        public int Views { get; set; }
        public int Orders { get; set; }
        public decimal Earned { get; set; }
    }

    public sealed class ExecSummary
    {
        public ExecCounters Counters { get; set; } = new();
        public List<ExecOrder> Orders { get; set; } = new();
        public List<ExecServiceCard> Services { get; set; } = new();
        public List<ExecWork> Works { get; set; } = new();
        public ExecMonth Month { get; set; } = new();

        public int Completeness { get; set; }
    }
}
