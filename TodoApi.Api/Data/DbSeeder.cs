using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TodoApi.Api.Models;

namespace TodoApi.Api.Data;

public static class DbSeeder
{
    /// <summary>
    /// Creates the first Admin from Seed:AdminEmail / Seed:AdminPassword. Idempotent: does nothing when
    /// the settings are absent or an Admin already exists, so it is safe to run on every startup.
    /// </summary>
    public static async Task SeedAdminAsync(this WebApplication app)
    {
        var email = app.Configuration["Seed:AdminEmail"];
        var password = app.Configuration["Seed:AdminPassword"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
        if (await db.Users.AnyAsync(u => u.Role == UserRole.Admin))
        {
            return;
        }

        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
        var now = DateTime.UtcNow;
        var admin = new User
        {
            Email = User.NormalizeEmail(email),
            DisplayName = "Administrator",
            PasswordHash = string.Empty,
            Role = UserRole.Admin,
            CreatedAt = now,
            UpdatedAt = now,
        };
        admin.PasswordHash = hasher.HashPassword(admin, password);

        db.Users.Add(admin);
        await db.SaveChangesAsync();
        app.Logger.LogInformation("Seeded initial admin user {Email}", admin.Email);
    }
}
