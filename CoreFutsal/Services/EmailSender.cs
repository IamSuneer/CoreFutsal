using CoreFutsal.Services.Interfaces;

namespace CoreFutsal.Services;

public class ConsoleEmailSender : IEmailSender
{
    private readonly ILogger<ConsoleEmailSender> _logger;

    public ConsoleEmailSender(ILogger<ConsoleEmailSender> logger) => _logger = logger;

    public Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        _logger.LogInformation("[EMAIL] To: {To} | Subject: {Subject}\n{Body}", to, subject, body);
        return Task.CompletedTask;
    }
}
