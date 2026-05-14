using DigitalCards.Application.Abstractions;
using DigitalCards.Domain;
using DigitalCards.Web.Security;
using Microsoft.Extensions.Options;

namespace DigitalCards.Web.Pilot;

public sealed class PilotAccessService
{
    private const string InactiveBusinessMessage = "Este negocio esta inactivo en Puntelio. Contacta al administrador.";

    private readonly PilotOptions _options;
    private readonly IPilotBusinessRepository _pilotBusinesses;

    public PilotAccessService(
        IPilotBusinessRepository pilotBusinesses,
        IOptions<PilotOptions> options)
    {
        _pilotBusinesses = pilotBusinesses;
        _options = options.Value;
    }

    public Task<PilotAccessResult> CheckAuthenticatedBusinessAsync(
        System.Security.Claims.ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        return CheckBusinessAsync(
            BusinessAuth.GetBusinessId(user),
            BusinessAuth.GetBusinessEmail(user),
            cancellationToken);
    }

    public async Task<PilotAccessResult> CheckBusinessAsync(
        Guid businessId,
        string businessEmail,
        CancellationToken cancellationToken)
    {
        var access = await _pilotBusinesses.FindByBusinessIdAsync(businessId, cancellationToken);
        if (access?.ActivationStatus == BusinessActivationStatus.Inactive)
        {
            return PilotAccessResult.Blocked(InactiveBusinessMessage);
        }

        if (!_options.Enabled)
        {
            return PilotAccessResult.Allowed;
        }

        if (access?.IsEnabled == true ||
            IsAllowedBusinessId(businessId) ||
            IsAllowedEmail(businessEmail, _options.AllowedBusinessEmails))
        {
            return PilotAccessResult.Allowed;
        }

        return PilotAccessResult.Blocked(_options.BlockedBusinessMessage);
    }

    public async Task<PilotAccessResult> CheckBusinessLoginAsync(
        Guid businessId,
        CancellationToken cancellationToken)
    {
        var access = await _pilotBusinesses.FindByBusinessIdAsync(businessId, cancellationToken);
        return access?.ActivationStatus == BusinessActivationStatus.Inactive
            ? PilotAccessResult.Blocked(InactiveBusinessMessage)
            : PilotAccessResult.Allowed;
    }

    public Task<PilotAccessResult> CheckClientAsync(
        string userNameOrEmail,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(PilotAccessResult.Allowed);
    }

    private bool IsAllowedBusinessId(Guid businessId)
    {
        return _options.AllowedBusinessIds.Any(value =>
            Guid.TryParse(value, out var allowedBusinessId) &&
            allowedBusinessId == businessId);
    }

    private static bool IsAllowedEmail(string email, IEnumerable<string> allowedEmails)
    {
        var normalized = Normalize(email);
        return !string.IsNullOrWhiteSpace(normalized) &&
            allowedEmails.Select(Normalize).Contains(normalized, StringComparer.OrdinalIgnoreCase);
    }

    private static string Normalize(string value)
    {
        return value.Trim().ToLowerInvariant();
    }
}
