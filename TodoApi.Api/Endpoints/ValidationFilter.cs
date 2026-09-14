using FluentValidation;

namespace TodoApi.Api.Endpoints;

/// <summary>
/// Runs the registered <see cref="IValidator{T}"/> against the first handler argument of type
/// <typeparamref name="T"/> and short-circuits with 400 <c>ValidationProblemDetails</c> on failure.
/// The validator is resolved per request so scoped validators are supported.
/// </summary>
public class ValidationFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var argument = context.Arguments.OfType<T>().FirstOrDefault();
        if (argument is null)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [string.Empty] = ["A request body is required."],
            });
        }

        var validator = context.HttpContext.RequestServices.GetRequiredService<IValidator<T>>();
        var result = await validator.ValidateAsync(argument, context.HttpContext.RequestAborted);
        if (!result.IsValid)
        {
            return TypedResults.ValidationProblem(result.ToDictionary());
        }

        return await next(context);
    }
}
