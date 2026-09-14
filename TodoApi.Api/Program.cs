using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using TodoApi.Api.Auth;
using TodoApi.Api.Data;
using TodoApi.Api.Endpoints;
using TodoApi.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerWithJwt();
builder.Services.AddDbContext<TodoDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("TodoDb")));
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
// Development defaults this to true, turning body-binding failures into exceptions (-> 500 via
// UseExceptionHandler). Keep them as plain 400s in every environment; UseStatusCodePages adds the body.
builder.Services.PostConfigure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<TodoDbContext>().Database.MigrateAsync();
}

await app.SeedAdminAsync();

app.MapAuthEndpoints();
app.MapUsersEndpoints();
app.MapTodoEndpoints();

app.Run();
