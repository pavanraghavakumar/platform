using Hospital.Application.Contracts;

namespace Hospital.Infrastructure.Monitoring;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
