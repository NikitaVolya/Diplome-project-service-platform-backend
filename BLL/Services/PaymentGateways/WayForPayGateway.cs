using System.Security.Cryptography;
using System.Text;
using BLL.Services.Interfaces;
using Domain.Models;
using Newtonsoft.Json;

namespace BLL.Services.PaymentGateways
{
    public class WayForPayGateway : IPaymentGateway
    {
        private readonly string _merchantAccount;
        private readonly string _merchantSecretKey;
        private readonly string _domainName;

        public WayForPayGateway(string merchantAccount, string merchantSecretKey, string domainName)
        {
            _merchantAccount = merchantAccount;
            _merchantSecretKey = merchantSecretKey;
            _domainName = domainName;
        }

        public string ProviderName => "WayForPay";

        public Task<string> CreatePaymentUrlAsync(Payment payment, string returnUrl, string serverUrl)
        {
            var orderReference = payment.Id.ToString();
            var currency = "UAH";
            var amount = payment.Amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
            var productName = $"Order #{payment.OrderId}";
            var productCount = "1";
            var productPrice = amount;

            var stringToSign = $"{_merchantAccount};{_domainName};{orderReference};{amount};{currency};{productName};{productCount};{productPrice}";
            var signature = SignHmacMd5(stringToSign, _merchantSecretKey);

            var payload = new
            {
                merchantAccount = _merchantAccount,
                merchantDomainName = _domainName,
                authorizationType = "SimpleSignature",
                merchantSignature = signature,
                orderReference = orderReference,
                orderDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                amount = amount,
                currency = currency,
                productName = new[] { productName },
                productPrice = new[] { productPrice },
                productCount = new[] { productCount },
                serviceUrl = serverUrl,
                returnUrl = returnUrl
            };

            var jsonResult = JsonConvert.SerializeObject(new { gateway = "WayForPay", formData = payload });
            return Task.FromResult(jsonResult);
        }

        public bool ValidateCallback(string incomingData, string incomingSignature)
        {
            return true;
        }

        private string SignHmacMd5(string message, string secretKey)
        {
            var keyBytes = Encoding.UTF8.GetBytes(secretKey);
            var messageBytes = Encoding.UTF8.GetBytes(message);

            using var hmac = new HMACMD5(keyBytes);
            var hashBytes = hmac.ComputeHash(messageBytes);

            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }
}