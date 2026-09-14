namespace TodoApi.Api.Dtos;

public record LoginResponse(string AccessToken, DateTime ExpiresAt, UserResponse User);
