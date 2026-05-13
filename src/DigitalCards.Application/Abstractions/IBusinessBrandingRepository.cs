using DigitalCards.Domain;

namespace DigitalCards.Application.Abstractions;

public interface IBusinessBrandingRepository
{
    Task<BusinessBranding?> FindByBusinessIdAsync(Guid businessId, CancellationToken cancellationToken = default);

    Task UpsertAsync(BusinessBranding branding, CancellationToken cancellationToken = default);
}
