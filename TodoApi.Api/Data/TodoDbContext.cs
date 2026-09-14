using Microsoft.EntityFrameworkCore;
using TodoApi.Api.Models;

namespace TodoApi.Api.Data;

public class TodoDbContext(DbContextOptions<TodoDbContext> options) : DbContext(options)
{
    public DbSet<TodoItem> Todos => Set<TodoItem>();
    public DbSet<User> Users => Set<User>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TodoItem>(todo =>
        {
            todo.Property(t => t.Title).IsRequired().HasMaxLength(200);
            todo.Property(t => t.Description).HasMaxLength(2000);
            todo.HasIndex(t => t.IsCompleted);
            todo.HasIndex(t => t.OwnerId);
        });

        modelBuilder.Entity<User>(user =>
        {
            user.Property(u => u.Email).IsRequired().HasMaxLength(256);
            user.HasIndex(u => u.Email).IsUnique();
            user.Property(u => u.DisplayName).IsRequired().HasMaxLength(100);
            user.Property(u => u.PasswordHash).IsRequired();
            user.Property(u => u.Role).HasConversion<string>().HasMaxLength(16);
            user.HasMany(u => u.Todos)
                .WithOne(t => t.Owner)
                .HasForeignKey(t => t.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
