using Microsoft.EntityFrameworkCore;
using TodoApi.Api.Models;

namespace TodoApi.Api.Data;

public class TodoDbContext(DbContextOptions<TodoDbContext> options) : DbContext(options)
{
    public DbSet<TodoItem> Todos => Set<TodoItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TodoItem>(todo =>
        {
            todo.Property(t => t.Title).IsRequired().HasMaxLength(200);
            todo.Property(t => t.Description).HasMaxLength(2000);
            todo.HasIndex(t => t.IsCompleted);
        });
    }
}
