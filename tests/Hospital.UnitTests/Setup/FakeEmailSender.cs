using Hospital.Application.Contracts;

namespace Hospital.UnitTests.Setup;

public sealed class FakeEmailSender : IEmailSender
{
    public List<(string Recipient, string Subject, string Body)> Messages { get; } = new();

    public Task SendAsync(string recipient, string subject, string textBody, CancellationToken cancellationToken = default)
    {
        Messages.Add((recipient, subject, textBody));
        return Task.CompletedTask;
    }
}
