using TodoApi.Api.Auth;
using TodoApi.Api.Dtos;

namespace TodoApi.Api.Services;

/// <summary>All operations are scoped to <paramref name="user"/>: non-admins only ever see their own todos.</summary>
public interface ITodoService
{
    Task<PagedResponse<TodoResponse>> GetAllAsync(CurrentUser user, bool? isCompleted, int page, int pageSize, CancellationToken ct);
    Task<TodoResponse?> GetByIdAsync(CurrentUser user, int id, CancellationToken ct);
    Task<TodoResponse> CreateAsync(CurrentUser user, CreateTodoRequest request, CancellationToken ct);
    Task<bool> UpdateAsync(CurrentUser user, int id, UpdateTodoRequest request, CancellationToken ct);
    Task<bool> CompleteAsync(CurrentUser user, int id, CancellationToken ct);
    Task<bool> DeleteAsync(CurrentUser user, int id, CancellationToken ct);
}
