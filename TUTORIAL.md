# .NET Core শেখা — Todo API দিয়ে হাতে-কলমে

এই document-টা এই repo-র code ধরে ধরে ASP.NET Core শেখায়। প্রতিটা snippet আসল file থেকে নেওয়া — পাশাপাশি file খুলে পড়লে সবচেয়ে ভালো বোঝা যাবে। Technical শব্দগুলো ইচ্ছে করেই English-এ রাখা, কারণ documentation, error message আর Google search-এ ওগুলো English-এই পাবে।

**কার জন্য:** যারা development-এ নতুন, অথবা অন্য ভাষা জানে কিন্তু .NET প্রথমবার দেখছে।

## সূচি

| অংশ | বিষয় |
|---|---|
| [০](#০-শব্দগুলো-আগে-চিনে-নিই) | .NET, C#, ASP.NET Core, EF Core — কোনটা কী |
| [১](#১-সব-শুরু-হয়-programcs-এ) | `Program.cs` — service register করা আর pipeline বানানো |
| [২](#২-url-থেকে-function-call--endpointstodoendpointscs) | Endpoint — URL কীভাবে function হয় |
| [৩](#৩-একই-জিনিসের-দুই-রূপ--dtos-বনাম-models) | DTO বনাম entity |
| [৪](#৪-আসল-কাজ-যেখানে-হয়--servicestodoservicecs) | Service layer, interface, async, LINQ |
| [৫](#৫-database-এর-সাথে-কথা--datatododbcontextcs) | EF Core, DbContext, migration |
| [৬](#৬-দরজায়-পাহারা--validation-আর-error) | Validation filter, ProblemDetails |
| [৭](#৭-সব-একসাথে-post-apitodos) | একটা POST-এর পুরো যাত্রা |
| [৮](#৮-বদলে-শেখো--একটা-exercise) | হাতে-কলমে exercise |
| [৯](#৯-configuration--app-কোথা-থেকে-settings-পায়) | appsettings, environment |
| [১০](#১০-csproj-file--project-এর-পরিচয়পত্র) | `.csproj`, NuGet, build |
| [১১](#১১-http-টা-একটু-ভালো-করে-বোঝা) | HTTP method, status code, idempotency |
| [১২](#১২-testing--phase-5-এ-যা-আসছে) | Automated test কীভাবে কাজ করে |
| [১৩](#১৩-newcomer-দের-৬টা-common-ভুল) | Common ভুল আর প্রতিকার |
| [১৪](#১৪-যখন-কিছু-ভাঙে--কীভাবে-পড়বে) | Error পড়া, logging |
| [১৫](#১৫-এরপর-কী) | পরের ধাপ |
| [Cheat sheet](#c-cheat-sheet) | এক নজরে C# syntax |

---

## ০. শব্দগুলো আগে চিনে নিই

- **.NET** = runtime (তোমার compile করা code চালায়) + SDK (`dotnet` command, যেটা build করে)। এই project-এ এটা Docker container-এর ভেতরে থাকে; [`dotnet.sh`](dotnet.sh) শুধু সেটার একটা shortcut।
- **C#** = ভাষা। File-এর নাম শেষ হয় `.cs` দিয়ে।
- **ASP.NET Core** = .NET-এর যে অংশটা web app/API বানানোর জন্য। এটা একটা web server দেয় (**Kestrel**) আর HTTP request-কে তোমার code-এর function call-এ বদলানোর machinery দেয়।
- **EF Core** (Entity Framework Core) = database-এর সাথে কথা বলার library, যাতে SQL-এর বদলে C# লিখলেই চলে।
- **Project** (`.csproj`) = একটা build করার মতো unit। এখানে দুটো আছে: `TodoApi.Api` (app) আর `TodoApi.Tests`। **Solution** (`.sln`) শুধু এদের একসাথে রাখে।

---

## ১. সব শুরু হয় `Program.cs`-এ

[Program.cs](TodoApi.Api/Program.cs) খোলো। এটাই app-এর পুরো startup, আর এর ঠিক দুটো ভাগ।

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TodoDbContext>(...);
builder.Services.AddScoped<ITodoService, TodoService>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddProblemDetails();

var app = builder.Build();
```

**ভাগ ১ — `builder.Services`: "আমার app এই জিনিসগুলো ব্যবহার করতে পারবে।"**

একে বলে **Dependency Injection (DI)**। প্রতিটা code নিজে নিজে database connection বানানোর বদলে, তুমি এখানে একবার সব part **register** করে দাও, আর পরে যেকোনো code শুধু বলে "আমাকে একটা `ITodoService` দাও" — .NET নিজেই একটা হাতে ধরিয়ে দেয়। ভাবো এটা একটা toolbox, যেটা startup-এ ভরে রাখা হয়।

`AddScoped` মানে "প্রতিটা HTTP request-এর জন্য নতুন একটা বানাও।" অন্য option:

| Lifetime | কখন নতুন object |
|---|---|
| `AddSingleton` | পুরো app-এর জীবনে একটাই |
| `AddScoped` | প্রতিটা HTTP request-এ একটা |
| `AddTransient` | যতবার চাইবে ততবার নতুন |

```csharp
app.UseExceptionHandler();
app.UseStatusCodePages();
app.MapTodoEndpoints();
app.Run();
```

**ভাগ ২ — `app.Use...` / `app.Map...`: "প্রতিটা request-এর সাথে এই এই হবে।"**

প্রতিটা `Use...` লাইন একটা **middleware** যোগ করে — একটা স্তর, যার ভেতর দিয়ে request ঢোকার সময় যায় আর response বের হওয়ার সময় যায়। ক্রম গুরুত্বপূর্ণ: এটা একটা stack। `MapTodoEndpoints()` তোমার URL-গুলো register করে। `app.Run()` Kestrel চালু করে আর চিরকাল listen করতে থাকে।

যেকোনো ASP.NET Core app-এর mental model এটাই: **service register করো, pipeline বানাও, চালাও।**

---

## ২. URL থেকে function call — `Endpoints/TodoEndpoints.cs`

[TodoEndpoints.cs](TodoApi.Api/Endpoints/TodoEndpoints.cs) খোলো।

```csharp
var group = app.MapGroup("/api/todos");
group.MapGet("/{id:int}", GetById);
group.MapPost("/", Create);
```

`MapGet("/{id:int}", GetById)` বলছে: *`/api/todos/<সংখ্যা>`-তে GET এলে `GetById` function-টা call করো।* `{id:int}` অংশটা একটা **route parameter**, সাথে একটা constraint — `/api/todos/abc` আদৌ match করে না (404)।

এবার handler-টা দেখো:

```csharp
private static async Task<Results<Ok<TodoResponse>, NotFound>> GetById(
    int id, ITodoService service, CancellationToken ct)
{
    var todo = await service.GetByIdAsync(id, ct);
    return todo is null ? TypedResults.NotFound() : TypedResults.Ok(todo);
}
```

জাদুটা parameter-গুলোতে। ASP.NET Core প্রতিটা parameter দেখে বুঝে নেয় কোথা থেকে আনতে হবে:

| Parameter | কোথা থেকে আসে |
|---|---|
| `int id` | route-এর `{id}`-র সাথে মেলে → URL থেকে |
| `ITodoService service` | DI-তে register করা → toolbox থেকে |
| `CancellationToken ct` | বিশেষ: client connection কেটে দিলে signal দেয় |
| `CreateTodoRequest request` (Create-এ) | route-এও নেই, DI-তেও নেই → **JSON body** থেকে |

তুমি নিজে কিছুই parse করো না। Framework-এর পুরো point এটাই।

Return type-টা দেখতে ভয়ঙ্কর, কিন্তু পড়লে সোজা: *এই function হয় `Ok` return করে যার ভেতরে একটা `TodoResponse`, নয়তো `NotFound`।* `TypedResults.Ok(todo)` হয়ে যায় `200` + JSON; `TypedResults.NotFound()` হয়ে যায় `404`।

**`async` / `await`** — এটা সব জায়গায় দেখবে। `await service.GetByIdAsync(...)` মানে "এটা শুরু করো (database-এ যাবে), আর অপেক্ষার সময়টায় এই thread অন্য request সামলাক।" ফল তৈরি হলে পরের লাইন থেকে আবার চলে। সহজ নিয়ম: **যা কিছু database, disk বা network ছোঁয় সেটা `async`, আর সেটাকে `await` করো।**

---

## ৩. একই জিনিসের দুই রূপ — `Dtos/` বনাম `Models/`

এখানে আছে [TodoItem.cs](TodoApi.Api/Models/TodoItem.cs) (**entity** — database table-এ ঠিক যা আছে) আর [TodoResponse.cs](TodoApi.Api/Dtos/TodoResponse.cs), [CreateTodoRequest.cs](TodoApi.Api/Dtos/CreateTodoRequest.cs) (**DTO** — Data Transfer Object, যা network দিয়ে যায়)।

একটা class দিয়েই কেন নয়? কারণ client যেন `{"id": 5, "createdAt": "1999-..."}` পাঠিয়ে সেটা store করাতে না পারে। `CreateTodoRequest`-এ শুধু `Title`, `Description`, `DueDate` আছে — তাই client শুধু এই তিনটাই set করতে *পারে*। বাকিটা server ভরে।

```csharp
public record CreateTodoRequest(string Title, string? Description, DateTime? DueDate);
```

এক লাইনে তিনটা C# জিনিস:

- **`record`** — data রাখার class। এই এক লাইনে constructor, property, equality আর সুন্দর `ToString()` সব পেয়ে গেলে।
- **`string?`** — `?` মানে "null হতে পারে।" শুধু `string` মানে "কখনো null না।" Compiler এটা enforce করে (`Nullable` চালু আছে), যেটা বেশিরভাগ ভাষার সবচেয়ে common crash-টা ঠেকায়।
- **`DateTime?`** — তারিখের জন্য একই ধারণা।

---

## ৪. আসল কাজ যেখানে হয় — `Services/TodoService.cs`

[TodoService.cs](TodoApi.Api/Services/TodoService.cs) খোলো।

```csharp
public class TodoService(TodoDbContext db) : ITodoService
```

- `(TodoDbContext db)` হলো **primary constructor**: "একটা `TodoService` বানাতে একটা `TodoDbContext` লাগবে।" DI নিজেই দিয়ে দেয়। Class-এর ভেতরে `db` এমনিই পাওয়া যায়।
- `: ITodoService` মানে এটা [ITodoService.cs](TodoApi.Api/Services/ITodoService.cs)-এর **interface** implement করে। Interface একটা চুক্তি — শুধু method-এর signature-এর list, কোনো body নেই। Endpoint-গুলো class-এর উপর না, *interface*-এর উপর নির্ভর করে — তাই পরে test-এ একটা নকল `TodoService` বসিয়ে দিতে পারবে endpoint-এ হাত না দিয়েই। এটার অস্তিত্বের কারণ এটাই।

এবার একটা method:

```csharp
public async Task<TodoResponse> CreateAsync(CreateTodoRequest request, CancellationToken ct)
{
    var now = DateTime.UtcNow;
    var todo = new TodoItem { Title = request.Title, ..., CreatedAt = now, UpdatedAt = now };

    db.Todos.Add(todo);
    await db.SaveChangesAsync(ct);
    return TodoResponse.FromEntity(todo);
}
```

উপর থেকে নিচে পড়ো: request থেকে entity বানাও, add করো, save করো, response-এ বদলাও। `Task<TodoResponse>` মানে "একটা `TodoResponse`-এর প্রতিশ্রুতি" — প্রতিটা `async` method এটাই return করে। `var` মানে শুধু "type-টা compiler বুঝে নিক" — `now` স্পষ্টতই `DateTime`।

আর এটায় **LINQ** আছে, C#-এর query language:

```csharp
var query = db.Todos.AsNoTracking();
if (isCompleted is { } completed)
    query = query.Where(t => t.IsCompleted == completed);

var totalCount = await query.CountAsync(ct);
var entities = await query.OrderBy(t => t.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
```

`t => t.IsCompleted == completed` একটা **lambda** — ছোট্ট inline function: "`t` দিলে, তার `IsCompleted` `completed`-এর সমান কি না বলো।" `ToListAsync`-এর আগে কিছুই চলে না — ঠিক সেই মুহূর্তে EF Core পুরো chain-টাকে একটা SQL statement-এ বদলায় (`SELECT ... WHERE IsCompleted = 1 ORDER BY Id LIMIT 20 OFFSET 0`) আর চালায়। **তুমি C# লিখলে; database SQL পেল।**

---

## ৫. Database-এর সাথে কথা — `Data/TodoDbContext.cs`

[TodoDbContext.cs](TodoApi.Api/Data/TodoDbContext.cs) খোলো।

```csharp
public class TodoDbContext(DbContextOptions<TodoDbContext> options) : DbContext(options)
{
    public DbSet<TodoItem> Todos => Set<TodoItem>();
```

**DbContext** হলো database-এর সাথে তোমার session। প্রতিটা `DbSet<T>` একটা table। `db.Todos` *মানেই* `Todos` table, আর `TodoItem`-এর property-গুলো *মানেই* তার column।

`OnModelCreating`-এ সেই rule-গুলো যোগ করো যা C# type দিয়ে বলা যায় না (max length 200, একটা index)।

**Migration** ([Data/Migrations/](TodoApi.Api/Data/Migrations/)) হলো সেতু: `TodoItem` বদলালে চালাও

```bash
./dotnet.sh ef migrations add <Name> --project TodoApi.Api
```

EF তোমার জন্য `CREATE TABLE` / `ALTER TABLE` code বানিয়ে দেয়। Startup-এ (শুধু Development-এ) `MigrateAsync()` যেগুলো এখনো চালানো হয়নি সেগুলো চালায়। SQL DDL তুমি কখনো হাতে লেখো না।

একটা সূক্ষ্ম জিনিস: SQLite-এ timezone রাখার কোনো column type নেই, তাই [UtcDateTimeConverter.cs](TodoApi.Api/Data/UtcDateTimeConverter.cs) প্রতিটা `DateTime`-কে লেখার সময় UTC-তে বদলায় আর পড়ার সময় `Kind = Utc` বসিয়ে দেয় — যাতে JSON-এ সবসময় `Z` সহ সময় যায়।

---

## ৬. দরজায় পাহারা — validation আর error

[ValidationFilter.cs](TodoApi.Api/Endpoints/ValidationFilter.cs) একটা **endpoint filter** — যে code handler-এর *আগে* চলে। এটা `CreateTodoRequest`-টা খুঁজে নেয়, [Validators/](TodoApi.Api/Validators/) থেকে মেলানো validator চালায়, আর fail করলে `Create`-কে না ডেকেই `400` ফেরত দেয়।

```csharp
RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
```

এটা FluentValidation — rule-গুলো ইংরেজির মতো পড়া যায়। `ValidationFilter<T>`-র `<T>` একটা **generic**: একটা class যেটা যেকোনো request type-এর জন্য কাজ করে; কোনটার জন্য সেটা বলো `.AddEndpointFilter<ValidationFilter<CreateTodoRequest>>()` লাইনে।

আর `Program.cs`-এর দুটো middleware:

- `UseExceptionHandler()` — কিছু throw হলে client পরিষ্কার একটা `500` JSON পায়, stack trace না।
- `UseStatusCodePages()` — handler খালি `404` দিলে এটা একটা JSON body যোগ করে, যাতে client সবসময় একই error-এর আকার পায় (**ProblemDetails**, একটা standard format)।

কোন status code কোথায় ঠিক হয়:

| পরিস্থিতি | কে ঠিক করে | Status |
|---|---|---|
| সব ঠিক | Handler (`TypedResults`) | 200 / 201 / 204 |
| Title খালি, বা বেশি লম্বা | `ValidationFilter` + FluentValidation | 400 |
| JSON ভাঙা | Body binding | 400 |
| Content-Type ভুল | Body binding | 415 |
| এই id নেই | Handler (`NotFound()`) | 404 |
| Code-এ crash | `UseExceptionHandler` | 500 |

---

## ৭. সব একসাথে: `POST /api/todos`

Request: `POST /api/todos` body `{"title":"Buy milk"}`

1. Kestrel byte-গুলো পায়।
2. Routing `/api/todos` + POST মেলায় → `Create`।
3. JSON-টা `CreateTodoRequest`-এ বদলায়।
4. `ValidationFilter` title check করে — ঠিক আছে, তাই `Create` call করে।
5. `Create` DI-র কাছে `ITodoService` চায় আর `CreateAsync` call করে।
6. `TodoService` একটা `TodoItem` বানায়, EF Core `INSERT` চালায়, SQLite `Id = 1` দেয়।
7. `TodoService` সেটাকে `TodoResponse`-এ map করে; `Create` `201` return করে `Location` header সহ।
8. Response middleware-এর ভেতর দিয়ে উপরে উঠে বেরিয়ে যায়।

[docs/architecture.html](docs/architecture.html)-এর diagram-গুলো ঠিক এটাই আঁকা — browser-এ খুলে দেখো।

---

## ৮. বদলে শেখো — একটা exercise

Todo-তে একটা **priority** যোগ করো। প্রতিটা layer-এ একটু করে হাত পড়বে, আর এভাবেই সবচেয়ে ভালো বোঝা যায় কোনটা কোনটার সাথে জোড়া:

1. [TodoItem.cs](TodoApi.Api/Models/TodoItem.cs): যোগ করো `public int Priority { get; set; }`
2. [CreateTodoRequest.cs](TodoApi.Api/Dtos/CreateTodoRequest.cs), [UpdateTodoRequest.cs](TodoApi.Api/Dtos/UpdateTodoRequest.cs), [TodoResponse.cs](TodoApi.Api/Dtos/TodoResponse.cs): প্রতিটা record-এ `int Priority` যোগ করো (আর `FromEntity`-তেও)।
3. [TodoService.cs](TodoApi.Api/Services/TodoService.cs): `CreateAsync` আর `UpdateAsync`-এ copy করো। যেখানে যেখানে বাদ পড়বে compiler আঙুল দিয়ে দেখাবে — warnings-as-errors ঠিক এই কাজটাই করে।
4. [Validators/](TodoApi.Api/Validators/): `RuleFor(x => x.Priority).InclusiveBetween(1, 5);`
5. `./dotnet.sh ef migrations add AddPriority --project TodoApi.Api` — generated file-টা দেখো; ওটা একটা `AddColumn` হবে।
6. `./dotnet.sh run --project TodoApi.Api`, তারপর `"priority": 9` দিয়ে POST করো আর validation-কে reject করতে দেখো।

---

## ৯. Configuration — app কোথা থেকে settings পায়

[appsettings.json](TodoApi.Api/appsettings.json) খোলো:

```json
{
  "ConnectionStrings": {
    "TodoDb": "Data Source=todo.db"
  }
}
```

আর `Program.cs`-এ:

```csharp
builder.Configuration.GetConnectionString("TodoDb")
```

**Configuration** মানে — code-এর বাইরে রাখা settings। Database-এর path, port, API key — এগুলো code-এ hardcode করলে production-এ বদলাতে হলে আবার build করতে হবে। তাই JSON file-এ রাখা হয়।

.NET কয়েকটা জায়গা থেকে settings পড়ে, একটার উপর আরেকটা চাপিয়ে (পরেরটা আগেরটাকে override করে):

1. `appsettings.json` — সবার জন্য base
2. `appsettings.Development.json` — শুধু Development-এ
3. **Environment variable** — যেমন `ConnectionStrings__TodoDb=...` (দুইটা underscore = JSON-এর nesting)
4. Command-line — `--environment Production`

**Environment** নামের একটা বিশেষ setting আছে — `ASPNETCORE_ENVIRONMENT`। এটা `Development` হলে `app.Environment.IsDevelopment()` true হয়, তাই Swagger চালু হয় আর migration auto-apply হয়। Production-এ এই দুটোই বন্ধ। `dotnet.sh` script-এ এটা `Development` set করা আছে — তাই local run-এ Swagger দেখা যায়।

---

## ১০. `.csproj` file — project-এর পরিচয়পত্র

[TodoApi.Api.csproj](TodoApi.Api/TodoApi.Api.csproj) খোলো। এটা XML, ছোট, আর build-এর সব কিছু এখানেই:

```xml
<TargetFramework>net8.0</TargetFramework>                <!-- কোন .NET version -->
<Nullable>enable</Nullable>                              <!-- string? vs string enforce করো -->
<TreatWarningsAsErrors>true</TreatWarningsAsErrors>      <!-- warning মানেই build fail -->

<PackageReference Include="FluentValidation" Version="12.1.1" />
```

**`PackageReference`** = **NuGet package**। NuGet হলো .NET-এর package manager (Node-এর npm, Python-এর pip-এর মতো)। `./dotnet.sh add TodoApi.Api package X` চালালে এখানে একটা লাইন যোগ হয়, আর build-এর সময় `~/.nuget/packages`-এ download হয়। এই project-এ package মাত্র ৫টা — EF Core (SQLite + Design), FluentValidation (২টা), Swagger। বাকি সব (Kestrel, routing, JSON, DI) framework-এর ভেতরেই আছে।

`./dotnet.sh build` চালালে যা হয়: `.csproj` পড়ে → package restore করে → সব `.cs` file compile করে একটা `TodoApi.Api.dll` বানায় (`bin/` folder-এ)। `dotnet run` = build + সেই dll চালানো। `bin/` আর `obj/` এজন্যই `.gitignore`-এ — এগুলো output, source না।

---

## ১১. HTTP-টা একটু ভালো করে বোঝা

API লিখতে গেলে HTTP-র কয়েকটা rule মাথায় রাখতে হয়। এই app-এর endpoint-গুলো এই rule মেনেই বানানো:

| Method | মানে | এই app-এ |
|---|---|---|
| `GET` | শুধু পড়ো, কিছু বদলিও না | `GET /api/todos`, `GET /api/todos/1` |
| `POST` | নতুন কিছু বানাও | `POST /api/todos` → `201 Created` |
| `PUT` | পুরোটা replace করো | `PUT /api/todos/1` → `204 No Content` |
| `PATCH` | আংশিক বদলাও | `PATCH /api/todos/1/complete` |
| `DELETE` | মুছে ফেলো | `DELETE /api/todos/1` → `204` |

**Status code** পড়ার সহজ নিয়ম — প্রথম digit-টা দেখো:

- `2xx` — হয়েছে (`200` data সহ, `201` নতুন কিছু তৈরি হয়েছে, `204` হয়েছে কিন্তু ফেরত দেওয়ার কিছু নেই)
- `4xx` — **client-এর ভুল** (`400` পাঠানো data ভুল, `404` এই জিনিস নেই, `415` Content-Type ভুল)
- `5xx` — **server-এর ভুল** (`500` code-এ bug/crash)

একটা গুরুত্বপূর্ণ শব্দ: **idempotent** — একই request বারবার পাঠালে যদি একই ফল হয়। `GET`, `PUT`, `DELETE` idempotent; `POST` না (দুবার POST = দুটো todo)। এজন্যই `PATCH .../complete` "toggle" না বানিয়ে "mark as done" বানানো — দুবার call করলেও একই অবস্থা, নিরাপদ।

[TodoApi.Api.http](TodoApi.Api/TodoApi.Api.http) file-টা VS Code-এ খুললে প্রতিটা request-এর উপরে "Send Request" button দেখবে (REST Client extension লাগবে)। শেখার জন্য এটা curl-এর চেয়ে সহজ — click করো, response দেখো।

---

## ১২. Testing — Phase 5-এ যা আসছে

Phase 4 পর্যন্ত সব curl দিয়ে হাতে check করা হয়েছে। **Automated test** মানে সেই check-গুলোই code হিসেবে লেখা, যাতে `./dotnet.sh test` চালালেই সব একসাথে verify হয়। Code বদলানোর পর কিছু ভাঙল কি না — এক command-এ জানা যায়।

একটা test দেখতে এরকম হবে:

```csharp
public class TodoEndpointsTests(TodoApiFactory factory) : IClassFixture<TodoApiFactory>
{
    [Fact]
    public async Task GetById_returns_404_when_todo_does_not_exist()
    {
        // Arrange — প্রস্তুতি
        var client = factory.CreateClient();

        // Act — কাজটা করো
        var response = await client.GetAsync("/api/todos/999");

        // Assert — ফল মিলিয়ে দেখো
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
```

তিনটা জিনিস:

- **`[Fact]`** — xUnit-কে বলে "এটা একটা test"। Method-এর নামটাই বলে দেয় কী test হচ্ছে — নাম লম্বা হলে সমস্যা নেই, বরং ভালো।
- **Arrange / Act / Assert** — প্রতিটা test-এর তিনটা ধাপ। এই pattern-টা মুখস্থ করে ফেলো।
- **`WebApplicationFactory`** — পুরো app-টা (`Program.cs` থেকে শুরু করে) memory-র ভেতরে চালু করে, কোনো port ছাড়াই। `client.GetAsync(...)` আসল HTTP request-এর মতোই middleware → filter → handler → service → EF সব পার হয়। শুধু database-টা `Data Source=:memory:` — RAM-এ, test শেষে হাওয়া।

তাই test-গুলো "ঠিক এই function ঠিক কাজ করে কি না" না — বরং "এই URL-এ এই request পাঠালে ঠিক এই response আসে কি না"। Client যা দেখবে, সেটাই test হচ্ছে।

---

## ১৩. Newcomer-দের ৬টা common ভুল

(আর এই project-এ কীভাবে এড়ানো হয়েছে)

1. **`await` ভুলে যাওয়া।** `service.GetByIdAsync(id, ct);` (await ছাড়া) লিখলে তুমি result না, একটা `Task` পাবে — আর code এগিয়ে যাবে কাজ শেষ হওয়ার আগেই। Warnings-as-errors এই ভুলটা build-এই ধরে ফেলে।

2. **Entity সরাসরি return করা।** `return todo;` (TodoItem) লিখলে database-এর সব column client-এ চলে যায়, আর পরে column বদলালে API-ও বদলে যায়। এজন্য সবসময় `TodoResponse.FromEntity(todo)`।

3. **সব logic `Program.cs`-এ লেখা।** ছোট app-এ লোভ হয়। কিন্তু ৫টা endpoint-এর পরেই ৩০০ লাইনের file হয়ে যায় যেটা কেউ পড়তে চায় না। Endpoint → Service → Data — এই ভাগটা শুরু থেকেই রাখো।

4. **`DateTime.Now` ব্যবহার করা।** Server যে timezone-এ, সেই সময় দেয়। Server বদলালে data-র মানে বদলে যায়। সবসময় `DateTime.UtcNow`, আর client-কে `Z` দিয়ে পাঠাও — যেটা `UtcDateTimeConverter` নিশ্চিত করে।

5. **Exception দিয়ে 404 handle করা।** `throw new NotFoundException()` লিখে পরে catch করা — ধীর আর জটিল। `return null` → handler-এ `TypedResults.NotFound()` — সোজা, আর compiler-ও বোঝে।

6. **Secret commit করে ফেলা।** Connection string-এ password থাকলে সেটা `appsettings.json`-এ না — environment variable-এ। এখন SQLite বলে password নেই, কিন্তু PostgreSQL-এ গেলে এই rule মনে রেখো।

---

## ১৪. যখন কিছু ভাঙে — কীভাবে পড়বে

**Build error** এরকম দেখায়:

```
TodoService.cs(42,13): error CS0103: The name 'requst' does not exist in the current context
```

পড়ার নিয়ম: `file(line,column): error CODE: message`। Line 42-এ যাও, message পড়ো — এখানে typo (`requst`)। `CS0103` Google করলে হাজারটা answer পাবে। Compiler-এর error-গুলো প্রথমে ভয় লাগে, কিন্তু আসলে এগুলো তোমার সবচেয়ে ভালো বন্ধু — Python/JavaScript-এ এই ভুল runtime-এ crash করত, এখানে চালানোর আগেই ধরা পড়ে।

**Runtime-এ** কিছু বুঝতে হলে **logging** ব্যবহার করো। যেকোনো class-এ `ILogger<T>` inject করা যায় (DI-তে আগে থেকেই আছে):

```csharp
public class TodoService(TodoDbContext db, ILogger<TodoService> logger) : ITodoService
{
    // ...
    logger.LogInformation("Created todo {Id} with title {Title}", todo.Id, todo.Title);
```

`./dotnet.sh run`-এর terminal-এ এটা দেখা যাবে। `{Id}` এই style-টা (string interpolation `$"..."` না) — একে structured logging বলে, পরে log search করতে সুবিধা।

---

## ১৫. এরপর কী

এতক্ষণে যা জানো: request কীভাবে code-এর ভেতর দিয়ে যায়, DI কী, async কেন, EF Core কীভাবে SQL লেখে, validation কোথায় হয়, আর configuration কোথা থেকে আসে। এটা ASP.NET Core-এর মূল ৮০%।

শেখার সবচেয়ে ভালো উপায় এখন হাতে করা:

1. **অংশ ৮-এর exercise** (Priority field যোগ করা) — এখনো না করে থাকলে ওটা আগে করো। প্রতিটা layer ছুঁয়ে দেখলে সবকিছু কীভাবে জোড়া লাগে সেটা বোঝা যায়।
2. **Phase 5** — test লেখা ([CLAUDE.md](CLAUDE.md)-এ plan আছে)। একটা test লিখতে গেলে DI, async আর pipeline একসাথে বুঝতে হয় — তাই এটা সবচেয়ে ভালো পরের পাঠ।
3. তারপর নিজে একটা ছোট feature ভাবো — যেমন todo-তে **tag** যোগ করা (এক todo-তে অনেক tag → এটা EF-এর relationship শেখাবে), বা `GET /api/todos?search=milk` (LINQ-এর `Contains`)।

---

## C# cheat sheet

| দেখেছ | মানে |
|---|---|
| `var x = ...` | type-টা compiler বুঝে নিক |
| `string?` | null হতে পারে; শুধু `string` পারে না |
| `record Foo(int A)` | এক লাইনে ছোট data class |
| `async Task<T>` / `await` | non-blocking; "একটা T-র প্রতিশ্রুতি" |
| `x => x.Title` | lambda — inline function |
| `class Foo(Bar bar)` | primary constructor; ভেতরে `bar` পাওয়া যায় |
| `: IFoo` | `IFoo` interface implement করে |
| `Foo<T>` | generic — যেকোনো type `T`-র জন্য কাজ করে |
| `this IEndpointRouteBuilder app` | extension method — যার জন্য `app.MapTodoEndpoints()` লেখা যায় |
| `is { } x` | null না হলে `x`-এ ধরো (pattern matching) |
| `a ? b : c` | `a` সত্যি হলে `b`, নয়তো `c` |

## দরকারি command

```bash
./dotnet.sh build                                   # compile
./dotnet.sh test                                    # test চালাও
./dotnet.sh run --project TodoApi.Api               # http://localhost:5000/swagger
./dotnet.sh ef migrations add <Name> --project TodoApi.Api
./dotnet.sh format                                  # commit-এর আগে
```

Project-এর নিয়মকানুন আর phase plan [CLAUDE.md](CLAUDE.md)-এ; architecture-এর ছবি [docs/architecture.html](docs/architecture.html)-এ।
