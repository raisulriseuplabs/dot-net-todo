using TodoApi.Api.Dtos;

namespace TodoApi.Api.Services;

public interface ITodoService
{
    Task<PagedResponse<TodoResponse>> GetAllAsync(bool? isCompleted, int page, int pageSize, CancellationToken ct);
    Task<TodoResponse?> GetByIdAsync(int id, CancellationToken ct);
    Task<TodoResponse> CreateAsync(CreateTodoRequest request, CancellationToken ct);
    Task<bool> UpdateAsync(int id, UpdateTodoRequest request, CancellationToken ct);
    Task<bool> CompleteAsync(int id, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
}
