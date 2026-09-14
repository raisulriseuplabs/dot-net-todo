using Microsoft.EntityFrameworkCore;
using TodoApi.Api.Data;
using TodoApi.Api.Dtos;
using TodoApi.Api.Models;

namespace TodoApi.Api.Services;

public class TodoService(TodoDbContext db) : ITodoService
{
    public const int MaxPageSize = 100;

    public async Task<PagedResponse<TodoResponse>> GetAllAsync(bool? isCompleted, int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var query = db.Todos.AsNoTracking();
        if (isCompleted is { } completed)
        {
            query = query.Where(t => t.IsCompleted == completed);
        }

        var totalCount = await query.CountAsync(ct);
        var entities = await query
            .OrderBy(t => t.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = entities.Select(TodoResponse.FromEntity).ToList();
        return new PagedResponse<TodoResponse>(items, page, pageSize, totalCount);
    }

    public async Task<TodoResponse?> GetByIdAsync(int id, CancellationToken ct)
    {
        var todo = await db.Todos.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);
        return todo is null ? null : TodoResponse.FromEntity(todo);
    }

    public async Task<TodoResponse> CreateAsync(CreateTodoRequest request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var todo = new TodoItem
        {
            Title = request.Title,
            Description = request.Description,
            DueDate = request.DueDate,
            IsCompleted = false,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.Todos.Add(todo);
        await db.SaveChangesAsync(ct);
        return TodoResponse.FromEntity(todo);
    }

    public async Task<bool> UpdateAsync(int id, UpdateTodoRequest request, CancellationToken ct)
    {
        var todo = await db.Todos.FindAsync([id], ct);
        if (todo is null)
        {
            return false;
        }

        todo.Title = request.Title;
        todo.Description = request.Description;
        todo.IsCompleted = request.IsCompleted;
        todo.DueDate = request.DueDate;
        todo.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> CompleteAsync(int id, CancellationToken ct)
    {
        var todo = await db.Todos.FindAsync([id], ct);
        if (todo is null)
        {
            return false;
        }

        if (!todo.IsCompleted)
        {
            todo.IsCompleted = true;
            todo.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        var deleted = await db.Todos.Where(t => t.Id == id).ExecuteDeleteAsync(ct);
        return deleted > 0;
    }
}
