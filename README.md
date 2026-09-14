# Todo API

A small, complete REST API for a todo/task list, built with ASP.NET Core 8 Minimal APIs, EF Core and SQLite.
It has JWT authentication with two roles (`User`, `Admin`): users manage their own todos, admins manage
everything including user accounts.

Nothing needs to be installed on your machine except Docker.

## Run it (Docker)

```bash
cp .env.example .env        # then edit JWT_KEY and ADMIN_PASSWORD
docker compose up --build -d
```

That's it. The API is on **http://localhost:8080**, Swagger UI on **http://localhost:8080/swagger**,
and the database lives in a Docker volume (`todo-data`) so it survives restarts.

```bash
docker compose logs -f api   # watch the logs
docker compose down          # stop (keeps the data)
docker compose down -v       # stop and delete the database
```

On first start the app applies its migrations and creates the admin account from `.env`.

## Try it

Log in as the admin and create a todo:

```bash
TOKEN=$(curl -s -X POST http://localhost:8080/api/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"email":"admin@todo.local","password":"<ADMIN_PASSWORD from .env>"}' \
  | python3 -c 'import sys,json;print(json.load(sys.stdin)["accessToken"])')

curl -s -X POST http://localhost:8080/api/todos \
  -H 'Content-Type: application/json' \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"title":"Buy milk","dueDate":"2026-09-20T09:00:00Z"}'

curl -s http://localhost:8080/api/todos -H "Authorization: Bearer $TOKEN"
```

Or open Swagger, call `POST /api/auth/login`, click **Authorize** and paste the `accessToken`.

Anyone can register a normal user with `POST /api/auth/register`; only admins can create other admins.

## Endpoints

| Method | Route | Who | Notes |
|---|---|---|---|
| POST | `/api/auth/register` | anyone | creates a `User` |
| POST | `/api/auth/login` | anyone | returns `{ accessToken, expiresAt, user }` |
| GET | `/api/users/me` | signed in | your own account |
| GET, POST | `/api/users` | Admin | list (paged) / create with any role |
| GET, PUT, DELETE | `/api/users/{id}` | Admin | delete cascades to that user's todos |
| GET, POST | `/api/todos` | signed in | `?isCompleted=&page=&pageSize=` |
| GET, PUT, DELETE | `/api/todos/{id}` | signed in | non-admins only see their own (others are 404) |
| PATCH | `/api/todos/{id}/complete` | signed in | idempotent |
| GET | `/health` | anyone | liveness |

Errors are RFC 7807 `ProblemDetails`; validation failures list the offending fields.

## Configuration

Set via environment variables (double underscore = nesting), or `appsettings*.json` when running from source.

| Variable | Required | Purpose |
|---|---|---|
| `Jwt__Key` | yes, ≥ 32 chars | HMAC key for signing tokens (`openssl rand -base64 48`) |
| `Seed__AdminEmail`, `Seed__AdminPassword` | first start | creates the admin if none exists |
| `ConnectionStrings__TodoDb` | no | default in the image: `Data Source=/app/data/todo.db` |
| `Jwt__ExpiryMinutes` | no | default 60 |
| `Swagger__Enabled` | no | Swagger outside Development (compose sets `true`; use `false` for a real deployment) |

`docker-compose.yml` maps these from `.env` (`JWT_KEY`, `ADMIN_EMAIL`, `ADMIN_PASSWORD`, `SWAGGER_ENABLED`).

## Develop (no .NET SDK needed)

`./dotnet.sh` runs the `dotnet` CLI inside the official SDK container with the repo mounted:

```bash
./dotnet.sh build                          # compile (warnings are errors)
./dotnet.sh test                           # run tests
./dotnet.sh run --project TodoApi.Api      # http://localhost:5000, Swagger on, migrations applied
./dotnet.sh ef migrations add <Name> --project TodoApi.Api
```

Dev credentials come from `TodoApi.Api/appsettings.Development.json` (`admin@todo.local` / `Admin123!`).
`TodoApi.Api/TodoApi.Api.http` has a request per endpoint for the VS Code *REST Client* extension — run the
login request first and the token fills in for the rest.

## Layout

```
TodoApi.Api/
  Program.cs         composition root: services, middleware order, migrations, seeding
  Endpoints/         route groups (/api/auth, /api/users, /api/todos) + ValidationFilter<T>
  Services/          business logic; TodoService.VisibleTo() is the ownership rule
  Auth/              JWT setup, CurrentUser, Swagger Authorize button
  Data/              DbContext, migrations, UTC converter, admin seeder
  Models/ Dtos/ Validators/
TodoApi.Tests/       xUnit (integration tests are the next phase)
Dockerfile           multi-stage: sdk:8.0 build → aspnet:8.0 runtime, non-root
docker-compose.yml   port 8080, todo-data volume, secrets from .env
```

More: [CLAUDE.md](CLAUDE.md) (conventions, phase plan, API contract), [TUTORIAL.md](TUTORIAL.md)
(a walkthrough of the code for newcomers, in Bengali), [docs/architecture.html](docs/architecture.html) (diagrams).
