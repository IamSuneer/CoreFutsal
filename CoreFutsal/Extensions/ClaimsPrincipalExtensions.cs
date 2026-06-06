using System.Security.Claims;

namespace CoreFutsal.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("UserId claim is missing.");
        return Guid.Parse(value);
    }

    public static string GetRole(this ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClaimTypes.Role)
            ?? throw new InvalidOperationException("Role claim is missing.");
    }
}
