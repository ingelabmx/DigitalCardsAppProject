namespace DigitalCards.Application.Services;

public sealed class ClientPasswordHashSubject
{
    public ClientPasswordHashSubject(Guid clientId)
    {
        ClientId = clientId;
    }

    public Guid ClientId { get; }
}
