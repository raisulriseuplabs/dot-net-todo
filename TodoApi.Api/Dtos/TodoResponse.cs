using TodoApi.Api.Models;

namespace TodoApi.Api.Dtos;

public record TodoResponse(
    int Id,
    string Title,
    string? Description,
    bool IsCompleted,
    DateTime? DueDate,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int OwnerId)
{
    public static TodoResponse FromEntity(TodoItem todo) => new(
        todo.Id,
        todo.Title,
        todo.Description,
        todo.IsCompleted,
        todo.DueDate,
        todo.CreatedAt,
        todo.UpdatedAt,
        todo.OwnerId);
}
