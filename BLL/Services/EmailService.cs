using BLL.Options;
using BLL.Services.Interfaces;
using Microsoft.Extensions.Options;
using MimeKit;
using MailKit.Net.Smtp;


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
                MailKit.Security.SecureSocketOptions.None);

            await smtp.SendAsync(message);

            await smtp.DisconnectAsync(true);
        }
        public async Task SendCodeAsync(string to, string code)
        {
            var message = new MimeMessage();

            message.From.Add(
                MailboxAddress.Parse(_options.From));

            message.To.Add(
                MailboxAddress.Parse(to));

            message.Subject = "Тимчасовий код";

            message.Body = new TextPart("plain")
            {
                Text = $"""
                Вітаю!

                Ось тимчасовий код для вашого запиту на скидання пароля: {code}

                Якщо ви не робили запит на скидання пароля, будь ласка, ігноруйте цей лист.
                """
            };

            using var smtp = new SmtpClient();

            await smtp.ConnectAsync(
                _options.Host,
                _options.Port,
                MailKit.Security.SecureSocketOptions.None);

            await smtp.SendAsync(message);

            await smtp.DisconnectAsync(true);
        }
    }
}
