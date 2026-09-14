using System.Globalization;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TodoApi.Api.Auth;
using TodoApi.Api.Models;

namespace TodoApi.Api.Services;

public sealed class TokenService(IOptions<JwtOptions> options) : ITokenService
{
    public (string AccessToken, DateTime ExpiresAt) CreateToken(User user)
    {
        var jwt = options.Value;
        var expiresAt = DateTime.UtcNow.AddMinutes(jwt.ExpiryMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString(CultureInfo.InvariantCulture)),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Name, user.DisplayName),
                new Claim(AuthConstants.RoleClaim, user.Role.ToString()),
            ]),
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                SecurityAlgorithms.HmacSha256),
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);
        return (token, expiresAt);
    }
}
