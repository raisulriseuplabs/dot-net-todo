using Microsoft.AspNetCore.Http.HttpResults;
using TodoApi.Api.Dtos;
using TodoApi.Api.Services;

namespace TodoApi.Api.Endpoints;

public static class TodoEndpoints
{
    public static IEndpointRouteBuilder MapTodoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/todos")
            .WithTags("Todos")
            .WithOpenApi();

        group.MapGet("/", GetAll).WithName("GetTodos");
        group.MapGet("/{id:int}", GetById).WithName("GetTodoById");
        group.MapPost("/", Create)
            .WithName("CreateTodo")
            .AddEndpointFilter<ValidationFilter<CreateTodoRequest>>()
            .ProducesValidationProblem();
        group.MapPut("/{id:int}", Update)
            .WithName("UpdateTodo")
            .AddEndpointFilter<ValidationFilter<UpdateTodoRequest>>()
            .ProducesValidationProblem();
        group.MapPatch("/{id:int}/complete", Complete).WithName("CompleteTodo");
        group.MapDelete("/{id:int}", Delete).WithName("DeleteTodo");

        return app;
    }

    private static async Task<Ok<PagedResponse<TodoResponse>>> GetAll(
        ITodoService service,
        CancellationToken ct,
        bool? isCompleted = null,
        int page = 1,
        int pageSize = 20)
    {
        var result = await service.GetAllAsync(isCompleted, page, pageSize, ct);
        return TypedResults.Ok(result);
    }

    private static async Task<Results<Ok<TodoResponse>, NotFound>> GetById(
        int id, ITodoService service, CancellationToken ct)
    {
        var todo = await service.GetByIdAsync(id, ct);
        return todo is null ? TypedResults.NotFound() : TypedResults.Ok(todo);
    }

    private static async Task<CreatedAtRoute<TodoResponse>> Create(
        CreateTodoRequest request, ITodoService service, CancellationToken ct)
    {
        var todo = await service.CreateAsync(request, ct);
        return TypedResults.CreatedAtRoute(todo, "GetTodoById", new { id = todo.Id });
    }

    private static async Task<Results<NoContent, NotFound>> Update(
        int id, UpdateTodoRequest request, ITodoService service, CancellationToken ct)
    {
        var updated = await service.UpdateAsync(id, request, ct);
        return updated ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    private static async Task<Results<NoContent, NotFound>> Complete(
        int id, ITodoService service, CancellationToken ct)
    {
        var completed = await service.CompleteAsync(id, ct);
        return completed ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    private static async Task<Results<NoContent, NotFound>> Delete(
        int id, ITodoService service, CancellationToken ct)
    {
        var deleted = await service.DeleteAsync(id, ct);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}
