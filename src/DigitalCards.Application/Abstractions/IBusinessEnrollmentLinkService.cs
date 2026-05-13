namespace DigitalCards.Application.Abstractions;

public interface IBusinessEnrollmentLinkService
{
    Task<string> CreateOrReplaceTokenAsync(Guid businessId, CancellationToken cancellationToken = default);

    Task<Guid?> ResolveBusinessIdAsync(string token, CancellationToken cancellationToken = default);
}
