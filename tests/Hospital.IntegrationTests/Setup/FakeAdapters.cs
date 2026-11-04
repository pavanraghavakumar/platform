using Hospital.Application.Contracts;

namespace Hospital.IntegrationTests.Setup;

public sealed class CapturingEmailSender : IEmailSender
{
    public List<string> Recipients { get; } = new();
    public Task SendAsync(string recipient, string subject, string textBody, CancellationToken cancellationToken = default)
    {
        Recipients.Add(recipient);
        return Task.CompletedTask;
    }
}

public sealed class CapturingOperationalMonitor : IOperationalMonitor
{
    public List<string> Dependencies { get; } = new();
    public void RecordDependencyFailure(string dependency, string correlationId) => Dependencies.Add(dependency);
}
