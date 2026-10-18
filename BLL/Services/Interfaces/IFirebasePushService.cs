namespace BLL.Services.Interfaces
{
    public interface IFirebasePushService
    {
        Task<bool> SendPushNotificationAsync(string deviceToken, string title, string body, Dictionary<string, string>? data = null);
        Task<bool> SendMulticastNotificationAsync(IEnumerable<string> deviceTokens, string title, string body, Dictionary<string, string>? data = null);
    }
}