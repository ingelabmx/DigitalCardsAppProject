using DigitalCards.Application.Abstractions;
using DigitalCards.Web.Security;
using Microsoft.Extensions.Options;

namespace DigitalCards.Web.Pilot;

public sealed class PilotAccessService
{
    private readonly IClientRepository _clients;
    private readonly PilotOptions _options;

    public PilotAccessService(
        IClientRepository clients,
        IOptions<PilotOptions> options)
    {
        _clients = clients;
        _options = options.Value;
    }

    public PilotAccessResult CheckAuthenticatedBusiness(System.Security.Claims.ClaimsPrincipal user)
    {
        return CheckBusiness(BusinessAuth.GetBusinessId(user), BusinessAuth.GetBusinessEmail(user));
    }

    public PilotAccessResult CheckBusiness(Guid businessId, string businessEmail)
    {
        if (!_options.Enabled)
        {
            return PilotAccessResult.Allowed;
        }

        if (IsAllowedBusinessId(businessId) || IsAllowedEmail(businessEmail, _options.AllowedBusinessEmails))
        {
            return PilotAccessResult.Allowed;
        }

        return PilotAccessResult.Blocked(_options.BlockedBusinessMessage);
    }

    public async Task<PilotAccessResult> CheckClientAsync(
        string userNameOrEmail,
        CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return PilotAccessResult.Allowed;
        }

        var value = userNameOrEmail.Trim();
        if (IsEmail(value))
        {
            return CheckClientEmail(value);
        }

        var client = await _clients.FindByUserNameOrEmailAsync(value, cancellationToken);
        return client is null ? PilotAccessResult.Allowed : CheckClientEmail(client.Email);
    }

    private PilotAccessResult CheckClientEmail(string email)
    {
        if (IsAllowedEmail(email, _options.AllowedClientEmails) ||
            IsAllowedEmailDomain(email, _options.AllowedClientEmailDomains))
        {
            return PilotAccessResult.Allowed;
        }

        return PilotAccessResult.Blocked(_options.BlockedClientMessage);
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

    private static bool IsAllowedEmailDomain(string email, IEnumerable<string> allowedDomains)
    {
        var atIndex = email.LastIndexOf('@');
        if (atIndex < 0 || atIndex == email.Length - 1)
        {
            return false;
        }

        var domain = Normalize(email[(atIndex + 1)..]);
        return allowedDomains
            .Select(NormalizeDomain)
            .Any(allowedDomain => string.Equals(domain, allowedDomain, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsEmail(string value)
    {
        return value.Contains('@', StringComparison.Ordinal) &&
            value.IndexOf('@') > 0 &&
            value.IndexOf('@') < value.Length - 1;
    }

    private static string Normalize(string value)
    {
        return value.Trim().ToLowerInvariant();
    }

    private static string NormalizeDomain(string value)
    {
        return Normalize(value).TrimStart('@');
    }
}
