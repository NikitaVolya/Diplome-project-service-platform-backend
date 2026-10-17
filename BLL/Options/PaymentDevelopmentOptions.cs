namespace BLL.Options
{
    /// <summary>
    /// Switches that make local testing of webhooks possible (Stripe CLI "stripe trigger" etc.).
    /// They are refused at startup outside the Development environment (see Program.cs), and
    /// they only ever apply to events the provider marks as TEST events, so they cannot weaken
    /// production payments.
    /// </summary>
    public class PaymentDevelopmentOptions
    {
        /// <summary>
        /// A test event without a payment reference (what "stripe trigger" produces) is mapped to the
        /// most recent pending payment of the same provider that already has a checkout.
        /// </summary>
        public bool MapUnreferencedTestEvents { get; set; }

        /// <summary>
        /// For test events, do not compare the paid amount/currency with the payment
        /// (the Stripe CLI fixture charges a made-up amount).
        /// </summary>
        public bool SkipAmountCheckForTestEvents { get; set; }
    }
}
