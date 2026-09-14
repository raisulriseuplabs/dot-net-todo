namespace TodoApi.Api.Models;

public class User
{
    public int Id { get; set; }
    public required string Email { get; set; }
    public required string DisplayName { get; set; }
    public required string PasswordHash { get; set; }
    public UserRole Role { get; set; } = UserRole.User;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<TodoItem> Todos { get; set; } = [];

    /// <summary>Emails are compared case-insensitively; store and look them up in one canonical form.</summary>
    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
