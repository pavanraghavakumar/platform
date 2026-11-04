namespace Hospital.Application.Contracts;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
