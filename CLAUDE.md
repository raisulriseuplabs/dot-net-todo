# CLAUDE.md — Todo API (.NET)

Simple CRUD REST API for a todo/task list, built with ASP.NET Core Minimal APIs.
This file is the source of truth for how to build, test, and extend the project.

## Current status

- [x] Phase 0 — Environment (Docker-based; no SDK on the host)
- [x] Phase 1 — Scaffold
- [x] Phase 2 — Domain + persistence
- [x] Phase 3 — Endpoints
- [x] Phase 4 — Validation + error handling
- [ ] Phase 5 — Tests
- [ ] Phase 6 — Docker + docs

Tick a phase only after its checkpoint passes. Update this list when you finish a phase.

## Stack (decided — do not change without asking)

| Concern      | Choice                                        |
|--------------|-----------------------------------------------|
| Runtime      | .NET 8 SDK (LTS) via Docker image `mcr.microsoft.com/dotnet/sdk:8.0` — **no SDK on the host**; all `dotnet` commands go through `./dotnet.sh` |
| Web          | ASP.NET Core Minimal APIs (no controllers)    |
| Data         | EF Core 8 + SQLite (`todo.db` in project root, gitignored) |
| Validation   | FluentValidation                              |
| Docs         | Swashbuckle (Swagger UI at `/swagger` in Development) |
| Tests        | xUnit + `WebApplicationFactory<Program>` + SQLite in-memory |
| Container    | Multi-stage Dockerfile, `docker compose up`   |
| EF tooling   | `dotnet-ef` 8.x as a *local* tool (`.config/dotnet-tools.json`), not global |

## Docker-only workflow (important)

`dotnet` is **not installed** on this machine. Never run `dotnet ...` directly — use
`./dotnet.sh ...`, which runs the same command inside the SDK container with:

- the repo mounted at `/src` (files are created with your uid/gid, not root)
- `~/.nuget/packages` mounted as the NuGet cache (restores are fast after the first time)
- `~/.cache/dotnet-sdk-docker` mounted as the container's `HOME` (tool cache persists)
- port `5000` published and `ASPNETCORE_URLS=http://+:5000` set **only** for `run`/`watch`
- `ASPNETCORE_ENVIRONMENT=Development`

Consequences:
- The API is **HTTP only** in development (`http://localhost:5000`). Scaffold with `--no-https`; don't add dev certs.
- If `./dotnet.sh watch` doesn't pick up edits, add `-e DOTNET_USE_POLLING_FILE_WATCHER=1` to the script.
- IDE IntelliSense needs the SDK. Optional: add `.devcontainer/devcontainer.json` pointing at the same
  image so VS Code (Dev Containers extension) opens inside the container. Build/run/test work without it.
- Installing a global tool (`dotnet tool install -g`) is pointless — use the local manifest instead.

## Repository layout (target)

```
dot-net-api/
├── CLAUDE.md
├── README.md
├── TodoApi.sln
├── .gitignore                  # from `dotnet new gitignore`
├── Dockerfile
├── docker-compose.yml
├── TodoApi.Api/
│   ├── Program.cs              # composition root only — keep it short
│   ├── TodoApi.http            # manual request samples for VS Code
│   ├── Models/TodoItem.cs      # EF entity
│   ├── Dtos/                   # CreateTodoRequest, UpdateTodoRequest, TodoResponse
│   ├── Data/TodoDbContext.cs
│   ├── Data/Migrations/
│   ├── Services/ITodoService.cs, TodoService.cs
│   ├── Validators/             # FluentValidation validators for request DTOs
│   └── Endpoints/TodoEndpoints.cs   # MapTodoEndpoints() extension, route group /api/todos
└── TodoApi.Tests/
    ├── TodoApiFactory.cs       # WebApplicationFactory with in-memory SQLite
    └── TodoEndpointsTests.cs
```

## Commands

```bash
./dotnet.sh build                                   # build everything
./dotnet.sh run --project TodoApi.Api               # run API -> http://localhost:5000 (swagger at /swagger)
./dotnet.sh watch --project TodoApi.Api             # run with hot reload
./dotnet.sh test                                    # run all tests
./dotnet.sh test --filter "FullyQualifiedName~Todo" # run a subset
./dotnet.sh ef migrations add <Name> --project TodoApi.Api   # new migration
./dotnet.sh ef database update --project TodoApi.Api         # apply migrations
./dotnet.sh format                                  # format before committing
./dotnet.sh tool restore                            # if `ef` says the tool is missing
docker compose up --build                           # run the published image (Phase 6)
```

Stop a running `./dotnet.sh run` with Ctrl-C; if a container is left behind, `docker ps` + `docker stop <id>`.

## API contract

Base route: `/api/todos`. JSON in/out. IDs are `int`.

| Method | Route                       | Success | Errors        |
|--------|-----------------------------|---------|---------------|
| GET    | `/api/todos?isCompleted=&page=&pageSize=` | 200 list | — |
| GET    | `/api/todos/{id}`           | 200     | 404           |
| POST   | `/api/todos`                | 201 + `Location` | 400 validation |
| PUT    | `/api/todos/{id}`           | 204     | 400, 404      |
| PATCH  | `/api/todos/{id}/complete`  | 204     | 404           |
| DELETE | `/api/todos/{id}`           | 204     | 404           |

`TodoItem`: `Id`, `Title` (required, ≤200 chars), `Description?` (≤2000), `IsCompleted`,
`DueDate?`, `CreatedAt`, `UpdatedAt` (UTC, set server-side).

All error responses use RFC 7807 `ProblemDetails` / `ValidationProblemDetails`.

## Conventions

- Never return EF entities from endpoints — map to `TodoResponse`.
- Never put business logic in `Program.cs`; endpoints call `ITodoService`.
- Endpoint handlers return `Results.*` / `TypedResults.*`, never throw for expected 4xx cases.
- Use `async`/`await` end-to-end; pass `CancellationToken` through to EF.
- Nullable reference types and `TreatWarningsAsErrors` are ON — fix warnings, don't suppress.
- File-scoped namespaces, `var` where the type is obvious, one public type per file.
- Timestamps are `DateTime` in UTC (`DateTime.UtcNow`).
- Configuration via `appsettings.json` + environment variables; no secrets in the repo.
- Every endpoint gets at least: happy-path test, not-found test (where applicable), validation-failure test.

## Phase plan (one phase per session/prompt)

Each phase ends with a checkpoint. Do not start the next phase until the checkpoint passes.

### Phase 0 — Environment  ✅ done
1. `./dotnet.sh` wrapper created; `./dotnet.sh --version` → 8.0.x
2. `git init` (branch `main`), `.gitignore` from `dotnet new gitignore` + `*.db`, `*.db-*`
3. Local tool manifest with `dotnet-ef` 8.0.x (`./dotnet.sh ef --version` works)
- Checkpoint: all of the above verified.

### Phase 1 — Scaffold
1. `./dotnet.sh new sln -n TodoApi`
2. `./dotnet.sh new webapi -n TodoApi.Api --use-minimal-apis --no-https`
3. `./dotnet.sh new xunit -n TodoApi.Tests`, add project reference to `TodoApi.Api`
4. `./dotnet.sh sln add TodoApi.Api TodoApi.Tests`
5. Delete the WeatherForecast sample code.
6. Enable `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` in both csproj files.
- Checkpoint: `./dotnet.sh build` clean, `./dotnet.sh run --project TodoApi.Api` serves Swagger at `http://localhost:5000/swagger`, `./dotnet.sh test` runs (0 or 1 placeholder test).

### Phase 2 — Domain + persistence
1. Add packages to TodoApi.Api: `Microsoft.EntityFrameworkCore.Sqlite`, `Microsoft.EntityFrameworkCore.Design`
2. Create `Models/TodoItem.cs`, `Data/TodoDbContext.cs`, DTOs in `Dtos/`
3. Register `TodoDbContext` in `Program.cs` with connection string `Data Source=todo.db` from `appsettings.json`
4. `./dotnet.sh ef migrations add InitialCreate --project TodoApi.Api`; apply migrations automatically on startup in Development only
- Checkpoint: `./dotnet.sh build` clean, `todo.db` created on `./dotnet.sh run`, migration folder committed.

### Phase 3 — Endpoints
1. `Services/ITodoService.cs` + `TodoService.cs` (all CRUD ops, async, EF-backed)
2. `Endpoints/TodoEndpoints.cs` with `MapTodoEndpoints(this IEndpointRouteBuilder)` using a route group `/api/todos`, `.WithTags("Todos")`, `.WithOpenApi()`
3. Wire in `Program.cs`: `app.MapTodoEndpoints();`
4. Fill `TodoApi.http` with one request per endpoint
- Checkpoint: every route in the API contract table works via Swagger/curl and returns the documented status codes.

### Phase 4 — Validation + error handling
1. Add `FluentValidation` + `FluentValidation.DependencyInjectionExtensions`
2. Validators for `CreateTodoRequest` and `UpdateTodoRequest`; run them in an endpoint filter → 400 `ValidationProblemDetails`
3. `app.UseExceptionHandler()` + `builder.Services.AddProblemDetails()` for unhandled errors → 500 `ProblemDetails`
4. Missing resources → `TypedResults.NotFound()` (no exceptions)
- Checkpoint: POST with empty title → 400 with field errors; GET unknown id → 404 ProblemDetails; no stack traces leak outside Development.

### Phase 5 — Tests
1. `TodoApi.Tests/TodoApiFactory.cs`: `WebApplicationFactory<Program>` overriding the DbContext with SQLite in-memory (`Data Source=:memory:`, keep the connection open)
2. Make `Program` accessible to tests (`public partial class Program { }` at end of `Program.cs`)
3. `TodoEndpointsTests.cs`: one test class, tests per endpoint per convention above
4. Add `Microsoft.AspNetCore.Mvc.Testing` package to the test project
- Checkpoint: `./dotnet.sh test` green, ≥12 tests, no test depends on execution order.

### Phase 6 — Docker + docs
1. Multi-stage `Dockerfile` (sdk:8.0 build → aspnet:8.0 runtime), expose 8080
2. `docker-compose.yml` mounting a volume for `todo.db`
3. Update `README.md`: what it is, how to run locally, how to run with Docker, curl examples, how to test
- Checkpoint: `docker compose up --build` serves the API on `http://localhost:8080/api/todos`.

## Working rules for Claude Code

- Read this file first. If reality diverges from it (different SDK version, moved files), fix the file.
- Always use `./dotnet.sh`, never bare `dotnet` (it does not exist on the host).
- Run `./dotnet.sh build` after every code change and `./dotnet.sh test` before saying a phase is done.
- Report test failures verbatim; never claim green without running.
- Ask before: anything needing sudo, changing the stack table or the SDK image tag, adding auth/other big features not in the plan.
- Commit after each green checkpoint with a message like `Phase 3: add todo CRUD endpoints`. Do not commit `todo.db`, `bin/`, `obj/`.
- Keep `Program.cs` under ~40 lines; if it grows, extract to extension methods.
- Prefer editing existing files over creating new ones; don't add layers (repositories, mediators) that the plan doesn't call for.
