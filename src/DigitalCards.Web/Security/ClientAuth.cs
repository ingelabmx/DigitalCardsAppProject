using System.Security.Claims;
using DigitalCards.Application.Models;

namespace DigitalCards.Web.Security;

public static class ClientAuth
{
    public const string Scheme = "DigitalCards.Client";
    public const string Policy = "ClientOnly";
    public const string Role = "Client";
    public const string RoleClaim = "Role";
    public const string ClientIdClaim = "ClientId";
    public const string ClientEmailClaim = "ClientEmail";
    public const string ClientUserNameClaim = "ClientUserName";
    public const string ClientNameClaim = "ClientName";

    public static ClaimsPrincipal CreatePrincipal(ClientDto client)
    {
        var fullName = $"{client.FirstName} {client.LastName}".Trim();
        var claims = new[]
        {
            new Claim(ClientIdClaim, client.Id.ToString("D")),
            new Claim(ClientEmailClaim, client.Email),
            new Claim(ClientUserNameClaim, client.UserName),
            new Claim(ClientNameClaim, string.IsNullOrWhiteSpace(fullName) ? client.UserName : fullName),
            new Claim(RoleClaim, Role),
            new Claim(ClaimTypes.Role, Role)
        };

        return new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme));
    }

    public static Guid GetClientId(ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClientIdClaim);
        return Guid.TryParse(value, out var clientId)
            ? clientId
            : throw new InvalidOperationException("Authenticated client claim is missing or invalid.");
    }

    public static string GetClientName(ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClientNameClaim)
            ?? throw new InvalidOperationException("Authenticated client name claim is missing.");
    }

    public static string GetClientUserName(ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClientUserNameClaim)
            ?? throw new InvalidOperationException("Authenticated client username claim is missing.");
    }
}
