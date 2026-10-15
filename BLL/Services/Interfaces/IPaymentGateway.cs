using Domain.Models;

namespace BLL.Services.Interfaces
{
    public interface IPaymentGateway
    {
        string ProviderName { get; }
        Task<string> CreatePaymentUrlAsync(Payment payment, string returnUrl, string serverUrl);
        bool ValidateCallback(string incomingData, string incomingSignature);
    }
}