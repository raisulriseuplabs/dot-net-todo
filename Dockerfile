# ---- build stage: compile and publish with the full SDK ----------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first so the package layer is cached until the .csproj changes.
COPY TodoApi.Api/TodoApi.Api.csproj TodoApi.Api/
RUN dotnet restore TodoApi.Api/TodoApi.Api.csproj

COPY TodoApi.Api/ TodoApi.Api/
RUN dotnet publish TodoApi.Api/TodoApi.Api.csproj -c Release -o /app/publish --no-restore

# ---- runtime stage: only the ASP.NET runtime and the published output --------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# SQLite file lives here; docker-compose mounts a named volume on it.
RUN mkdir -p /app/data && chown app:app /app/data
USER app

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production \
    ConnectionStrings__TodoDb="Data Source=/app/data/todo.db"

# The base image already sets ASPNETCORE_HTTP_PORTS=8080.
EXPOSE 8080
ENTRYPOINT ["dotnet", "TodoApi.Api.dll"]
