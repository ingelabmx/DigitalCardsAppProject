namespace DigitalCards.Application.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

