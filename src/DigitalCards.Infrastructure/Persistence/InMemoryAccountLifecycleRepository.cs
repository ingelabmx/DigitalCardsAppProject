using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;

namespace DigitalCards.Infrastructure.Persistence;

public sealed class InMemoryAccountLifecycleRepository : IAccountLifecycleRepository
{
    private readonly InMemoryDigitalCardsStore _store;

    public InMemoryAccountLifecycleRepository(InMemoryDigitalCardsStore store)
    {
        _store = store;
    }

    public Task<ClientCardLifecycleRecord?> FindCardLifecycleAsync(
        Guid cardId,
        CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            return Task.FromResult(_store.ClientCardStatuses.SingleOrDefault(status => status.CardId == cardId));
        }
    }

    public Task SetCardActiveAsync(
        ClientCardLifecycleRecord status,
        CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            var index = _store.ClientCardStatuses.FindIndex(existing => existing.CardId == status.CardId);
            if (index < 0)
            {
                _store.ClientCardStatuses.Add(status);
            }
            else
            {
                _store.ClientCardStatuses[index] = status;
            }
        }

        return Task.CompletedTask;
    }

    public Task<bool> DeleteBusinessCardAsync(
        Guid businessId,
        Guid cardId,
        CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            return Task.FromResult(DeleteBusinessCardCore(businessId, cardId));
        }
    }

    public Task<bool> DeleteBusinessAsync(
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            var businessRemoved = _store.Businesses.RemoveAll(business => business.Id == businessId) > 0;
            var cardIds = _store.LoyaltyCards
                .Where(card => card.BusinessId == businessId)
                .Select(card => card.Id)
                .ToArray();

            foreach (var cardId in cardIds)
            {
                DeleteBusinessCardCore(businessId, cardId);
            }

            _store.BusinessCredentials.RemoveAll(credential => credential.BusinessId == businessId);
            _store.PilotBusinesses.RemoveAll(access => access.BusinessId == businessId);
            _store.BusinessBranding.RemoveAll(branding => branding.BusinessId == businessId);
            _store.BusinessEnrollmentLinks.RemoveAll(link => link.BusinessId == businessId);
            _store.PasswordResetTokens.RemoveAll(token => token.AccountId == businessId);
            _store.ClientConsents.RemoveAll(consent => consent.BusinessId == businessId);
            _store.CutoverSmokeEvidence.RemoveAll(evidence => evidence.BusinessId == businessId);

            return Task.FromResult(businessRemoved);
        }
    }

    public Task<bool> DeleteClientAsync(
        Guid clientId,
        CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            var clientRemoved = _store.Clients.RemoveAll(client => client.Id == clientId) > 0;
            var cards = _store.LoyaltyCards
                .Where(card => card.ClientId == clientId)
                .Select(card => new { card.BusinessId, card.Id })
                .ToArray();

            foreach (var card in cards)
            {
                DeleteBusinessCardCore(card.BusinessId, card.Id);
            }

            _store.ClientCredentials.RemoveAll(credential => credential.ClientId == clientId);
            _store.PilotClients.RemoveAll(access => access.ClientId == clientId);
            _store.PasswordResetTokens.RemoveAll(token => token.AccountId == clientId);
            _store.ClientConsents.RemoveAll(consent => consent.ClientId == clientId);

            return Task.FromResult(clientRemoved);
        }
    }

    private bool DeleteBusinessCardCore(Guid businessId, Guid cardId)
    {
        var cardRemoved = _store.LoyaltyCards.RemoveAll(card =>
            card.Id == cardId && card.BusinessId == businessId) > 0;
        if (!cardRemoved)
        {
            return false;
        }

        var passes = _store.AppleWalletPasses
            .Where(pass => pass.CardId == cardId)
            .ToArray();

        foreach (var pass in passes)
        {
            _store.AppleWalletRegistrations.RemoveAll(registration =>
                string.Equals(registration.PassTypeIdentifier, pass.PassTypeIdentifier, StringComparison.Ordinal) &&
                string.Equals(registration.SerialNumber, pass.SerialNumber, StringComparison.Ordinal));
        }

        _store.AppleWalletPasses.RemoveAll(pass => pass.CardId == cardId);
        _store.AppleWalletDevices.RemoveAll(device =>
            _store.AppleWalletRegistrations.All(registration =>
                !string.Equals(registration.DeviceLibraryIdentifier, device.DeviceLibraryIdentifier, StringComparison.Ordinal)));
        _store.WalletLinkTokens.RemoveAll(token => token.CardId == cardId);
        _store.StampLedger.RemoveAll(record => record.CardId == cardId);
        _store.ClientCardStatuses.RemoveAll(status => status.CardId == cardId);
        return true;
    }
}
