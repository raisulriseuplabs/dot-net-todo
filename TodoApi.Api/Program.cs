using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TodoApi.Api.Data;
using TodoApi.Api.Endpoints;
using TodoApi.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<TodoDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("TodoDb")));
builder.Services.AddScoped<ITodoService, TodoService>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddProblemDetails();
// Development defaults this to true, turning body-binding failures into exceptions (-> 500 via
// UseExceptionHandler). Keep them as plain 400s in every environment; UseStatusCodePages adds the body.
builder.Services.PostConfigure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<TodoDbContext>().Database.MigrateAsync();
}

app.MapTodoEndpoints();

app.Run();
