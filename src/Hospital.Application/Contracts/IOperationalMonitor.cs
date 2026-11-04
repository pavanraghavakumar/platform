namespace Hospital.Application.Contracts;

public interface IOperationalMonitor
{
    void RecordDependencyFailure(string dependency, string correlationId);
}
