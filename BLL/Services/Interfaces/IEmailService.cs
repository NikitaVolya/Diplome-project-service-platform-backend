

namespace BLL.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendAsync(string to, string subject, string body);
        Task SendCodeAsync(string to, string code);
    }
}
