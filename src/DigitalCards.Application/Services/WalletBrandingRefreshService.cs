using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;
using DigitalCards.Domain;

namespace DigitalCards.Application.Services;

public sealed class WalletBrandingRefreshService
{
    private const int DefaultLimit = 25;
    private const int MaxLimit = 100;

    private readonly IAppleWalletPassRepository _appleWalletPasses;
    private readonly IAppleWalletService _appleWallet;
    private readonly IBusinessBrandingRepository _businessBranding;
    private readonly IBusinessRepository _businesses;
    private readonly IClientRepository _clients;
    private readonly IClock _clock;
    private readonly IGoogleWalletService _googleWallet;
    private readonly ILoyaltyCardRepository _loyaltyCards;
    private readonly IStampLedgerRepository _stampLedger;

    public WalletBrandingRefreshService(
        IBusinessRepository businesses,
        IBusinessBrandingRepository businessBranding,
        IClientRepository clients,
        ILoyaltyCardRepository loyaltyCards,
        IAppleWalletPassRepository appleWalletPasses,
        IAppleWalletService appleWallet,
        IGoogleWalletService googleWallet,
        IStampLedgerRepository stampLedger,
        IClock clock)
    {
        _businesses = businesses;
        _businessBranding = businessBranding;
        _clients = clients;
        _loyaltyCards = loyaltyCards;
        _appleWalletPasses = appleWalletPasses;
        _appleWallet = appleWallet;
        _googleWallet = googleWallet;
        _stampLedger = stampLedger;
        _clock = clock;
    }

    public async Task<WalletBrandingRefreshResult> RefreshAsync(
        Guid businessId,
        int limit,
        Guid? actorBusinessId,
        CancellationToken cancellationToken = default)
    {
        if (businessId == Guid.Empty)
        {
            return Failed(Guid.Empty, "Negocio no valido.");
        }

        var business = await _businesses.FindByIdAsync(businessId, cancellationToken);
        if (business is null)
        {
            return Failed(businessId, "El negocio no existe.");
        }

        var displayBusiness = await ApplyBrandingAsync(business, cancellationToken);
        var cards = limit <= 0
            ? await _loyaltyCards.ListByBusinessAsync(businessId, cancellationToken)
            : await _loyaltyCards.SearchByBusinessAsync(
                businessId,
                query: string.Empty,
                limit: NormalizeLimit(limit),
                cancellationToken);

        var cardsWithTrackedWallets = 0;
        var googleAttemptedTotal = 0;
        var googleSucceededTotal = 0;
        var appleAttemptedTotal = 0;
        var appleSucceededTotal = 0;
        var safeErrors = new List<string>();

        foreach (var card in cards)
        {
            var client = await _clients.FindByIdAsync(card.ClientId, cancellationToken);
            if (client is null)
            {
                safeErrors.Add("MissingClient");
                continue;
            }

            var applePass = await _appleWalletPasses.FindPassByCardIdAsync(card.Id, cancellationToken);
            var hasGoogle = !string.IsNullOrWhiteSpace(card.GoogleObjectId);
            var hasApple = applePass is not null;
            if (!hasGoogle && !hasApple)
            {
                continue;
            }

            cardsWithTrackedWallets++;
            var cardErrors = new List<string>(capacity: 2);
            var googleAttempted = false;
            var googleSucceeded = false;
            var appleAttempted = false;
            var appleSucceeded = false;

            if (hasGoogle)
            {
                googleAttempted = true;
                googleAttemptedTotal++;
                try
                {
                    await _googleWallet.PatchStampStateAsync(card, client, displayBusiness, cancellationToken);
                    googleSucceeded = true;
                    googleSucceededTotal++;
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    cardErrors.Add(SafeExceptionSummary(exception));
                }
            }

            if (hasApple)
            {
                appleAttempted = true;
                appleAttemptedTotal++;
                try
                {
                    await _appleWallet.NotifyPassUpdatedAsync(card, client, displayBusiness, cancellationToken);
                    appleSucceeded = true;
                    appleSucceededTotal++;
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    cardErrors.Add(SafeExceptionSummary(exception));
                }
            }

            if (cardErrors.Count > 0)
            {
                safeErrors.AddRange(cardErrors);
            }

            await _stampLedger.AddAsync(
                new StampLedgerRecord(
                    0,
                    card.Id,
                    card.BusinessId,
                    card.ClientId,
                    StampLedgerSource.BrandingRefresh,
                    actorBusinessId,
                    card.CurrentStamps,
                    card.CurrentStamps,
                    card.LifetimeStamps,
                    card.LifetimeStamps,
                    card.LastStampedAt,
                    googleAttempted,
                    googleSucceeded,
                    appleAttempted,
                    appleSucceeded,
                    cardErrors.Count == 0
                        ? null
                        : string.Join(", ", cardErrors.Distinct(StringComparer.OrdinalIgnoreCase)),
                    _clock.UtcNow),
                cancellationToken);
        }

        if (cardsWithTrackedWallets == 0)
        {
            safeErrors.Add("NoTrackedWallets");
        }

        return new WalletBrandingRefreshResult(
            businessId,
            cards.Count,
            cardsWithTrackedWallets,
            googleAttemptedTotal,
            googleSucceededTotal,
            appleAttemptedTotal,
            appleSucceededTotal,
            safeErrors.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            ErrorMessage: null);
    }

    private async Task<Business> ApplyBrandingAsync(Business business, CancellationToken cancellationToken)
    {
        var branding = await _businessBranding.FindByBusinessIdAsync(business.Id, cancellationToken);
        if (branding is null)
        {
            return business;
        }

        return new Business(
            business.Id,
            business.Name,
            business.Email,
            business.PasswordHashPlaceholder,
            string.IsNullOrWhiteSpace(branding.LogoPath) ? business.LogoPath : branding.LogoPath,
            branding.PublicName,
            branding.PrimaryColor,
            branding.SecondaryColor,
            branding.ProgramName,
            branding.ProgramDescription,
            branding.CustomFieldColor);
    }

    private static int NormalizeLimit(int limit)
    {
        return Math.Clamp(limit <= 0 ? DefaultLimit : limit, 1, MaxLimit);
    }

    private static WalletBrandingRefreshResult Failed(Guid businessId, string errorMessage)
    {
        return new WalletBrandingRefreshResult(
            businessId,
            CardsScanned: 0,
            CardsWithTrackedWallets: 0,
            GoogleWalletAttempted: 0,
            GoogleWalletSucceeded: 0,
            AppleWalletAttempted: 0,
            AppleWalletSucceeded: 0,
            SafeErrors: [],
            errorMessage);
    }

    private static string SafeExceptionSummary(Exception exception)
    {
        return exception.GetType().Name;
    }
}
