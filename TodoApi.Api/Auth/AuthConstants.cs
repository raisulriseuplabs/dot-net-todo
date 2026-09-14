namespace TodoApi.Api.Auth;

public static class AuthConstants
{
    /// <summary>Authorization policy name: requires the Admin role.</summary>
    public const string AdminPolicy = "Admin";

    /// <summary>JWT claim that carries the <see cref="Models.UserRole"/> name.</summary>
    public const string RoleClaim = "role";
}
