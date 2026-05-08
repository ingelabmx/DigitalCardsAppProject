using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;
using DigitalCards.Domain;
using Microsoft.Extensions.Logging;

namespace DigitalCards.Infrastructure.Wallets;

public sealed class FakeGoogleWalletService : IGoogleWalletService
{
    private readonly ILogger<FakeGoogleWalletService> _logger;

    public FakeGoogleWalletService(ILogger<FakeGoogleWalletService> logger)
    {
        _logger = logger;
    }

    public Task<GoogleWalletIssueResult> IssueSaveLinkAsync(
        LoyaltyCard card,
        Client client,
        Business business,
        CancellationToken cancellationToken = default)
    {
        var objectId = $"fake-google-{card.Id:N}";
        var saveUrl = $"https://pay.google.com/gp/v/save/fake-{card.Id:N}";
        _logger.LogInformation("Issued fake Google Wallet object {ObjectId} for card {CardId}.", objectId, card.Id);
        return Task.FromResult(new GoogleWalletIssueResult(objectId, saveUrl));
    }

    public Task PatchStampStateAsync(
        LoyaltyCard card,
        Client client,
        Business business,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Patched fake Google Wallet object {ObjectId} with {CurrentStamps} current stamps.",
            card.GoogleObjectId,
            card.CurrentStamps);

        return Task.CompletedTask;
    }
}

