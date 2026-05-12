namespace DigitalCards.Application.Services;

public sealed class AdminPasswordHashSubject
{
    public AdminPasswordHashSubject(Guid adminUserId)
    {
        AdminUserId = adminUserId;
    }

    public Guid AdminUserId { get; }
}
