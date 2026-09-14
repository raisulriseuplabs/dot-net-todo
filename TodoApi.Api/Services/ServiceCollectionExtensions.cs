using FluentValidation;
using Microsoft.AspNetCore.Identity;
using TodoApi.Api.Models;

namespace TodoApi.Api.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ITodoService, TodoService>();
        services.AddValidatorsFromAssemblyContaining<TodoService>();
        return services;
    }
}
