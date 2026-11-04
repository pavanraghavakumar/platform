using Hospital.Application.Contracts;

namespace Hospital.UnitTests.Setup;

public sealed class FakeOperationalMonitor : IOperationalMonitor
{
    public List<(string Dependency, string CorrelationId)> Failures { get; } = new();
    public void RecordDependencyFailure(string dependency, string correlationId) => Failures.Add((dependency, correlationId));
}
