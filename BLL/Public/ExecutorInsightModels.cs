using Domain.Models;

namespace BLL.Public
{
    public sealed class ExecDay
    {
        public DateTime Day { get; set; }

        public int Views { get; set; }

        public int Orders { get; set; }
    }

    public sealed class ExecSource
    {
        public string Title { get; set; } = string.Empty;

        public string Key { get; set; } = string.Empty;

        public int Views { get; set; }

        public double Share { get; set; }
    }

    public sealed class ExecPopular
    {
        public string Title { get; set; } = string.Empty;
        public int Orders { get; set; }
    }

    public sealed class ExecMetric
    {
        public string Title { get; set; } = string.Empty;

        public string Now { get; set; } = string.Empty;
        public string Was { get; set; } = string.Empty;

        public string Change { get; set; } = string.Empty;

        public int Direction { get; set; }
    }

    public sealed class ExecAnalytics
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }

        public int Views { get; set; }
        public int Reviews { get; set; }
        public int Orders { get; set; }
        public double Conversion { get; set; }

        public double ViewsChange { get; set; }
        public double ReviewsChange { get; set; }
        public double OrdersChange { get; set; }
        public double ConversionChange { get; set; }

        public List<ExecDay> Days { get; set; } = new();
        public List<ExecSource> Sources { get; set; } = new();
        public List<ExecPopular> Popular { get; set; } = new();
        public List<ExecMetric> Metrics { get; set; } = new();

        public int Ceiling { get; set; } = 100;
    }

    public sealed class ExecTxn
    {
        public DateTime Date { get; set; }
        public string Title { get; set; } = string.Empty;

        public bool Income { get; set; }

        public decimal Amount { get; set; }

        public string State { get; set; } = string.Empty;

        public string StateKey { get; set; } = string.Empty;
    }

    public sealed class ExecBar
    {
        public DateTime Day { get; set; }
        public decimal Amount { get; set; }
    }

    public sealed class ExecMethod
    {
        public int Id { get; set; }
        public PayoutKind Kind { get; set; }
        public string Label { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
    }

    public sealed class ExecFinance
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }

        public decimal Income { get; set; }

        public decimal Available { get; set; }

        public decimal Pending { get; set; }

        public decimal Paid { get; set; }

        public double IncomeChange { get; set; }

        public int PendingOrders { get; set; }

        public List<ExecBar> Bars { get; set; } = new();
        public List<ExecTxn> Transactions { get; set; } = new();
        public List<ExecMethod> Methods { get; set; } = new();

        public decimal Ceiling { get; set; }
    }

    public sealed class ExecTicket
    {
        public int Id { get; set; }
        public string Topic { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public TicketStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
