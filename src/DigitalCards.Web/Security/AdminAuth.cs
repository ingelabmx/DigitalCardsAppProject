using System.Security.Claims;
using DigitalCards.Application.Models;

namespace DigitalCards.Web.Security;

public static class AdminAuth
{
    public const string Scheme = "DigitalCards.Admin";
    public const string Policy = "AdminOnly";
    public const string Role = "Admin";
    public const string RoleClaim = "Role";
    public const string AdminUserIdClaim = "AdminUserId";
    public const string AdminEmailClaim = "AdminEmail";
    public const string AdminNameClaim = "AdminName";

    public static ClaimsPrincipal CreatePrincipal(AdminUserDto admin)
    {
        var claims = new[]
        {
            new Claim(AdminUserIdClaim, admin.Id.ToString("D")),
            new Claim(AdminEmailClaim, admin.Email),
            new Claim(AdminNameClaim, admin.Name),
            new Claim(RoleClaim, Role),
            new Claim(ClaimTypes.Role, Role)
        };

        return new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme));
    }

    public static Guid GetAdminUserId(ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(AdminUserIdClaim);
        return Guid.TryParse(value, out var adminUserId)
            ? adminUserId
            : throw new InvalidOperationException("Authenticated admin claim is missing or invalid.");
    }

    public static string GetAdminName(ClaimsPrincipal user)
    {
        return user.FindFirstValue(AdminNameClaim)
            ?? throw new InvalidOperationException("Authenticated admin name claim is missing.");
    }
}
