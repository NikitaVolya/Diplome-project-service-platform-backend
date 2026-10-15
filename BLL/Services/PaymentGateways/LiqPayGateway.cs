using System.Security.Cryptography;
using System.Text;
using BLL.Services.Interfaces;
using Domain.Models;
using Newtonsoft.Json;

namespace BLL.Services.PaymentGateways
{
    public class LiqPayGateway : IPaymentGateway
    {
        private readonly string _publicKey;
        private readonly string _privateKey;

        public LiqPayGateway(string publicKey, string privateKey)
        {
            _publicKey = publicKey;
            _privateKey = privateKey;
        }

        public string ProviderName => "LiqPay";

        public Task<string> CreatePaymentUrlAsync(Payment payment, string returnUrl, string serverUrl)
        {
            var param = new Dictionary<string, object>
        {
            { "public_key", _publicKey },
            { "version", 3 },
            { "action", "pay" },
            { "amount", payment.Amount },
            { "currency", "UAH" },
            { "description", $"Payment for order #{payment.OrderId}" },
            { "order_id", payment.Id.ToString() },
            { "server_url", serverUrl },
            { "result_url", returnUrl }
        };

            var jsonParams = JsonConvert.SerializeObject(param);
            var data = Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonParams));
            var signature = SignData(_privateKey + data + _privateKey);

            var paymentFormJson = JsonConvert.SerializeObject(new { data, signature, gateway = "LiqPay" });

            return Task.FromResult(paymentFormJson);
        }

        public bool ValidateCallback(string incomingData, string incomingSignature)
        {
            var calculatedSignature = SignData(_privateKey + incomingData + _privateKey);
            return calculatedSignature == incomingSignature;
        }

        private string SignData(string plainText)
        {
            using var sha1 = SHA1.Create();
            var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(plainText));
            return Convert.ToBase64String(hash);
        }
    }
}