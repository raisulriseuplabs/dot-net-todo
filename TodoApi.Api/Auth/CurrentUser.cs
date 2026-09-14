using TodoApi.Api.Models;

namespace TodoApi.Api.Auth;

/// <summary>The caller's identity as read from a validated token. Services use this to scope data.</summary>
public sealed record CurrentUser(int Id, UserRole Role)
{
    public bool IsAdmin => Role == UserRole.Admin;
}
