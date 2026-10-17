using BLL.Exceptions;
using BLL.Options;
using BLL.Services.Interfaces;
using Domain.Models;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BLL.Services.PaymentGateways
{
    public sealed class WayForPayGateway : IPaymentGateway
    {
        private const string PayUrl = "https://secure.wayforpay.com/pay";

        private readonly WayForPayOptions _options;

        public WayForPayGateway(IOptions<WayForPayOptions> options)
        {
            _options = options.Value;
        }

        public string ProviderName => "WayForPay";

        public PaymentProvider Provider => PaymentProvider.WayForPay;

        public Task<PaymentCheckout> CreateCheckoutAsync(
            Payment payment,
            string returnUrl,
            string callbackUrl,
            CancellationToken cancellationToken = default)
        {
            var orderReference = payment.Id.ToString(CultureInfo.InvariantCulture);
            var orderDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var amount = payment.Amount.ToString("0.00", CultureInfo.InvariantCulture);
            var currency = payment.Currency.ToUpperInvariant();
            var productName = $"Order #{payment.OrderId}";
            const string productCount = "1";

            // Purchase signature (WayForPay documentation):
            // merchantAccount;merchantDomainName;orderReference;orderDate;amount;currency;
            // productName[];productCount[];productPrice[]
            // The previous code omitted orderDate, so WayForPay rejected the signature.
            var stringToSign = string.Join(';', new[]
            {
                _options.MerchantAccount,
                _options.DomainName,
                orderReference,
                orderDate.ToString(CultureInfo.InvariantCulture),
                amount,
                currency,
                productName,
                productCount,
                amount
            });

            var fields = new Dictionary<string, object>
            {
                ["merchantAccount"] = _options.MerchantAccount,
                ["merchantAuthType"] = "SimpleSignature",
                ["merchantDomainName"] = _options.DomainName,
                ["merchantTransactionSecureType"] = "AUTO",
                ["merchantSignature"] = Sign(stringToSign),
                ["apiVersion"] = 1,
                ["orderReference"] = orderReference,
                // Must be the very same value that went into the signature.
                ["orderDate"] = orderDate,
                ["amount"] = amount,
                ["currency"] = currency,
                ["productName"] = new[] { productName },
                ["productPrice"] = new[] { amount },
                ["productCount"] = new[] { productCount },
                ["serviceUrl"] = callbackUrl,
                ["returnUrl"] = returnUrl
            };

            return Task.FromResult(new PaymentCheckout(ProviderName, null, PayUrl, fields, null));
        }

        public GatewayCallback? ParseCallback(GatewayRequest request)
        {
            using var document = ParseJson(request.RawBody);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                throw new ValidationFailedException("Malformed WayForPay callback.");
            }

            // Values are used exactly as received: the signature is computed over their text form.
            var merchantAccount = Raw(root, "merchantAccount");
            var orderReference = Raw(root, "orderReference");
            var amount = Raw(root, "amount");
            var currency = Raw(root, "currency");
            var authCode = Raw(root, "authCode");
            var cardPan = Raw(root, "cardPan");
            var transactionStatus = Raw(root, "transactionStatus");
            var reasonCode = Raw(root, "reasonCode");
            var signature = Raw(root, "merchantSignature");

            // Callback signature (WayForPay documentation):
            // merchantAccount;orderReference;amount;currency;authCode;cardPan;transactionStatus;reasonCode
            var expected = Sign(string.Join(';', new[]
            {
                merchantAccount, orderReference, amount, currency,
                authCode, cardPan, transactionStatus, reasonCode
            }));

            if (!string.Equals(merchantAccount, _options.MerchantAccount, StringComparison.Ordinal) ||
                !SignaturesMatch(expected, signature))
            {
                throw new UnauthorizedException("Invalid webhook signature.");
            }

            if (!int.TryParse(orderReference, NumberStyles.None, CultureInfo.InvariantCulture, out var paymentId) ||
                !decimal.TryParse(amount, NumberStyles.Number, CultureInfo.InvariantCulture, out var paidAmount))
            {
                throw new ValidationFailedException("Malformed WayForPay callback.");
            }

            var status = transactionStatus switch
            {
                "Approved" => PaymentStatus.Completed,
                "Declined" or "Expired" => PaymentStatus.Failed,
                "Refunded" or "Voided" => PaymentStatus.Refunded,

                // InProcess, Pending, WaitingAuthComplete, ...: acknowledge, change nothing.
                _ => PaymentStatus.Pending
            };

            return new GatewayCallback(
                paymentId,
                status,
                paidAmount,
                currency,
                null,
                BuildAcknowledgement(orderReference));
        }

        // WayForPay keeps re-sending the callback until we answer with a signed "accept".
        private object BuildAcknowledgement(string orderReference)
        {
            var time = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var signature = Sign($"{orderReference};accept;{time}");

            return new
            {
                orderReference,
                status = "accept",
                time,
                signature
            };
        }

        private static JsonDocument ParseJson(string body)
        {
            try
            {
                return JsonDocument.Parse(body);
            }
            catch (JsonException)
            {
                throw new ValidationFailedException("Malformed WayForPay callback.");
            }
        }

        private static string Raw(JsonElement root, string name)
        {
            if (!root.TryGetProperty(name, out var value))
            {
                return string.Empty;
            }

            return value.ValueKind switch
            {
                JsonValueKind.String => value.GetString() ?? string.Empty,
                JsonValueKind.Null or JsonValueKind.Undefined => string.Empty,
                _ => value.GetRawText()
            };
        }

        // HMAC_MD5 with the merchant secret key, lowercase hex (WayForPay documentation).
        private string Sign(string message)
        {
            using var hmac = new HMACMD5(Encoding.UTF8.GetBytes(_options.MerchantSecretKey));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private static bool SignaturesMatch(string expected, string actual)
        {
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected),
                Encoding.UTF8.GetBytes(actual.ToLowerInvariant()));
        }
    }
}