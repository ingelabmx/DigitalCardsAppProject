using DigitalCards.Application.Abstractions;

namespace DigitalCards.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

