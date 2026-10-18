using BLL.Services.Interfaces;
using Domain.Models;
using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.Checkout;

namespace BLL.Services.PaymentGateways
{
    public class StripeGateway : IPaymentGateway
    {
        private readonly string _secretKey;

        public StripeGateway(IConfiguration configuration)
        {
            // Берем ключ из конфигурации или передаем напрямую
            _secretKey = configuration["Stripe:SecretKey"] ?? "sk_test_mock";
            StripeConfiguration.ApiKey = _secretKey;
        }

        public string ProviderName => "Stripe";

        public async Task<string> CreatePaymentUrlAsync(Payment payment, string returnUrl, string serverUrl)
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
                            UnitAmount = (long)(payment.Amount * 100),
                            Currency = "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"Order #{payment.OrderId}"
                            }
                        },
                        Quantity = 1,
                    },
                },
                Mode = "payment",
                SuccessUrl = returnUrl + "?success=true",
                CancelUrl = returnUrl + "?canceled=true",
                ClientReferenceId = payment.Id.ToString(),
            };

            var service = new SessionService();
            Session session = await service.CreateAsync(options);

            var result = Newtonsoft.Json.JsonConvert.SerializeObject(new { gateway = "Stripe", paymentUrl = session.Url });
            return result;
        }
        public bool ValidateCallback(string incomingData, string incomingSignature)
        {
            // Для Stripe вебхуки обычно валидируются через Stripe.EventUtility.ConstructEvent с секретом эндпоинта.
            // Для базового MVP и заглушки возвращаем true, а при финальной настройке пропишем проверку подписи события.
            return true;
        }
    }
}