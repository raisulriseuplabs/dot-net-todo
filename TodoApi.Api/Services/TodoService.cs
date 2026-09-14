using Microsoft.EntityFrameworkCore;
using TodoApi.Api.Auth;
using TodoApi.Api.Data;
using TodoApi.Api.Dtos;
using TodoApi.Api.Models;

namespace TodoApi.Api.Services;

public class TodoService(TodoDbContext db) : ITodoService
{
    public async Task<PagedResponse<TodoResponse>> GetAllAsync(CurrentUser user, bool? isCompleted, int page, int pageSize, CancellationToken ct)
    {
        (page, pageSize) = Paging.Normalize(page, pageSize);

        var query = VisibleTo(user).AsNoTracking();
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

    public async Task<TodoResponse?> GetByIdAsync(CurrentUser user, int id, CancellationToken ct)
    {
        var todo = await VisibleTo(user).AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);
        return todo is null ? null : TodoResponse.FromEntity(todo);
    }

    public async Task<TodoResponse> CreateAsync(CurrentUser user, CreateTodoRequest request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var todo = new TodoItem
        {
            Title = request.Title,
            Description = request.Description,
            DueDate = request.DueDate,
            IsCompleted = false,
            OwnerId = user.Id,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.Todos.Add(todo);
        await db.SaveChangesAsync(ct);
        return TodoResponse.FromEntity(todo);
    }

    public async Task<bool> UpdateAsync(CurrentUser user, int id, UpdateTodoRequest request, CancellationToken ct)
    {
        var todo = await VisibleTo(user).FirstOrDefaultAsync(t => t.Id == id, ct);
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

    public async Task<bool> CompleteAsync(CurrentUser user, int id, CancellationToken ct)
    {
        var todo = await VisibleTo(user).FirstOrDefaultAsync(t => t.Id == id, ct);
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

    public async Task<bool> DeleteAsync(CurrentUser user, int id, CancellationToken ct)
    {
        var deleted = await VisibleTo(user).Where(t => t.Id == id).ExecuteDeleteAsync(ct);
        return deleted > 0;
    }

    /// <summary>
    /// Admins see every todo; everyone else only their own. A todo outside this set behaves as if it
    /// does not exist (404), so callers cannot probe for other users' ids.
    /// </summary>
    private IQueryable<TodoItem> VisibleTo(CurrentUser user) =>
        user.IsAdmin ? db.Todos : db.Todos.Where(t => t.OwnerId == user.Id);
}
