namespace TodoApi.Api.Dtos;

public record CreateTodoRequest(string Title, string? Description, DateTime? DueDate);
