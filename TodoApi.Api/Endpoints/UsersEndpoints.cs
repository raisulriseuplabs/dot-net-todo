using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using TodoApi.Api.Auth;
using TodoApi.Api.Dtos;
using TodoApi.Api.Services;

namespace TodoApi.Api.Endpoints;

public static class UsersEndpoints
{
    public static IEndpointRouteBuilder MapUsersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users")
            .WithTags("Users")
            .WithOpenApi()
            .RequireAuthorization();

        group.MapGet("/me", Me).WithName("GetCurrentUser");

        // Everything below additionally requires the Admin role.
        group.MapGet("/", GetAll)
            .WithName("GetUsers")
            .RequireAuthorization(AuthConstants.AdminPolicy);
        group.MapGet("/{id:int}", GetById)
            .WithName("GetUserById")
            .RequireAuthorization(AuthConstants.AdminPolicy);
        group.MapPost("/", Create)
            .WithName("CreateUser")
            .RequireAuthorization(AuthConstants.AdminPolicy)
            .AddEndpointFilter<ValidationFilter<CreateUserRequest>>()
            .ProducesValidationProblem();
        group.MapPut("/{id:int}", Update)
            .WithName("UpdateUser")
            .RequireAuthorization(AuthConstants.AdminPolicy)
            .AddEndpointFilter<ValidationFilter<UpdateUserRequest>>()
            .ProducesValidationProblem();
        group.MapDelete("/{id:int}", Delete)
            .WithName("DeleteUser")
            .RequireAuthorization(AuthConstants.AdminPolicy);

        return app;
    }

    private static async Task<Results<Ok<UserResponse>, NotFound>> Me(
        ClaimsPrincipal principal, IUserService users, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(principal.ToCurrentUser().Id, ct);
        return user is null ? TypedResults.NotFound() : TypedResults.Ok(user);
    }

    private static async Task<Ok<PagedResponse<UserResponse>>> GetAll(
        IUserService users, CancellationToken ct, int page = 1, int pageSize = 20)
    {
        return TypedResults.Ok(await users.GetAllAsync(page, pageSize, ct));
    }

    private static async Task<Results<Ok<UserResponse>, NotFound>> GetById(
        int id, IUserService users, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(id, ct);
        return user is null ? TypedResults.NotFound() : TypedResults.Ok(user);
    }

    private static async Task<Results<CreatedAtRoute<UserResponse>, ProblemHttpResult>> Create(
        CreateUserRequest request, IUserService users, CancellationToken ct)
    {
        var result = await users.CreateAsync(request, ct);
        return result.Error switch
        {
            UserError.None => TypedResults.CreatedAtRoute(result.User!, "GetUserById", new { id = result.User!.Id }),
            _ => TypedResults.Problem(statusCode: StatusCodes.Status409Conflict, title: "Email is already registered."),
        };
    }

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> Update(
        int id, UpdateUserRequest request, ClaimsPrincipal principal, IUserService users, CancellationToken ct)
    {
        var result = await users.UpdateAsync(id, request, principal.ToCurrentUser(), ct);
        return result.Error switch
        {
            UserError.None => TypedResults.NoContent(),
            UserError.NotFound => TypedResults.NotFound(),
            _ => TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, title: "You cannot change your own role."),
        };
    }

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> Delete(
        int id, ClaimsPrincipal principal, IUserService users, CancellationToken ct)
    {
        return await users.DeleteAsync(id, principal.ToCurrentUser(), ct) switch
        {
            UserError.None => TypedResults.NoContent(),
            UserError.NotFound => TypedResults.NotFound(),
            _ => TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, title: "You cannot delete your own account."),
        };
    }
}
