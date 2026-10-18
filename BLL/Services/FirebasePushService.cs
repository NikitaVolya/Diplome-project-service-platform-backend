using BLL.Services.Interfaces;
using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Logging;

namespace BLL.Services
{
    public class FirebasePushService : IFirebasePushService
    {
        private readonly ILogger<FirebasePushService> _logger;

        public FirebasePushService(ILogger<FirebasePushService> logger)
        {
            _logger = logger;
        }

        public async Task<bool> SendPushNotificationAsync(string deviceToken, string title, string body, Dictionary<string, string>? data = null)
        {
            if (string.IsNullOrWhiteSpace(deviceToken))
            {
                _logger.LogWarning("Firebase Push: Device token is empty. Skipping.");
                return false;
            }

            try
            {
                var message = new Message
                {
                    Token = deviceToken,
                    Notification = new Notification
                    {
                        Title = title,
                        Body = body
                    },
                    Data = data
                };

                string response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
                _logger.LogInformation("Firebase Push sent successfully. Response ID: {Response}", response);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send Firebase Push notification to token: {Token}", deviceToken);
                return false;
            }
        }

        public async Task<bool> SendMulticastNotificationAsync(IEnumerable<string> deviceTokens, string title, string body, Dictionary<string, string>? data = null)
        {
            var tokensList = deviceTokens?.Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
            if (tokensList == null || !tokensList.Any())
            {
                _logger.LogWarning("Firebase Push: Tokens list is empty.");
                return false;
            }

            try
            {
                var message = new MulticastMessage
                {
                    Tokens = tokensList,
                    Notification = new Notification
                    {
                        Title = title,
                        Body = body
                    },
                    Data = data
                };

                BatchResponse response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message);
                _logger.LogInformation("Firebase Multicast Push sent. Success count: {SuccessCount}, Failure count: {FailureCount}",
                    response.SuccessCount, response.FailureCount);

                return response.SuccessCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send Firebase Multicast Push notification.");
                return false;
            }
        }
    }
}