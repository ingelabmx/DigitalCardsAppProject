using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;
using DigitalCards.Web.Security;
using Microsoft.AspNetCore.RateLimiting;

namespace DigitalCards.Web;

public static class AppleWalletWebServiceEndpoints
{
    public static IEndpointRouteBuilder MapAppleWalletWebService(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/apple-wallet/v1")
            .RequireRateLimiting(SecurityRateLimitPolicyNames.WalletPublic);

        group.MapPost(
            "/devices/{deviceLibraryIdentifier}/registrations/{passTypeIdentifier}/{serialNumber}",
            async (
                string deviceLibraryIdentifier,
                string passTypeIdentifier,
                string serialNumber,
                AppleWalletPushTokenRequest request,
                HttpContext httpContext,
                ILoggerFactory loggerFactory,
                IAppleWalletService appleWallet,
                CancellationToken cancellationToken) =>
            {
                var logger = loggerFactory.CreateLogger("AppleWalletWebService");
                if (string.IsNullOrWhiteSpace(request.PushToken))
                {
                    logger.LogWarning(
                        "Apple Wallet registration rejected because push token was missing for pass type {PassTypeIdentifier} serial {SerialNumber}.",
                        passTypeIdentifier,
                        serialNumber);
                    return Results.BadRequest();
                }

                var status = await appleWallet.RegisterDeviceAsync(
                    deviceLibraryIdentifier,
                    passTypeIdentifier,
                    serialNumber,
                    request.PushToken,
                    GetAuthorization(httpContext),
                    cancellationToken);

                logger.LogInformation(
                    "Apple Wallet registration returned {RegistrationStatus} for pass type {PassTypeIdentifier} serial {SerialNumber}.",
                    status,
                    passTypeIdentifier,
                    serialNumber);

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
                ILoggerFactory loggerFactory,
                IAppleWalletService appleWallet,
                CancellationToken cancellationToken) =>
            {
                var logger = loggerFactory.CreateLogger("AppleWalletWebService");
                var status = await appleWallet.UnregisterDeviceAsync(
                    deviceLibraryIdentifier,
                    passTypeIdentifier,
                    serialNumber,
                    GetAuthorization(httpContext),
                    cancellationToken);

                logger.LogInformation(
                    "Apple Wallet unregistration returned {UnregistrationStatus} for pass type {PassTypeIdentifier} serial {SerialNumber}.",
                    status,
                    passTypeIdentifier,
                    serialNumber);

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
                ILoggerFactory loggerFactory,
                IAppleWalletService appleWallet,
                CancellationToken cancellationToken) =>
            {
                var logger = loggerFactory.CreateLogger("AppleWalletWebService");
                var result = await appleWallet.ListUpdatedPassesAsync(
                    deviceLibraryIdentifier,
                    passTypeIdentifier,
                    passesUpdatedSince,
                    cancellationToken);

                logger.LogInformation(
                    "Apple Wallet update check for pass type {PassTypeIdentifier} returned {UpdatedPassCount} updated passes.",
                    passTypeIdentifier,
                    result?.SerialNumbers.Count ?? 0);

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
                ILoggerFactory loggerFactory,
                IAppleWalletService appleWallet,
                CancellationToken cancellationToken) =>
            {
                var logger = loggerFactory.CreateLogger("AppleWalletWebService");
                var result = await appleWallet.CreateUpdatedPassAsync(
                    passTypeIdentifier,
                    serialNumber,
                    GetAuthorization(httpContext),
                    cancellationToken);

                logger.LogInformation(
                    "Apple Wallet updated pass request returned {PassRequestStatus} for pass type {PassTypeIdentifier} serial {SerialNumber}.",
                    result.Status,
                    passTypeIdentifier,
                    serialNumber);

                return result.Status switch
                {
                    AppleWalletPassRequestStatus.Ready => Results.File(
                        result.PassFile!.Content,
                        result.PassFile.ContentType,
                        result.PassFile.FileName,
                        lastModified: result.PassFile.LastModified),
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
