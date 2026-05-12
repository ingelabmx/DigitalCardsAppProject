namespace DigitalCards.Application.Services;

public sealed class BusinessPasswordHashSubject
{
    public BusinessPasswordHashSubject(Guid businessId)
    {
        BusinessId = businessId;
    }

    public Guid BusinessId { get; }
}
