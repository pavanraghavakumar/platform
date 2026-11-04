using Hospital.Application.Contracts;
using Microsoft.Extensions.Logging;

namespace Hospital.Infrastructure.Messaging;

public sealed class DevelopmentEmailSender : IEmailSender
{
    private static readonly Action<ILogger, string, string, Exception?> LogEmailQueued =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(2001, nameof(SendAsync)),
            "Development email queued for {Recipient} with subject {Subject}");

    private readonly ILogger<DevelopmentEmailSender> logger;

    public DevelopmentEmailSender(ILogger<DevelopmentEmailSender> logger)
    {
        this.logger = logger;
    }

    public Task SendAsync(string recipient, string subject, string textBody, CancellationToken cancellationToken = default)
    {
        LogEmailQueued(logger, recipient, subject, null);
        return Task.CompletedTask;
    }
}
