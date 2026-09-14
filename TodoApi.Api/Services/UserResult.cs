using TodoApi.Api.Dtos;

namespace TodoApi.Api.Services;

/// <summary>Outcome of a user mutation: either a <see cref="User"/> or the reason it was refused.</summary>
public sealed record UserResult(UserResponse? User, UserError Error)
{
    public static UserResult Ok(UserResponse user) => new(user, UserError.None);
    public static UserResult Fail(UserError error) => new(null, error);
}
