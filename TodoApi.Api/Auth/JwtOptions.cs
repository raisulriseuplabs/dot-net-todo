namespace TodoApi.Api.Auth;

/// <summary>Bound from the "Jwt" configuration section. Key must come from a secret store or environment variable in production.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "TodoApi";
    public string Audience { get; set; } = "TodoApi";
    public string Key { get; set; } = "";
    public int ExpiryMinutes { get; set; } = 60;
}
