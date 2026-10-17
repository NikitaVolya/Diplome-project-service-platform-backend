using Domain.Models;

namespace BLL.Services.Interfaces
{
    /// <summary>What the client needs to send the customer to the provider's payment page.</summary>
    /// <param name="Provider">Provider name.</param>
    /// <param name="RedirectUrl">Ready URL to redirect to (Stripe). Null for form based providers.</param>
    /// <param name="FormAction">URL the client must POST an HTML form to (LiqPay, WayForPay).</param>
    /// <param name="FormFields">Fields of that form. Array values must be sent as name[] fields.</param>
    /// <param name="ExternalId">Provider-side id of the checkout (e.g. Stripe session id), if known.</param>
    public sealed record PaymentCheckout(
        string Provider,
        string? RedirectUrl,
        string? FormAction,
        IReadOnlyDictionary<string, object>? FormFields,
        string? ExternalId);

    /// <summary>Transport independent copy of an incoming webhook request.</summary>
    /// <param name="RawBody">Raw body exactly as received (needed for signature checks). Empty for form posts.</param>
    /// <param name="Form">Form fields for application/x-www-form-urlencoded posts.</param>
    /// <param name="Headers">Request headers (case-insensitive).</param>
    public sealed record GatewayRequest(
        string RawBody,
        IReadOnlyDictionary<string, string> Form,
        IReadOnlyDictionary<string, string> Headers);

    /// <summary>
    /// A webhook whose signature has already been verified.
    /// Status == Pending means "valid event, nothing to change".
    /// </summary>
    /// <param name="PaymentId">Our payment id, or null when the event carries no reference to it.</param>
    /// <param name="Acknowledgement">Body the provider expects in the HTTP response (WayForPay), or null.</param>
    /// <param name="IsTestEvent">True when the provider flags the event as a test event (Stripe livemode = false).</param>
    public sealed record GatewayCallback(
        int? PaymentId,
        PaymentStatus Status,
        decimal Amount,
        string Currency,
        string? ExternalTransactionId,
        object? Acknowledgement = null,
        bool IsTestEvent = false);

    public interface IPaymentGateway
    {
        string ProviderName { get; }

        PaymentProvider Provider { get; }

        Task<PaymentCheckout> CreateCheckoutAsync(
            Payment payment,
            string returnUrl,
            string callbackUrl,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifies the signature of an incoming webhook and extracts the payment result.
        /// Throws UnauthorizedException when the signature is wrong or missing,
        /// ValidationFailedException when the payload is malformed.
        /// Returns null for events this application does not care about.
        /// </summary>
        GatewayCallback? ParseCallback(GatewayRequest request);
    }
}