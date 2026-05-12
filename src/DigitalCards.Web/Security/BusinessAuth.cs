using System.Security.Claims;
using DigitalCards.Application.Models;

namespace DigitalCards.Web.Security;

public static class BusinessAuth
{
    public const string Scheme = "DigitalCards.Business";
    public const string Policy = "BusinessOnly";
    public const string Role = "Business";
    public const string RoleClaim = "Role";
    public const string BusinessIdClaim = "BusinessId";
    public const string BusinessEmailClaim = "BusinessEmail";
    public const string BusinessNameClaim = "BusinessName";

    public static ClaimsPrincipal CreatePrincipal(BusinessDto business)
    {
        var claims = new[]
        {
            new Claim(BusinessIdClaim, business.Id.ToString("D")),
            new Claim(BusinessEmailClaim, business.Email),
            new Claim(BusinessNameClaim, business.Name),
            new Claim(RoleClaim, Role),
            new Claim(ClaimTypes.Role, Role)
        };

        return new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme));
    }

    public static Guid GetBusinessId(ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(BusinessIdClaim);
        return Guid.TryParse(value, out var businessId)
            ? businessId
            : throw new InvalidOperationException("Authenticated business claim is missing or invalid.");
    }

    public static string GetBusinessName(ClaimsPrincipal user)
    {
        return user.FindFirstValue(BusinessNameClaim)
            ?? throw new InvalidOperationException("Authenticated business name claim is missing.");
    }
}
