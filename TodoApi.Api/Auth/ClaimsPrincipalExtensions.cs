using System.Globalization;
using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using TodoApi.Api.Models;

namespace TodoApi.Api.Auth;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Only valid after authentication succeeded, i.e. inside an endpoint marked RequireAuthorization().
    /// Throws if the token was not issued by <see cref="Services.TokenService"/>.
    /// </summary>
    public static CurrentUser ToCurrentUser(this ClaimsPrincipal principal)
    {
        var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? throw new InvalidOperationException("Authenticated principal has no 'sub' claim.");
        var role = principal.FindFirstValue(AuthConstants.RoleClaim) ?? nameof(UserRole.User);

        return new CurrentUser(int.Parse(sub, CultureInfo.InvariantCulture), Enum.Parse<UserRole>(role));
    }
}
