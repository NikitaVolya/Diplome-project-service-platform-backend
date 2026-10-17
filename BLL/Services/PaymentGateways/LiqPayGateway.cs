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
    public sealed class LiqPayGateway : IPaymentGateway
    {
        private const string CheckoutUrl = "https://www.liqpay.ua/api/3/checkout";

        private readonly LiqPayOptions _options;

        public LiqPayGateway(IOptions<LiqPayOptions> options)
        {
            _options = options.Value;
        }

        public string ProviderName => "LiqPay";

        public PaymentProvider Provider => PaymentProvider.LiqPay;

        // LiqPay sandbox merchants use keys that start with "sandbox_".
        private bool UsesSandboxCredentials =>
            _options.PublicKey.StartsWith("sandbox_", StringComparison.Ordinal);

        public Task<PaymentCheckout> CreateCheckoutAsync(
            Payment payment,
            string returnUrl,
            string callbackUrl,
            CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, object>
            {
                ["public_key"] = _options.PublicKey,
                ["version"] = 3,
                ["action"] = "pay",
                ["amount"] = payment.Amount,
                ["currency"] = payment.Currency,
                ["description"] = $"Payment for order #{payment.OrderId}",
                // Our own payment id is the reference we get back in the callback.
                ["order_id"] = payment.Id.ToString(CultureInfo.InvariantCulture),
                ["server_url"] = callbackUrl,
                ["result_url"] = returnUrl
            };

            var data = Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(parameters));
            var signature = Sign(data);

            var checkout = new PaymentCheckout(
                ProviderName,
                null,
                CheckoutUrl,
                new Dictionary<string, object>
                {
                    ["data"] = data,
                    ["signature"] = signature
                },
                null);

            return Task.FromResult(checkout);
        }

        public GatewayCallback? ParseCallback(GatewayRequest request)
        {
            var (data, signature) = ExtractFields(request);

            if (string.IsNullOrEmpty(data) || string.IsNullOrEmpty(signature))
            {
                throw new ValidationFailedException(
                    "LiqPay callback must contain the 'data' and 'signature' fields " +
                    "(application/x-www-form-urlencoded, a JSON body or the query string).");
            }

            if (!SignaturesMatch(Sign(data), signature))
            {
                throw new UnauthorizedException("Invalid webhook signature.");
            }

            try
            {
                using var document = JsonDocument.Parse(Convert.FromBase64String(data));
                var root = document.RootElement;

                var status = MapStatus(root.GetProperty("status").GetString());
                if (status is null)
                {
                    return null;
                }

                var paymentId = int.Parse(root.GetProperty("order_id").ToString(), CultureInfo.InvariantCulture);
                var amount = root.GetProperty("amount").GetDecimal();
                var currency = root.GetProperty("currency").GetString() ?? string.Empty;
                var externalId = root.TryGetProperty("payment_id", out var paymentIdElement)
                    ? paymentIdElement.ToString()
                    : null;

                return new GatewayCallback(paymentId, status.Value, amount, currency, externalId);
            }
            catch (Exception ex) when (
                ex is JsonException or FormatException or KeyNotFoundException or
                InvalidOperationException or OverflowException)
            {
                throw new ValidationFailedException("Malformed LiqPay callback.");
            }
        }

        // ------------------------------------------------------------------ reading the request

        /// <summary>
        /// A real LiqPay callback is application/x-www-form-urlencoded with "data" and "signature",
        /// and that is what the controller puts into request.Form.
        /// For manual testing (Swagger, curl, Postman) the same two values are also accepted from
        /// a JSON body ({"data": "...", "signature": "..."}) and from a urlencoded text body.
        /// None of this weakens security: whatever the source, the signature is verified afterwards.
        /// </summary>
        private static (string? Data, string? Signature) ExtractFields(GatewayRequest request)
        {
            request.Form.TryGetValue("data", out var data);
            request.Form.TryGetValue("signature", out var signature);

            var bodyIsNeeded =
                (string.IsNullOrWhiteSpace(data) || string.IsNullOrWhiteSpace(signature)) &&
                !string.IsNullOrWhiteSpace(request.RawBody);

            if (bodyIsNeeded)
            {
                var body = request.RawBody.Trim();
                var fromBody = body.StartsWith('{') ? ReadJsonFields(body) : ReadUrlEncodedFields(body);

                if (string.IsNullOrWhiteSpace(data))
                {
                    fromBody.TryGetValue("data", out data);
                }

                if (string.IsNullOrWhiteSpace(signature))
                {
                    fromBody.TryGetValue("signature", out signature);
                }
            }

            return (Normalize(data), Normalize(signature));
        }

        private static Dictionary<string, string> ReadJsonFields(string body)
        {
            var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                using var document = JsonDocument.Parse(body);
                if (document.RootElement.ValueKind != JsonValueKind.Object)
                {
                    return fields;
                }

                foreach (var property in document.RootElement.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.String)
                    {
                        fields[property.Name] = property.Value.GetString() ?? string.Empty;
                    }
                }
            }
            catch (JsonException)
            {
                // Not JSON: nothing to read, the caller reports the missing fields.
            }

            return fields;
        }

        private static Dictionary<string, string> ReadUrlEncodedFields(string body)
        {
            var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var pair in body.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var separator = pair.IndexOf('=');
                var key = Decode(separator < 0 ? pair : pair[..separator]);
                var value = separator < 0 ? string.Empty : Decode(pair[(separator + 1)..]);

                fields[key] = value;
            }

            return fields;
        }

        private static string Decode(string value)
        {
            return Uri.UnescapeDataString(value.Replace('+', ' '));
        }

        // Base64 never contains spaces, but a '+' that was pasted without URL-encoding turns into one.
        private static string? Normalize(string? value)
        {
            return value?.Trim().Replace(' ', '+');
        }

        // ------------------------------------------------------------------ status and signature

        private PaymentStatus? MapStatus(string? status)
        {
            return status switch
            {
                "success" => PaymentStatus.Completed,

                // "sandbox" means a TEST payment. It counts only when we really run
                // with sandbox credentials, never in production.
                "sandbox" when UsesSandboxCredentials => PaymentStatus.Completed,

                "failure" or "error" => PaymentStatus.Failed,
                "reversed" => PaymentStatus.Refunded,

                // wait_accept, processing, ... : nothing to do yet.
                _ => null
            };
        }

        // signature = base64( sha1( private_key + data + private_key ) )  (LiqPay documentation)
        private string Sign(string data)
        {
            var payload = Encoding.UTF8.GetBytes(_options.PrivateKey + data + _options.PrivateKey);
            return Convert.ToBase64String(SHA1.HashData(payload));
        }

        private static bool SignaturesMatch(string expected, string actual)
        {
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected),
                Encoding.UTF8.GetBytes(actual));
        }
    }
}
