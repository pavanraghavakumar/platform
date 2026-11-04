using Hospital.Application.Contracts;
using Microsoft.Extensions.Logging;

namespace Hospital.Infrastructure.Monitoring;

public sealed class OperationalMonitor : IOperationalMonitor
{
    private static readonly Action<ILogger, string, string, Exception?> LogDependencyFailure =
        LoggerMessage.Define<string, string>(
            LogLevel.Error,
            new EventId(1001, nameof(RecordDependencyFailure)),
            "Dependency {Dependency} failed. CorrelationId: {CorrelationId}");

    private readonly ILogger<OperationalMonitor> logger;

    public OperationalMonitor(ILogger<OperationalMonitor> logger)
    {
        this.logger = logger;
    }

    public void RecordDependencyFailure(string dependency, string correlationId) =>
        LogDependencyFailure(logger, dependency, correlationId, null);
}
