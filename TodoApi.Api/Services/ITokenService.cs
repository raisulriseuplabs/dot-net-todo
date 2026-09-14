using TodoApi.Api.Models;

namespace TodoApi.Api.Services;

public interface ITokenService
{
    (string AccessToken, DateTime ExpiresAt) CreateToken(User user);
}
