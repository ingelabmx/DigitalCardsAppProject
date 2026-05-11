using DigitalCards.Application.Abstractions;

namespace DigitalCards.Web;

public static class WalletDiagnosticsEndpoints
{
    public static IEndpointRouteBuilder MapWalletDiagnostics(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
            "/internal/wallet-diagnostics/{cardId:int}",
            async (
                int cardId,
                IConfiguration configuration,
                ILoyaltyCardRepository loyaltyCards,
                IAppleWalletPassRepository applePasses,
                CancellationToken cancellationToken) =>
            {
                if (!configuration.GetValue<bool>("DigitalCards:Diagnostics:EnableWalletDiagnostics"))
                {
                    return Results.NotFound();
                }

                var card = await loyaltyCards.FindByEnrollmentTokenAsync(cardId.ToString(), cancellationToken);
                if (card is null)
                {
                    return Results.NotFound();
                }

                var applePass = await applePasses.FindPassByCardIdAsync(card.Id, cancellationToken);
                var appleDeviceCount = 0;
                if (applePass is not null)
                {
                    var devices = await applePasses.ListDevicesForPassAsync(
                        applePass.PassTypeIdentifier,
                        applePass.SerialNumber,
                        cancellationToken);
                    appleDeviceCount = devices.Count;
                }

                return Results.Json(new
                {
                    cardId,
                    currentStamps = card.CurrentStamps,
                    lifetimeStamps = card.LifetimeStamps,
                    lastStampedAt = card.LastStampedAt,
                    googleIssued = !string.IsNullOrWhiteSpace(card.GoogleObjectId),
                    appleTracked = applePass is not null,
                    appleRegisteredDeviceCount = appleDeviceCount,
                    appleUpdatedAt = applePass?.UpdatedAt,
                    appleUpdateTag = applePass?.UpdateTag
                });
            });

        return endpoints;
    }
}
