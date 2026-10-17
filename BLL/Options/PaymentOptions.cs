namespace BLL.Options
{
    public class PaymentOptions
    {
        public const string SectionName = "Payments";

        /// <summary>Public https base URL of THIS API, e.g. https://api.example.com. Used to build webhook URLs.</summary>
        public string PublicBaseUrl { get; set; } = string.Empty;

        /// <summary>Front-end page the customer is sent back to after paying.</summary>
        public string ReturnUrl { get; set; } = string.Empty;

        public string Currency { get; set; } = "UAH";

        /// <summary>Local-testing helpers. Must stay disabled everywhere except Development.</summary>
        public PaymentDevelopmentOptions Development { get; set; } = new();
    }
}
