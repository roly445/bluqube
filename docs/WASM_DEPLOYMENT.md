# BluQube WASM Deployment Guide

> **Deploy once, run everywhere.** This guide explains how BluQube bridges Blazor WebAssembly clients and Blazor Server backends using generated requesters and responders.

---

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [The Client-Server Boundary](#the-client-server-boundary)
3. [Project Structure](#project-structure)
4. [Server Setup](#server-setup)
5. [Client (WASM) Setup](#client-wasm-setup)
6. [What NOT to Reference on Client](#what-not-to-reference-on-client)
7. [URL Binding Across the Boundary](#url-binding-across-the-boundary)
8. [Deployment Checklist](#deployment-checklist)
9. [Common Pitfalls](#common-pitfalls)
10. [Complete Working Example](#complete-working-example)

---

## Architecture Overview

BluQube enables "write once, run on Server or WASM" by automatically generating the plumbing between your client and server:

- **Shared Layer:** Command and query *records* defined with attributes. These live in a shared project or referenced by both client and server.
- **Client (WASM):** Uses generated *requesters*—HTTP clients that serialize commands/queries and send them to the server.
- **Server:** Uses generated *responders*—ASP.NET route handlers that deserialize requests, invoke handlers, and return results.

### What Runs Where

| Component | Client (WASM) | Server |
|-----------|---------------|--------|
| Command/Query *definitions* | ✅ | ✅ |
| Handlers & Processors | ❌ | ✅ |
| Validation logic | ❌ | ✅ |
| Authorization checks | ❌ | ✅ |
| Generated *requesters* | ✅ | ❌ |
| Generated *responders* | ❌ | ✅ |
| `ICommandRunner` | ✅ | ✅ |
| `IQueryRunner` | ✅ | ✅ |

The client runner uses the generated requester to make HTTP calls; the server responder receives them, validates, authorizes, and executes handler logic.

---

## The Client-Server Boundary

### How the Boundary Works

1. **Client declares intent:** A Blazor component calls `ICommandRunner.Send(new MyCommand(...))`.
2. **Generator produces requester:** Source generation creates `GenericMyCommandHandler` on the client that knows how to HTTP-serialize the command.
3. **HTTP sends to server:** The command is POSTed to the path defined in `[BluQubeCommand(Path = "...")]`.
4. **Server generator produces responder:** Source generation creates an ASP.NET route handler that knows how to deserialize the command.
5. **Handler executes:** The server-side handler runs, with validation and authorization already applied.
6. **Result returns:** `CommandResult` or `QueryResult<T>` is serialized as JSON and sent back to the client.
7. **Client receives result:** The component can inspect `.IsSucceeded`, `.ValidationResult`, `.ErrorData`, etc.

### Key Attributes

**`[BluQubeRequester]`**

Marks a client-side `Program` class to trigger generation of HTTP requesters for all commands and queries in that assembly.

```csharp
[BluQubeRequester]
public static class Program
{
    // Client setup here
}
```

**`[BluQubeResponder]`**

Marks a server-side `Program` class to trigger generation of ASP.NET endpoints for all handlers and query processors in that assembly.

```csharp
[BluQubeResponder]
public static class Program
{
    // Server setup here
}
```

The source generators scan the marked assembly (and all referenced assemblies) to find:
- Handlers that inherit from `CommandHandler<T>`
- Processors that inherit from `GenericQueryProcessor<TQuery, TResult>`
- Commands/Queries decorated with `[BluQubeCommand]` / `[BluQubeQuery]`

---

## Project Structure

### Recommended: Three-Project Layout

```
Solution/
├── Shared/
│   └── Commands/
│       └── AddTodoCommand.cs        [BluQubeCommand]
│   └── Queries/
│       └── GetTodosQuery.cs         [BluQubeQuery]
│
├── Server/
│   ├── Program.cs                   [BluQubeResponder]
│   ├── Handlers/
│   │   └── AddTodoCommandHandler.cs
│   └── Processors/
│       └── GetTodosQueryProcessor.cs
│
└── Client/
    ├── Program.cs                   [BluQubeRequester]
    └── Pages/
        └── Todos.razor
```

**Benefits:**
- Separation of concerns: Shared commands live in one place.
- WASM client stays clean—no ASP.NET dependencies.
- Easy to share types across client and server.
- Handlers/processors only reference server-side infrastructure.

### Single-Project Layout

If you're using Blazor's built-in template (`.Web` project with both server and client), you can use a single project marked with both attributes:

```csharp
[BluQubeResponder]
[BluQubeRequester]
public static class Program
{
    // Both server and client setup in one file
}
```

**Caveats:**
- The server `Program.cs` must reference all command/query types *before* the generators run.
- Harder to keep client types clean (ASP.NET dependencies may leak).
- Not recommended for large apps.

---

## Server Setup

### 1. Add the BluQube Package

```bash
dotnet add package BluQube
```

### 2. Mark Program.cs with `[BluQubeResponder]`

```csharp
using BluQube.Attributes;
// ... other usings ...

[BluQubeResponder]
public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // ... your Razor/authentication/etc setup ...

        builder.Services.AddMediatR(cfg => 
            cfg.RegisterServicesFromAssemblyContaining<Program>());
        
        builder.Services.AddScoped<ICommandRunner, CommandRunner>();
        builder.Services.AddScoped<IQueryRunner, QueryRunner>();

        builder.Services.Configure<JsonOptions>(options =>
        {
            options.AddBluQubeJsonConverters();  // IMPORTANT
        });

        var app = builder.Build();

        // ... middleware setup ...

        app.AddBluQubeApi();  // Generates all endpoints
        app.Run();
    }
}
```

### 3. Implement Handlers and Processors on Server Only

**Example Handler:**
```csharp
public class AddTodoCommandHandler(
    IEnumerable<IValidator<AddTodoCommand>> validators,
    ILogger<AddTodoCommandHandler> logger,
    ITodoService todoService)
    : CommandHandler<AddTodoCommand>(validators, logger)
{
    protected override async Task<CommandResult> HandleInternal(
        AddTodoCommand request, CancellationToken cancellationToken)
    {
        await todoService.AddAsync(request.Title, request.Description, cancellationToken);
        return CommandResult.Success();
    }
}
```

**Example Query Processor:**
```csharp
public class GetTodosQueryProcessor(ITodoService todoService)
    : GenericQueryProcessor<GetTodosQuery, GetTodosResult>
{
    public async Task<QueryResult<GetTodosResult>> Handle(
        GetTodosQuery request, CancellationToken cancellationToken)
    {
        var items = await todoService.GetAllAsync(cancellationToken);
        return QueryResult<GetTodosResult>.Succeeded(
            new GetTodosResult(items));
    }
}
```

### 4. Ensure the Client Assembly is Available

If you have a separate client project, reference it from the server project so the generator can find commands and queries:

```xml
<!-- Server.csproj -->
<ItemGroup>
    <ProjectReference Include="..\Client\Client.csproj" />
</ItemGroup>
```

---

## Client (WASM) Setup

### 1. Add the BluQube Package

```bash
dotnet add package BluQube
```

### 2. Mark Program.cs with `[BluQubeRequester]`

```csharp
using BluQube.Attributes;
using BluQube.Commands;
using BluQube.Queries;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

[BluQubeRequester]
public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);

        // Register MediatR for command/query dispatch
        builder.Services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssemblies(typeof(Program).Assembly));

        // Register command and query runners
        builder.Services.AddScoped<ICommandRunner, CommandRunner>();
        builder.Services.AddScoped<IQueryRunner, QueryRunner>();

        // Configure HttpClient for the server
        builder.Services.AddHttpClient(
            "bluqube",
            client =>
            {
                client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
            });

        // Register generated requesters (handles HTTP serialization)
        builder.Services.AddBluQubeRequesters();

        // Register authentication/authorization
        builder.Services.AddAuthorizationCore();
        builder.Services.AddCascadingAuthenticationState();

        await builder.Build().RunAsync();
    }
}
```

### 3. Use Commands and Queries in Components

```csharp
@page "/todos"
@inject ICommandRunner CommandRunner
@inject IQueryRunner QueryRunner

<h3>My Todos</h3>

@if (Todos != null)
{
    <ul>
        @foreach (var todo in Todos)
        {
            <li>@todo.Title</li>
        }
    </ul>
}

<button @onclick="OnAddTodo">Add Todo</button>

@code {
    private GetTodosResult? Todos;

    protected override async Task OnInitializedAsync()
    {
        var result = await QueryRunner.Send(new GetTodosQuery());
        if (result.IsSucceeded)
        {
            Todos = result.Data;
        }
    }

    private async Task OnAddTodo()
    {
        var result = await CommandRunner.Send(
            new AddTodoCommand("New Todo", "Description"));
        
        if (result.IsSucceeded)
        {
            // Refresh the list
            var refreshResult = await QueryRunner.Send(new GetTodosQuery());
            if (refreshResult.IsSucceeded)
            {
                Todos = refreshResult.Data;
            }
        }
        else if (result.Status == CommandResultStatus.Invalid)
        {
            // Display validation errors
            Console.WriteLine("Validation failed:");
            foreach (var failure in result.ValidationResult.Failures)
            {
                Console.WriteLine($"  {failure.PropertyName}: {failure.ErrorMessage}");
            }
        }
        else if (result.Status == CommandResultStatus.Unauthorized)
        {
            Console.WriteLine("Not authorized to perform this action.");
        }
    }
}
```

---

## What NOT to Reference on Client

The WASM client must **never** reference ASP.NET-specific packages or libraries:

❌ **Do NOT reference:**
- `Microsoft.AspNetCore.*` (except `Microsoft.AspNetCore.Components.WebAssembly`)
- `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
- Entity Framework Core
- Database packages (SQL Server, PostgreSQL drivers)
- Any server-side middleware or filters
- Handler implementations

✅ **DO reference:**
- Commands and queries (the attribute-decorated records)
- `BluQube` package
- `MediatR`
- `FluentValidation` (if you want to validate on client too)

### Why This Matters

1. **WASM Trimming:** ASP.NET packages don't trim well for WASM; they bloat the download size.
2. **Compatibility:** ASP.NET packages assume a server runtime; WASM can't run them.
3. **Clean Separation:** Handlers run on the server; requesters run on the client. Mixing them confuses the boundary.

### The Shim Pattern

If you define a command with ASP.NET attributes (like `[FromRoute]`), the **server generator creates internal shim records** with the binding attributes. Your WASM client types stay clean and attribute-free.

```csharp
// Client sees this (clean)
[BluQubeCommand(Path = "commands/todo/{id}")]
public record DeleteTodoCommand(Guid Id) : ICommand;

// Server generator creates this internally (for binding)
internal record DeleteTodoCommandShim
{
    [FromRoute] public Guid Id { get; init; }
}
```

---

## URL Binding Across the Boundary

BluQube automatically binds command/query properties to URL path parameters and query strings.

### Path Parameters

Extract properties from the path template:

```csharp
[BluQubeCommand(Path = "commands/todo/{id}")]
public record DeleteTodoCommand(Guid Id) : ICommand;
```

The client-side generated requester produces:
```
DELETE /commands/todo/{id-value-escaped}
```

### Mixed Route + Body

Split properties between the path and request body:

```csharp
[BluQubeCommand(Path = "commands/todo/{id}/title")]
public record UpdateTodoTitleCommand(Guid Id, string NewTitle) : ICommand;
```

The client generates:
```
POST /commands/todo/{id-value-escaped}/title
Content-Type: application/json

{"newTitle":"New Title"}
```

`Id` is extracted from the path; `NewTitle` comes from the body.

### GET Queries with Query Strings

Mark queries as GET to enable query string binding:

```csharp
[BluQubeQuery(Path = "queries/todos", Method = "GET")]
public record GetTodosQuery(string? Filter = null, int PageSize = 10) : IQuery<GetTodosResult>;
```

The client generates:
```
GET /queries/todos?Filter=active&PageSize=10
```

Unmatched properties become query string parameters.

### POST Queries (Default)

Without `Method = "GET"`, queries POST:

```csharp
[BluQubeQuery(Path = "queries/todos")]
public record GetTodosQuery(string? Filter = null) : IQuery<GetTodosResult>;
```

The client generates:
```
POST /queries/todos
Content-Type: application/json

{"filter":"active"}
```

---

## Deployment Checklist

### Before Deploying

- [ ] **Clean build:** Run `dotnet clean && dotnet build` to ensure generators run.
- [ ] **Verify generators ran:** Check `obj/Debug/net10.0/*_Requesting.g.cs` and `*_Responding.g.cs` exist and contain your types.
- [ ] **Test locally:** Run both Server and Client projects locally; test a command/query round-trip.
- [ ] **Check dependencies:** Verify Client project does NOT reference ASP.NET packages.
- [ ] **CORS configured:** If client and server are on different domains, enable CORS on the server.
- [ ] **HTTPS configured:** WASM to HTTP is blocked in most browsers; ensure both use HTTPS in production.
- [ ] **Base URL correct:** Client `builder.HostEnvironment.BaseAddress` must match server URL.

### Server Deployment

1. Publish the server project:
   ```bash
   dotnet publish -c Release
   ```
2. Deploy to your hosting environment (Azure, AWS, Docker, etc.).
3. Ensure `app.AddBluQubeApi()` runs before `app.Run()`.

### Client Deployment

1. Publish the client project:
   ```bash
   dotnet publish -c Release
   ```
2. The `wwwroot` folder is deployed as static assets.
3. Configure the server to serve the client's `index.html` for client-side routing (SPA fallback).
4. Server `Program.cs` should call `app.MapFallbackToFile("index.html")` after adding static assets.

Example:
```csharp
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Client.Program).Assembly);

app.AddBluQubeApi();
app.MapFallbackToFile("index.html");  // SPA fallback for client routes
app.Run();
```

---

## Common Pitfalls

### Pitfall 1: CORS Not Configured

**Symptom:** Client makes a request; browser console shows `CORS policy: No 'Access-Control-Allow-Origin' header`.

**Cause:** Server is blocking cross-origin requests from the client.

**Fix:**
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClient", builder =>
    {
        builder
            .WithOrigins("https://yourdomain.com")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// ...

app.UseCors("AllowClient");
```

### Pitfall 2: Base URL Mismatch

**Symptom:** Client makes requests to `http://localhost:5000/...` but server is listening on `http://localhost:5001/...`.

**Cause:** `builder.HostEnvironment.BaseAddress` doesn't match server URL.

**Fix:** In the hosting configuration, ensure the client app is served from the same origin as the server, or explicitly set the base address:
```csharp
builder.Services.AddHttpClient(
    "bluqube",
    client =>
    {
        client.BaseAddress = new Uri("https://api.yourdomain.com/");
    });
```

### Pitfall 3: Missing JSON Converters on Server

**Symptom:** Client receives `500 Internal Server Error` when accessing a command result.

**Cause:** `options.AddBluQubeJsonConverters()` is missing from server `Program.cs`.

**Fix:**
```csharp
builder.Services.Configure<JsonOptions>(options =>
{
    options.AddBluQubeJsonConverters();
});
```

### Pitfall 4: Missing Responders on Server

**Symptom:** Client makes a request; server responds with `404 Not Found`.

**Cause:** `app.AddBluQubeApi()` is missing or called before handlers are registered.

**Fix:**
```csharp
// Register handlers and query processors first
builder.Services.AddMediatR(cfg => 
    cfg.RegisterServicesFromAssemblyContaining<Program>());

// Then add the BluQube endpoints
app.AddBluQubeApi();
```

### Pitfall 5: HTTPS in WASM

**Symptom:** In production, WASM client can't reach the server; works locally over HTTP.

**Cause:** Browsers block WASM from making HTTP requests to `http://` origins; HTTPS is required.

**Fix:** Ensure both client and server use HTTPS in production. Configure your hosting to redirect HTTP to HTTPS.

### Pitfall 6: Client References ASP.NET Packages

**Symptom:** Client project fails to build for WASM with trimming warnings or type not found errors.

**Cause:** Client project references `Microsoft.AspNetCore.*` or database packages.

**Fix:** Move those dependencies to the server project. Only include command/query definitions in the client.

### Pitfall 7: Incremental Generator Cache Issue

**Symptom:** After changing a `[BluQubeCommand]` attribute path, the old endpoint is still used.

**Cause:** Roslyn's incremental generator caches based on attribute signature; changes to attribute properties don't trigger regeneration.

**Fix:** Run `dotnet clean` before `dotnet build`:
```bash
dotnet clean && dotnet build
```

---

## Complete Working Example

### Step 1: Define a Shared Command and Query

**Shared/TodoCommands.cs:**
```csharp
using BluQube.Attributes;
using BluQube.Commands;
using BluQube.Queries;

namespace Shared;

[BluQubeCommand(Path = "commands/add-todo")]
public record AddTodoCommand(string Title, string Description) : ICommand;

[BluQubeQuery(Path = "queries/todos")]
public record GetTodosQuery : IQuery<GetTodosResult>;

public record GetTodosResult(List<string> Items) : IQueryResult;
```

### Step 2: Implement Server Handlers

**Server/Handlers/AddTodoCommandHandler.cs:**
```csharp
using BluQube.Commands;
using MediatR;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Server.Handlers;

public class AddTodoCommandHandler(
    IEnumerable<IValidator<AddTodoCommand>> validators,
    ILogger<AddTodoCommandHandler> logger)
    : CommandHandler<AddTodoCommand>(validators, logger)
{
    private static readonly List<string> Todos = new();

    protected override async Task<CommandResult> HandleInternal(
        AddTodoCommand request, CancellationToken cancellationToken)
    {
        Todos.Add($"{request.Title}: {request.Description}");
        logger.LogInformation("Todo added: {Title}", request.Title);
        return CommandResult.Success();
    }
}

public class GetTodosQueryProcessor : GenericQueryProcessor<GetTodosQuery, GetTodosResult>
{
    private static readonly List<string> Todos = new();

    public async Task<QueryResult<GetTodosResult>> Handle(
        GetTodosQuery request, CancellationToken cancellationToken)
    {
        return QueryResult<GetTodosResult>.Succeeded(
            new GetTodosResult(Todos));
    }
}
```

**Server/Program.cs:**
```csharp
using BluQube.Attributes;
using BluQube.Commands;
using BluQube.Queries;
using Microsoft.AspNetCore.Http.Json;

[BluQubeResponder]
public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<Program>());
        builder.Services.AddScoped<ICommandRunner, CommandRunner>();
        builder.Services.AddScoped<IQueryRunner, QueryRunner>();

        builder.Services.Configure<JsonOptions>(options =>
        {
            options.AddBluQubeJsonConverters();
        });

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.MapStaticAssets();

        app.AddBluQubeApi();
        app.Run();
    }
}
```

### Step 3: Setup Client (WASM)

**Client/Program.cs:**
```csharp
using BluQube.Attributes;
using BluQube.Commands;
using BluQube.Queries;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

[BluQubeRequester]
public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);

        builder.Services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssemblies(typeof(Program).Assembly));
        builder.Services.AddScoped<ICommandRunner, CommandRunner>();
        builder.Services.AddScoped<IQueryRunner, QueryRunner>();

        builder.Services.AddHttpClient(
            "bluqube",
            client => client.BaseAddress = new Uri(
                builder.HostEnvironment.BaseAddress));
        builder.Services.AddBluQubeRequesters();

        await builder.Build().RunAsync();
    }
}
```

**Client/Pages/Todos.razor:**
```razor
@page "/todos"
@inject ICommandRunner CommandRunner
@inject IQueryRunner QueryRunner

<h3>Todos</h3>

@if (Todos != null)
{
    <ul>
        @foreach (var todo in Todos.Items)
        {
            <li>@todo</li>
        }
    </ul>
}

<div>
    <input @bind="NewTitle" placeholder="Title" />
    <input @bind="NewDescription" placeholder="Description" />
    <button @onclick="OnAddTodo">Add</button>
</div>

@code {
    private GetTodosResult? Todos;
    private string NewTitle = "";
    private string NewDescription = "";

    protected override async Task OnInitializedAsync()
    {
        await RefreshTodos();
    }

    private async Task OnAddTodo()
    {
        if (string.IsNullOrEmpty(NewTitle))
            return;

        var result = await CommandRunner.Send(
            new AddTodoCommand(NewTitle, NewDescription));

        if (result.IsSucceeded)
        {
            NewTitle = "";
            NewDescription = "";
            await RefreshTodos();
        }
    }

    private async Task RefreshTodos()
    {
        var result = await QueryRunner.Send(new GetTodosQuery());
        if (result.IsSucceeded)
            Todos = result.Data;
    }
}
```

### Step 4: Test

1. **Start the server:**
   ```bash
   dotnet run --project Server/
   ```

2. **Start the client (in a new terminal):**
   ```bash
   dotnet run --project Client/
   ```

3. **Navigate to the client app** (typically `https://localhost:7000`), and test adding and viewing todos.

The client makes HTTP requests to the server; the server deserializes them, executes handlers, and returns results—all automatically wired by BluQube's generators.

---

## Summary

BluQube WASM deployment is straightforward:

1. **Define** commands and queries once with attributes.
2. **Implement** handlers on the server; generators create the endpoints.
3. **Setup** the client; generators create the HTTP requesters.
4. **Deploy** server and client to their respective hosts.
5. **Monitor** base URL, CORS, HTTPS, and generator caches.

The beauty is in the boundary: your WASM client stays lean, your server handlers stay focused, and the plumbing is automatic.
