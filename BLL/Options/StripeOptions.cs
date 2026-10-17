namespace BLL.Options
{
    public class StripeOptions
    {
        public const string SectionName = "Stripe";

        public string SecretKey { get; set; } = string.Empty;

        /// <summary>Signing secret of the webhook endpoint (starts with "whsec_").</summary>
        public string WebhookSecret { get; set; } = string.Empty;
    }
}
