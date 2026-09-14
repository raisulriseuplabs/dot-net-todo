using TodoApi.Api.Models;

namespace TodoApi.Api.Dtos;

public record UserResponse(
    int Id,
    string Email,
    string DisplayName,
    UserRole Role,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static UserResponse FromEntity(User user) => new(
        user.Id,
        user.Email,
        user.DisplayName,
        user.Role,
        user.CreatedAt,
        user.UpdatedAt);
}
