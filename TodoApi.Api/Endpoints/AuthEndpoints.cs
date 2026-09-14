using Microsoft.AspNetCore.Http.HttpResults;
using TodoApi.Api.Dtos;
using TodoApi.Api.Services;

namespace TodoApi.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Auth")
            .WithOpenApi();

        group.MapPost("/register", Register)
            .WithName("Register")
            .AddEndpointFilter<ValidationFilter<RegisterRequest>>()
            .ProducesValidationProblem();
        group.MapPost("/login", Login)
            .WithName("Login")
            .AddEndpointFilter<ValidationFilter<LoginRequest>>()
            .ProducesValidationProblem();

        return app;
    }

    /// <summary>Open to anyone; always creates a <c>User</c>-role account. Admins are created via /api/users.</summary>
    private static async Task<Results<CreatedAtRoute<UserResponse>, ProblemHttpResult>> Register(
        RegisterRequest request, IUserService users, CancellationToken ct)
    {
        var result = await users.RegisterAsync(request, ct);
        return result.Error switch
        {
            UserError.None => TypedResults.CreatedAtRoute(result.User!, "GetUserById", new { id = result.User!.Id }),
            _ => TypedResults.Problem(statusCode: StatusCodes.Status409Conflict, title: "Email is already registered."),
        };
    }

    private static async Task<Results<Ok<LoginResponse>, ProblemHttpResult>> Login(
        LoginRequest request, IUserService users, CancellationToken ct)
    {
        var login = await users.LoginAsync(request, ct);
        return login is null
            ? TypedResults.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid email or password.")
            : TypedResults.Ok(login);
    }
}
