using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;

namespace DigitalCards.Web;

public static class AppleWalletWebServiceEndpoints
{
    public static IEndpointRouteBuilder MapAppleWalletWebService(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/apple-wallet/v1");

        group.MapPost(
            "/devices/{deviceLibraryIdentifier}/registrations/{passTypeIdentifier}/{serialNumber}",
            async (
                string deviceLibraryIdentifier,
                string passTypeIdentifier,
                string serialNumber,
                AppleWalletPushTokenRequest request,
                HttpContext httpContext,
                IAppleWalletService appleWallet,
                CancellationToken cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(request.PushToken))
                {
                    return Results.BadRequest();
                }

                var status = await appleWallet.RegisterDeviceAsync(
                    deviceLibraryIdentifier,
                    passTypeIdentifier,
                    serialNumber,
                    request.PushToken,
                    GetAuthorization(httpContext),
                    cancellationToken);

                return status switch
                {
                    AppleWalletRegistrationStatus.Created => Results.Created(
                        $"/apple-wallet/v1/devices/{deviceLibraryIdentifier}/registrations/{passTypeIdentifier}/{serialNumber}",
                        value: null),
                    AppleWalletRegistrationStatus.AlreadyRegistered => Results.Ok(),
                    AppleWalletRegistrationStatus.Unauthorized => Results.Unauthorized(),
                    AppleWalletRegistrationStatus.NotFound => Results.NotFound(),
                    _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
                };
            });

        group.MapDelete(
            "/devices/{deviceLibraryIdentifier}/registrations/{passTypeIdentifier}/{serialNumber}",
            async (
                string deviceLibraryIdentifier,
                string passTypeIdentifier,
                string serialNumber,
                HttpContext httpContext,
                IAppleWalletService appleWallet,
                CancellationToken cancellationToken) =>
            {
                var status = await appleWallet.UnregisterDeviceAsync(
                    deviceLibraryIdentifier,
                    passTypeIdentifier,
                    serialNumber,
                    GetAuthorization(httpContext),
                    cancellationToken);

                return status switch
                {
                    AppleWalletUnregistrationStatus.Removed => Results.Ok(),
                    AppleWalletUnregistrationStatus.Unauthorized => Results.Unauthorized(),
                    AppleWalletUnregistrationStatus.NotFound => Results.NotFound(),
                    _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
                };
            });

        group.MapGet(
            "/devices/{deviceLibraryIdentifier}/registrations/{passTypeIdentifier}",
            async (
                string deviceLibraryIdentifier,
                string passTypeIdentifier,
                string? passesUpdatedSince,
                IAppleWalletService appleWallet,
                CancellationToken cancellationToken) =>
            {
                var result = await appleWallet.ListUpdatedPassesAsync(
                    deviceLibraryIdentifier,
                    passTypeIdentifier,
                    passesUpdatedSince,
                    cancellationToken);

                return result is null
                    ? Results.NoContent()
                    : Results.Json(new
                    {
                        serialNumbers = result.SerialNumbers,
                        lastUpdated = result.LastUpdated
                    });
            });

        group.MapGet(
            "/passes/{passTypeIdentifier}/{serialNumber}",
            async (
                string passTypeIdentifier,
                string serialNumber,
                HttpContext httpContext,
                IAppleWalletService appleWallet,
                CancellationToken cancellationToken) =>
            {
                var result = await appleWallet.CreateUpdatedPassAsync(
                    passTypeIdentifier,
                    serialNumber,
                    GetAuthorization(httpContext),
                    cancellationToken);

                return result.Status switch
                {
                    AppleWalletPassRequestStatus.Ready => Results.File(
                        result.PassFile!.Content,
                        result.PassFile.ContentType,
                        result.PassFile.FileName),
                    AppleWalletPassRequestStatus.Unauthorized => Results.Unauthorized(),
                    AppleWalletPassRequestStatus.NotFound => Results.NotFound(),
                    _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
                };
            });

        group.MapPost(
            "/log",
            (AppleWalletLogRequest request, ILoggerFactory loggerFactory) =>
            {
                var logger = loggerFactory.CreateLogger("AppleWalletWebService");
                logger.LogInformation("Apple Wallet client submitted {LogCount} log entries.", request.Logs.Length);
                return Results.Ok();
            });

        return endpoints;
    }

    private static string? GetAuthorization(HttpContext httpContext)
    {
        return httpContext.Request.Headers.Authorization.ToString();
    }

    public sealed record AppleWalletPushTokenRequest(string? PushToken);

    public sealed record AppleWalletLogRequest(string[] Logs);
}
