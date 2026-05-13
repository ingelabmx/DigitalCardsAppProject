using DigitalCards.Application.Abstractions;
using DigitalCards.Domain;

namespace DigitalCards.Infrastructure.Persistence;

public sealed class InMemoryBusinessBrandingRepository : IBusinessBrandingRepository
{
    private readonly InMemoryDigitalCardsStore _store;

    public InMemoryBusinessBrandingRepository(InMemoryDigitalCardsStore store)
    {
        _store = store;
    }

    public Task<BusinessBranding?> FindByBusinessIdAsync(
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            return Task.FromResult(_store.BusinessBranding.SingleOrDefault(
                branding => branding.BusinessId == businessId));
        }
    }

    public Task UpsertAsync(BusinessBranding branding, CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            var index = _store.BusinessBranding.FindIndex(existing => existing.BusinessId == branding.BusinessId);
            if (index < 0)
            {
                _store.BusinessBranding.Add(branding);
            }
            else
            {
                _store.BusinessBranding[index] = branding;
            }

            return Task.CompletedTask;
        }
    }
}
