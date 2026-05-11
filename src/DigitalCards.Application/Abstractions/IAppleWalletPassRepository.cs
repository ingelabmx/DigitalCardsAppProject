using DigitalCards.Application.Models;

namespace DigitalCards.Application.Abstractions;

public interface IAppleWalletPassRepository
{
    Task UpsertPassAsync(AppleWalletPassRecord pass, CancellationToken cancellationToken = default);

    Task<AppleWalletPassRecord?> FindPassAsync(
        string passTypeIdentifier,
        string serialNumber,
        CancellationToken cancellationToken = default);

    Task<AppleWalletPassRecord?> FindPassByCardIdAsync(
        Guid cardId,
        CancellationToken cancellationToken = default);

    Task UpdatePassTagAsync(
        string passTypeIdentifier,
        string serialNumber,
        string updateTag,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken = default);

    Task UpsertDeviceAsync(AppleWalletDeviceRecord device, CancellationToken cancellationToken = default);

    Task<bool> AddRegistrationAsync(
        string deviceLibraryIdentifier,
        string passTypeIdentifier,
        string serialNumber,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken = default);

    Task<bool> RemoveRegistrationAsync(
        string deviceLibraryIdentifier,
        string passTypeIdentifier,
        string serialNumber,
        CancellationToken cancellationToken = default);

    Task DeleteDeviceIfOrphanedAsync(
        string deviceLibraryIdentifier,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AppleWalletPassRecord>> ListUpdatedPassesForDeviceAsync(
        string deviceLibraryIdentifier,
        string passTypeIdentifier,
        string? previousLastUpdated,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AppleWalletDeviceRecord>> ListDevicesForPassAsync(
        string passTypeIdentifier,
        string serialNumber,
        CancellationToken cancellationToken = default);
}
