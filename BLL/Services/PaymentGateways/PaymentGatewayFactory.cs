using BLL.Services.Interfaces;

namespace BLL.Services.PaymentGateways
{
    public class PaymentGatewayFactory
    {
        private readonly IEnumerable<IPaymentGateway> _gateways;

        public PaymentGatewayFactory(IEnumerable<IPaymentGateway> gateways)
        {
            _gateways = gateways;
        }

        public IPaymentGateway GetGateway(string providerName)
        {
            var gateway = _gateways.FirstOrDefault(g =>
                g.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase));

            if (gateway == null)
            {
                throw new ArgumentException($"Payment provider '{providerName}' is not supported or not registered.");
            }

            return gateway;
        }
    }
}