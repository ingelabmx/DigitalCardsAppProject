using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;

namespace DigitalCards.Infrastructure.Persistence;

public sealed class InMemoryAppleWalletPassRepository : IAppleWalletPassRepository
{
    private readonly InMemoryDigitalCardsStore _store;

    public InMemoryAppleWalletPassRepository(InMemoryDigitalCardsStore store)
    {
        _store = store;
    }

    public Task UpsertPassAsync(AppleWalletPassRecord pass, CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            var index = _store.AppleWalletPasses.FindIndex(existing => IsSamePass(existing, pass.PassTypeIdentifier, pass.SerialNumber));
            if (index >= 0)
            {
                _store.AppleWalletPasses[index] = pass;
            }
            else
            {
                _store.AppleWalletPasses.Add(pass);
            }
        }

        return Task.CompletedTask;
    }

    public Task<AppleWalletPassRecord?> FindPassAsync(
        string passTypeIdentifier,
        string serialNumber,
        CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            return Task.FromResult(_store.AppleWalletPasses.SingleOrDefault(pass =>
                IsSamePass(pass, passTypeIdentifier, serialNumber)));
        }
    }

    public Task<AppleWalletPassRecord?> FindPassByCardIdAsync(Guid cardId, CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            return Task.FromResult(_store.AppleWalletPasses.SingleOrDefault(pass => pass.CardId == cardId));
        }
    }

    public Task UpdatePassTagAsync(
        string passTypeIdentifier,
        string serialNumber,
        string updateTag,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            var index = _store.AppleWalletPasses.FindIndex(pass => IsSamePass(pass, passTypeIdentifier, serialNumber));
            if (index >= 0)
            {
                var pass = _store.AppleWalletPasses[index];
                var currentTag = long.TryParse(pass.UpdateTag, out var ct) ? ct : 0L;
                var proposedTag = long.TryParse(updateTag, out var pt) ? pt : 0L;
                var finalTag = Math.Max(proposedTag, currentTag + 1).ToString();
                _store.AppleWalletPasses[index] = pass with
                {
                    UpdateTag = finalTag,
                    UpdatedAt = updatedAt
                };
            }
        }

        return Task.CompletedTask;
    }

    public Task UpsertDeviceAsync(AppleWalletDeviceRecord device, CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            var index = _store.AppleWalletDevices.FindIndex(existing =>
                string.Equals(existing.DeviceLibraryIdentifier, device.DeviceLibraryIdentifier, StringComparison.Ordinal));
            if (index >= 0)
            {
                _store.AppleWalletDevices[index] = device;
            }
            else
            {
                _store.AppleWalletDevices.Add(device);
            }
        }

        return Task.CompletedTask;
    }

    public Task<bool> AddRegistrationAsync(
        string deviceLibraryIdentifier,
        string passTypeIdentifier,
        string serialNumber,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            var exists = _store.AppleWalletRegistrations.Any(registration =>
                string.Equals(registration.DeviceLibraryIdentifier, deviceLibraryIdentifier, StringComparison.Ordinal) &&
                string.Equals(registration.PassTypeIdentifier, passTypeIdentifier, StringComparison.Ordinal) &&
                string.Equals(registration.SerialNumber, serialNumber, StringComparison.Ordinal));
            if (exists)
            {
                return Task.FromResult(false);
            }

            _store.AppleWalletRegistrations.Add((deviceLibraryIdentifier, passTypeIdentifier, serialNumber, createdAt));
            return Task.FromResult(true);
        }
    }

    public Task<bool> RemoveRegistrationAsync(
        string deviceLibraryIdentifier,
        string passTypeIdentifier,
        string serialNumber,
        CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            var removed = _store.AppleWalletRegistrations.RemoveAll(registration =>
                string.Equals(registration.DeviceLibraryIdentifier, deviceLibraryIdentifier, StringComparison.Ordinal) &&
                string.Equals(registration.PassTypeIdentifier, passTypeIdentifier, StringComparison.Ordinal) &&
                string.Equals(registration.SerialNumber, serialNumber, StringComparison.Ordinal)) > 0;

            return Task.FromResult(removed);
        }
    }

    public Task DeleteDeviceIfOrphanedAsync(string deviceLibraryIdentifier, CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            if (_store.AppleWalletRegistrations.Any(registration =>
                string.Equals(registration.DeviceLibraryIdentifier, deviceLibraryIdentifier, StringComparison.Ordinal)))
            {
                return Task.CompletedTask;
            }

            _store.AppleWalletDevices.RemoveAll(device =>
                string.Equals(device.DeviceLibraryIdentifier, deviceLibraryIdentifier, StringComparison.Ordinal));
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<AppleWalletPassRecord>> ListUpdatedPassesForDeviceAsync(
        string deviceLibraryIdentifier,
        string passTypeIdentifier,
        string? previousLastUpdated,
        CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            var registrations = _store.AppleWalletRegistrations
                .Where(registration =>
                    string.Equals(registration.DeviceLibraryIdentifier, deviceLibraryIdentifier, StringComparison.Ordinal) &&
                    string.Equals(registration.PassTypeIdentifier, passTypeIdentifier, StringComparison.Ordinal))
                .Select(registration => registration.SerialNumber)
                .ToHashSet(StringComparer.Ordinal);

            var passes = _store.AppleWalletPasses
                .Where(pass =>
                    string.Equals(pass.PassTypeIdentifier, passTypeIdentifier, StringComparison.Ordinal) &&
                    registrations.Contains(pass.SerialNumber) &&
                    IsNewer(pass.UpdateTag, previousLastUpdated))
                .ToArray();

            return Task.FromResult<IReadOnlyList<AppleWalletPassRecord>>(passes);
        }
    }

    public Task<IReadOnlyList<AppleWalletDeviceRecord>> ListDevicesForPassAsync(
        string passTypeIdentifier,
        string serialNumber,
        CancellationToken cancellationToken = default)
    {
        lock (_store.Sync)
        {
            var deviceIds = _store.AppleWalletRegistrations
                .Where(registration =>
                    string.Equals(registration.PassTypeIdentifier, passTypeIdentifier, StringComparison.Ordinal) &&
                    string.Equals(registration.SerialNumber, serialNumber, StringComparison.Ordinal))
                .Select(registration => registration.DeviceLibraryIdentifier)
                .ToHashSet(StringComparer.Ordinal);

            var devices = _store.AppleWalletDevices
                .Where(device => deviceIds.Contains(device.DeviceLibraryIdentifier))
                .ToArray();

            return Task.FromResult<IReadOnlyList<AppleWalletDeviceRecord>>(devices);
        }
    }

    private static bool IsSamePass(AppleWalletPassRecord pass, string passTypeIdentifier, string serialNumber)
    {
        return string.Equals(pass.PassTypeIdentifier, passTypeIdentifier, StringComparison.Ordinal) &&
            string.Equals(pass.SerialNumber, serialNumber, StringComparison.Ordinal);
    }

    private static bool IsNewer(string updateTag, string? previousLastUpdated)
    {
        if (string.IsNullOrWhiteSpace(previousLastUpdated))
        {
            return true;
        }

        return long.TryParse(updateTag, out var current) &&
            long.TryParse(previousLastUpdated, out var previous) &&
            current > previous;
    }
}
