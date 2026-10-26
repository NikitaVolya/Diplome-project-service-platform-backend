using BLL.Options;
using BLL.Services.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace BLL.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailOptions _options;

        public EmailService(IOptions<EmailOptions> options)
        {
            _options = options.Value;
        }

        public async Task SendAsync(
            string to,
            string subject,
            string body)
        {
            var message = new MimeMessage();

            message.From.Add(
                MailboxAddress.Parse(_options.From));

            message.To.Add(
                MailboxAddress.Parse(to));

            message.Subject = subject;

            message.Body = new TextPart("plain")
            {
                Text = body
            };

            using var smtp = new SmtpClient();

            await smtp.ConnectAsync(
                _options.Host,
                _options.Port,
                SecureSocketOptions.StartTls);

            await smtp.AuthenticateAsync(
                _options.Username,
                _options.Password);

            await smtp.SendAsync(message);

            await smtp.DisconnectAsync(true);
        }

        public async Task SendCodeAsync(string to, string code)
        {
            await SendAsync(
                to,
                "Тимчасовий код",
                $"""
                Вітаю!

                Ось тимчасовий код для підтвердження вашого запиту: {code}

                Якщо ви не робили цей запит, просто проігноруйте лист.
                """
            );
        }
    }
}