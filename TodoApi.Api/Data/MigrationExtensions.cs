using Microsoft.EntityFrameworkCore;

namespace TodoApi.Api.Data;

public static class MigrationExtensions
{
    /// <summary>
    /// Applies pending EF migrations on startup. Safe here because the app is a single instance over a
    /// local SQLite file; with multiple replicas or a shared server DB, run migrations as a separate step instead.
    /// </summary>
    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
        var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();
        if (pending.Count == 0)
        {
            return;
        }

        await db.Database.MigrateAsync();
        app.Logger.LogInformation("Applied {Count} database migration(s): {Migrations}", pending.Count, string.Join(", ", pending));
    }
}
