using BLL.Exceptions;
using BLL.Options;
using BLL.Services.Interfaces;
using Domain.Models;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using System.Globalization;

namespace BLL.Services.PaymentGateways
{
    public sealed class StripeGateway : IPaymentGateway
    {
        private readonly StripeOptions _options;
        private readonly StripeClient _client;

        public StripeGateway(IOptions<StripeOptions> options)
        {
            _options = options.Value;

            // A dedicated client instead of the static StripeConfiguration.ApiKey,
            // which is global state shared with the whole process.
            _client = new StripeClient(_options.SecretKey);
        }

        public string ProviderName => "Stripe";

        public PaymentProvider Provider => PaymentProvider.Stripe;

        public async Task<PaymentCheckout> CreateCheckoutAsync(
            Payment payment,
            string returnUrl,
            string callbackUrl,
            CancellationToken cancellationToken = default)
        {
            var options = new SessionCreateOptions
            {
                AllowedPaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = ToMinorUnits(payment.Amount),
                            Currency = payment.Currency.ToLowerInvariant(),
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"Order #{payment.OrderId}"
                            }
                        },
                        Quantity = 1,
                    },
                },
                Mode = "payment",
                SuccessUrl = AppendQuery(returnUrl, "success=true&session_id={CHECKOUT_SESSION_ID}"),
                CancelUrl = AppendQuery(returnUrl, "canceled=true"),
                // Our own payment id travels with the session and comes back in the webhook.
                ClientReferenceId = payment.Id.ToString(CultureInfo.InvariantCulture),
                Metadata = new Dictionary<string, string>
                {
                    ["paymentId"] = payment.Id.ToString(CultureInfo.InvariantCulture),
                    ["orderId"] = payment.OrderId.ToString(CultureInfo.InvariantCulture)
                }
            };

            var session = await new SessionService(_client)
                .CreateAsync(options, cancellationToken: cancellationToken);

            return new PaymentCheckout(ProviderName, session.Url, null, null, session.Id);
        }

        public GatewayCallback? ParseCallback(GatewayRequest request)
        {
            request.Headers.TryGetValue("Stripe-Signature", out var signatureHeader);

            Event stripeEvent;
            try
            {
                // Verifies the HMAC signature AND the timestamp tolerance (replay protection).
                // throwOnApiVersionMismatch is off: otherwise valid events would be rejected
                // whenever the account API version differs from the one Stripe.net was built for.
                stripeEvent = EventUtility.ConstructEvent(
                    request.RawBody,
                    signatureHeader ?? string.Empty,
                    _options.WebhookSecret,
                    throwOnApiVersionMismatch: false);
            }
            catch (StripeException)
            {
                throw new UnauthorizedException("Invalid webhook signature.");
            }

            if (stripeEvent.Data.Object is not Session session)
            {
                return null;
            }

            PaymentStatus status;
            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                case "checkout.session.async_payment_succeeded":
                    // For delayed payment methods "completed" can still be unpaid.
                    if (!string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase))
                    {
                        return null;
                    }
                    status = PaymentStatus.Completed;
                    break;

                case "checkout.session.async_payment_failed":
                case "checkout.session.expired":
                    status = PaymentStatus.Failed;
                    break;

                default:
                    return null;
            }

            var amount = (session.AmountTotal ?? 0L) / 100m;
            var currency = (session.Currency ?? string.Empty).ToUpperInvariant();

            // A session without a reference to our payment is NOT an error: it is either an event we did not
            // create (e.g. "stripe trigger checkout.session.completed", whose fixture session has no
            // client_reference_id) or something that belongs to another system. The signature is valid, so
            // we hand it over without a payment id and the service decides what to do with it.
            return new GatewayCallback(
                ReadPaymentId(session),
                status,
                amount,
                currency,
                session.PaymentIntentId ?? session.Id,
                null,
                !stripeEvent.Livemode);
        }

        // client_reference_id first, metadata.paymentId as a second source.
        private static int? ReadPaymentId(Session session)
        {
            if (int.TryParse(session.ClientReferenceId, NumberStyles.None, CultureInfo.InvariantCulture, out var fromReference))
            {
                return fromReference;
            }

            if (session.Metadata != null &&
                session.Metadata.TryGetValue("paymentId", out var rawMetadataValue) &&
                int.TryParse(rawMetadataValue, NumberStyles.None, CultureInfo.InvariantCulture, out var fromMetadata))
            {
                return fromMetadata;
            }

            return null;
        }

        // Stripe expects the smallest currency unit (kopecks / cents); round, never truncate.
        private static long ToMinorUnits(decimal amount)
        {
            return (long)Math.Round(amount * 100m, 0, MidpointRounding.AwayFromZero);
        }

        private static string AppendQuery(string url, string query)
        {
            return url.Contains('?') ? $"{url}&{query}" : $"{url}?{query}";
        }
    }
}
