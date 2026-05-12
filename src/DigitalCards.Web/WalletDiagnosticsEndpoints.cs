using DigitalCards.Application.Abstractions;

namespace DigitalCards.Web;

public static class WalletDiagnosticsEndpoints
{
    public static IEndpointRouteBuilder MapWalletDiagnostics(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
            "/internal/wallet-diagnostics/{cardId}",
            async (
                string cardId,
                IConfiguration configuration,
                IBusinessRepository businesses,
                IClientRepository clients,
                ILoyaltyCardRepository loyaltyCards,
                IAppleWalletPassRepository applePasses,
                CancellationToken cancellationToken) =>
            {
                if (!configuration.GetValue<bool>("DigitalCards:Diagnostics:EnableWalletDiagnostics"))
                {
                    return Results.NotFound();
                }

                var card = await loyaltyCards.FindByEnrollmentTokenAsync(cardId, cancellationToken);
                if (card is null)
                {
                    return Results.NotFound();
                }

                var client = await clients.FindByIdAsync(card.ClientId, cancellationToken);
                var business = await businesses.FindByIdAsync(card.BusinessId, cancellationToken);
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
                    lookup = cardId,
                    cardId = card.Id,
                    enrollmentTokenSuffix = Suffix(card.EnrollmentToken),
                    clientUserName = client?.UserName,
                    clientEmail = MaskEmail(client?.Email),
                    businessId = business?.Id,
                    businessName = business?.Name,
                    businessEmail = MaskEmail(business?.Email),
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

    private static string Suffix(string value)
    {
        return value.Length <= 6 ? value : value[^6..];
    }

    private static string? MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var atIndex = email.IndexOf('@');
        if (atIndex <= 1)
        {
            return "***";
        }

        return string.Concat(email[0], "***", email[atIndex..]);
    }
}
