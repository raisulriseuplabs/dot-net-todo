using TodoApi.Api.Models;

namespace TodoApi.Api.Dtos;

public record CreateUserRequest(string Email, string DisplayName, string Password, UserRole Role);
