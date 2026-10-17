using Microsoft.Extensions.Logging;

namespace BLL.Public;

public interface IVerificationCodeSender
{
    Task SendAsync(string email, CodePurpose purpose, string code, CancellationToken cancellationToken = default);

    bool IsStub { get; }
}

public sealed class LoggingVerificationCodeSender : IVerificationCodeSender
{
    private readonly ILogger<LoggingVerificationCodeSender> _logger;

    public LoggingVerificationCodeSender(ILogger<LoggingVerificationCodeSender> logger) => _logger = logger;

    public bool IsStub => true;

    public Task SendAsync(string email, CodePurpose purpose, string code, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Код підтвердження ({Purpose}) для {Email}: {Code}", purpose, email, code);
        return Task.CompletedTask;
    }
}
