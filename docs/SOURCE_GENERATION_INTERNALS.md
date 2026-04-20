# Source Generation Internals

## Overview

BluQube uses **incremental source generation** to eliminate boilerplate and keep client/server code synchronized. When you decorate a record with `[BluQubeCommand]` or `[BluQubeQuery]`, two independent generators spring into action:

1. **Client-side generator** (`Requesting.cs`) — Generates HTTP requesters that serialize commands/queries into HTTP requests (POST/GET with URL binding).
2. **Server-side generator** (`Responding.cs`) — Generates HTTP endpoints and result converters that deserialize requests, invoke handlers, and return results.

This "write once, generate twice" pattern is what enables **"write once, run on Server or WASM"** development. A command defined in a shared client project automatically gets an HTTP requester for WASM, and a server project automatically gets an endpoint to handle it.

---

## What Gets Generated

### Before: Your Code

You write four things:

**Client-side (shared between Server and WASM):**
```csharp
[BluQubeCommand(Path = "commands/todo/add")]
public record AddTodoCommand(string Title) : ICommand<AddTodoCommandResult>;
```

**Server-side:**
```csharp
public class AddTodoCommandHandler(
    ITodoService todoService, 
    IEnumerable<IValidator<AddTodoCommand>> validators, 
    ILogger<AddTodoCommandHandler> logger)
    : CommandHandler<AddTodoCommand, AddTodoCommandResult>(validators, logger)
{
    protected override Task<CommandResult<AddTodoCommandResult>> HandleInternal(
        AddTodoCommand request, CancellationToken cancellationToken)
    {
        var todo = todoService.AddTodo(request.Title);
        return Task.FromResult(CommandResult<AddTodoCommandResult>.Succeeded(
            new AddTodoCommandResult(todo.Id)));
    }
}
```

### After: Generated Code

**Client-side requester** (generated in `obj/` → compiled into assembly):
```csharp
// GENERATED: Generic{CommandName}Handler
// (conceptual; exact details vary by command type)
internal class GenericAddTodoCommandHandler(
    IHttpClientFactory httpClientFactory,
    CommandResultConverter<AddTodoCommandResult> jsonConverter,
    ILogger<GenericCommandHandler<AddTodoCommand, AddTodoCommandResult>> logger)
        : GenericCommandHandler<AddTodoCommand, AddTodoCommandResult>(
            httpClientFactory, jsonConverter, logger)
{
    protected override string Path => "commands/todo/add";
}
```

**Server-side endpoint** (generated in `obj/` → compiled into assembly):
```csharp
// GENERATED: EndpointRouteBuilderExtensions.AddBluQubeApi()
internal static class EndpointRouteBuilderExtensions
{
    internal static IEndpointRouteBuilder AddBluQubeApi(this IEndpointRouteBuilder builder)
    {
        builder.MapPost("commands/todo/add", async (
            ICommandRunner commandRunner, 
            AddTodoCommand command) =>
        {
            var data = await commandRunner.Send(command);
            return Results.Json(data);
        });
        
        return builder;
    }
}
```

**JSON converters** (generated to handle polymorphic result types):
```csharp
// GENERATED for AddTodoCommandResult
internal class AddTodoCommandResultConverter : CommandResultConverter<AddTodoCommandResult>
{
    // Handles JSON serialization/deserialization of CommandResult<AddTodoCommandResult>
}
```

---

## The Generator Pipeline

### How Generation Happens

```
┌─────────────────────────────────────────────────────────────┐
│ You annotate a record with [BluQubeCommand/Query]           │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ↓
┌─────────────────────────────────────────────────────────────┐
│ Roslyn IIncrementalGenerator scans syntax tree              │
│ (predicate: match records with BluQube attributes)          │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ├─→ Input Definition Processors
                       │   (extract metadata from attributes)
                       │
                       ├─→ Semantic Model Analysis
                       │   (resolve types, namespaces, properties)
                       │
                       └─→ Output Definition Processors
                           (generate C# source code)
                                │
                                ├─ Client: Requesting.cs
                                │  → GenericQueryProcessor<T, TResult>
                                │  → HttpClient POST/GET wiring
                                │
                                └─ Server: Responding.cs
                                   → MapPost/MapGet endpoints
                                   → Shim records for URL/query binding
                                   → JSON converter registration
                                   → Service collection extensions
```

### Incremental Caching

Both generators use `IIncrementalGenerator`, which means:
- **Cached:** The generator caches results by input attributes
- **Efficient:** Only processes changed types on rebuild
- **Gotcha:** Attribute changes don't always trigger regeneration; see [Debugging](#debugging-generation-failures)

---

## Client-Side Generator: `Requesting.cs`

### What It Does

Generates **one class per Query/Command** that knows how to serialize the command/query and send it via HTTP.

### Input Processors

| Processor | Input | Output |
|-----------|-------|--------|
| `QueryInputDefinitionProcessor` | `[BluQubeQuery]` decorated record | Query name, path, method, namespace |
| `CommandInputDefinitionProcessor` | `[BluQubeCommand]` decorated record | Command name, path, namespace |
| `CommandWithResultInputDefinitionProcessor` | `[BluQubeCommand]` with result type | Command name, result type, path, namespace |
| `RequesterInputDefinitionProcessor` | `[BluQubeRequester]` marker on class | Signal: "this project has requesters to generate" |

### Output: GenericQueryProcessorOutputDefinitionProcessor

For a query like:
```csharp
[BluQubeQuery(Path = "queries/todo/get-all")]
public record GetAllToDoItemsQuery : IQuery<GetAllToDoItemsQueryAnswer>;
```

**Generated output:**
```csharp
// obj/Debug/net10.0/.../BluQube.Samples.Blazor.Client_Requesting.g.cs
namespace BluQube.Samples.Blazor.Client.Infrastructure.Queries;

internal class GenericGetAllToDoItemsQueryProcessor(
    IHttpClientFactory httpClientFactory,
    QueryResultConverter<GetAllToDoItemsQueryAnswer> jsonConverter,
    ILogger<GenericQueryProcessor<GetAllToDoItemsQuery, GetAllToDoItemsQueryAnswer>> logger)
        : GenericQueryProcessor<GetAllToDoItemsQuery, GetAllToDoItemsQueryAnswer>(
            httpClientFactory, jsonConverter, logger)
{
    protected override string Path => "queries/todo/get-all";
    protected override string HttpMethod => "POST";
}
```

### Path Building with URL Binding

For queries with route parameters:
```csharp
[BluQubeQuery(Path = "queries/todo/get/{id}", Method = "GET")]
public record GetTodoQuery(int Id) : IQuery<TodoDetailResult>;
```

**Generated:**
```csharp
protected override string Path => "queries/todo/get/{id}";
protected override string HttpMethod => "GET";

protected override string BuildPath(GetTodoQuery request)
{
    var queryString = string.Join("&", new[] { }.Where(x => !string.IsNullOrEmpty(x)));
    var path = $"queries/todo/get/{System.Uri.EscapeDataString(request.Id.ToString())}";
    return string.IsNullOrEmpty(queryString) ? path : $"{path}?{queryString}";
}
```

**Key detail:** `Uri.EscapeDataString()` ensures AOT-safe URL encoding (no reflection).

### Service Collection Registration

Generated in `ServiceCollectionExtensionsOutputDefinitionProcessor`:
```csharp
// obj/Debug/net10.0/.../BluQube.Samples.Blazor.Client_Requesting.g.cs (extension method)
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBluQubeRequesters(this IServiceCollection services)
    {
        services.AddTransient<QueryResultConverter<GetAllToDoItemsQueryAnswer>>(
            _ => new GetAllToDoItemsQueryAnswerConverter());
        services.AddTransient<CommandResultConverter<AddTodoCommandResult>>(
            _ => new AddTodoCommandResultConverter());
        // ... all other converters
        return services;
    }
}
```

---

## Server-Side Generator: `Responding.cs`

### What It Does

Scans the current **server assembly and all referenced assemblies** for `[BluQubeResponder]` marker, then generates:
1. **Endpoint registration** — MapPost/MapGet calls wired to handlers
2. **Shim records** — For requests with URL binding (route/query params)
3. **JSON converter registration** — Registers converters for all result types

### Assembly Scanning

The responder generator runs in two phases:

**Phase 1: Collect input definitions**
```csharp
// Find all classes with these attributes in current + referenced assemblies
- [BluQubeResponder] (marker class)
- IQueryProcessor<TQuery, TResult> implementations
- CommandHandler<T> / CommandHandler<T, TResult> implementations
```

**Phase 2: Register outputs**
```csharp
// For each handler found, generate an endpoint + shim + converter
```

### Shim Records

**Problem:** User's command/query record lives in a shared client project. It can't have ASP.NET Mvc attributes like `[FromRoute]` or `[FromQuery]` (those are server-only concerns).

**Solution:** Generate internal "shim" records with binding attributes.

**Example:**

User writes:
```csharp
// Client project (shared)
[BluQubeCommand(Path = "commands/todo/update/{id}")]
public record UpdateTodoCommand(int Id, string Title) : ICommand;
```

Generator creates:
```csharp
// Server project (generated)
namespace YourApp;

internal record UpdateTodoCommandBody(
    [property: Microsoft.AspNetCore.Mvc.FromRoute] int id,
    [property: Microsoft.AspNetCore.Mvc.FromBody] string title);

// Then the endpoint uses the shim:
builder.MapPost("commands/todo/update/{id}", async (
    ICommandRunner commandRunner,
    [FromRoute] int id,
    UpdateTodoCommandBody body) =>
{
    var command = new UpdateTodoCommand(id, body.title);
    return await commandRunner.Send(command);
});
```

**Why shims exist:**
- Keep user types clean (no ASP.NET pollution)
- Enable WASM-safe code (WASM client projects can't reference `[FromRoute]`)
- Separation of concerns (HTTP binding is server impl detail)

### Endpoint Generation

Generated in `EndpointRouteBuilderExtensionsOutputDefinitionProcessor`:

**For query with no route params:**
```csharp
endpointRouteBuilder.MapPost("queries/todo/get-all", async (IQueryRunner queryRunner, GetAllToDoItemsQuery query) =>
{
    var data = await queryRunner.Send(query);
    return Results.Json(data);
});
```

**For query with route and query-string params:**
```csharp
// Generated shim:
internal record GetTodoQueryParams(
    [property: Microsoft.AspNetCore.Mvc.FromQuery] string? filter,
    [property: Microsoft.AspNetCore.Mvc.FromQuery] int pageSize);

// Endpoint:
endpointRouteBuilder.MapGet("queries/todo/{id}", async (
    IQueryRunner queryRunner,
    [FromRoute] int id,
    [AsParameters] GetTodoQueryParams queryParams) =>
{
    var query = new GetTodoQuery(id, queryParams.filter, queryParams.pageSize);
    var data = await queryRunner.Send(query);
    return Results.Json(data);
});
```

### JSON Converter Registration

Generated in `JsonOptionsExtensionsOutputDefinitionProcessor`:

```csharp
// obj/Debug/net10.0/.../BluQube.Samples.Blazor_Responding.g.cs
public static class JsonOptionsExtensions
{
    public static JsonSerializerOptions AddBluQubeJsonConverters(this JsonSerializerOptions options)
    {
        options.AddConverter(new GetAllToDoItemsQueryAnswerConverter());
        options.AddConverter(new AddTodoCommandResultConverter());
        // ... all result converters
        return options;
    }
}
```

---

## How to Inspect Generated Code

### Location

Generated files live in the **obj folder**:

```
bin/
└── Debug/
    └── net10.0/
        └── <ProjectName>.g.cs      ← Generated files here
```

Or more specifically, in intermediate build output:

```
obj/
└── Debug/
    └── net10.0/
        └── [ProjectName]_Requesting.g.cs    (client-side)
        └── [ProjectName]_Responding.g.cs    (server-side)
```

### Finding Generated Files in IDE

**Visual Studio:**
1. Build the project
2. Open **Solution Explorer** → Project → **Show All Files**
3. Navigate to `obj/Debug/net10.0/` → Look for `.g.cs` files

**Command line:**
```bash
# Find all generated files
find . -name "*.g.cs" -path "*/obj/*"
```

### Reading Generated Output

Generated files are **read-only** and **auto-regenerated** on rebuild. To understand them:

1. **Don't edit them** — Changes are lost on next build
2. **Search by class name** — e.g., search for `GenericAddTodoCommandHandler`
3. **Compare with input** — Check the attribute path/method to understand output
4. **Use Roslyn analyzer** — Some IDEs show generated code via syntax tree visualizers

---

## Debugging Generation Failures

### Common Symptoms

| Symptom | Cause | Fix |
|---------|-------|-----|
| Handler not invoked, endpoint 404 | Attribute not recognized or missing `[BluQubeResponder]` | Run `dotnet clean && dotnet build` |
| Generated code not reflecting attribute changes | Incremental caching didn't invalidate | Clean build required |
| JSON serialization fails at runtime | Converter not registered | Ensure `AddBluQubeJsonConverters()` called in Program.cs |
| "Cannot find type" errors in generated code | Namespace mismatch or type not found | Verify type exists in correct namespace |
| Endpoint receives request but hangs | Shim record binding failed | Check `[FromRoute]` / `[FromQuery]` match attribute names |

### Debug Steps

#### 1. Clean Build (Always First)

```bash
dotnet clean
dotnet build
```

**Why:** Incremental generators cache by attribute signature. If you change an attribute property, the cache may not invalidate.

#### 2. Check Build Output

```bash
dotnet build --verbosity diagnostic 2>&1 | grep -i "generator\|error"
```

Look for:
- `Generator 'Requesting' generated source`
- `Generator 'Responding' generated source`
- Error messages about attribute parsing

#### 3. Inspect Generated Files

1. Build succeeded → Open generated `.g.cs` file in `obj/Debug/net10.0/`
2. Search for your type name (e.g., `GenericAddTodoCommandHandler`)
3. Compare generated code with expected structure

#### 4. Verify Input Definition Processors

Each processor has a `CanProcess()` method that checks if a syntax node matches:

**For Requesting.cs:**
```csharp
public bool CanProcess(SyntaxNode node) =>
    node is RecordDeclarationSyntax record &&
    record.AttributeLists.Any(al => al.Attributes.Any(a =>
        a.Name.ToString().Contains("BluQubeQuery")));
```

**If `CanProcess` returns false:**
- Attribute name is wrong (typo?)
- Attribute not imported in correct namespace
- Record syntax not recognized (record keyword used correctly?)

#### 5. Test With Minimal Example

Create a minimal command/query to isolate the issue:

```csharp
[BluQubeCommand(Path = "test/debug")]
public record DebugCommand : ICommand;
```

If this generates correctly:
- Generator pipeline works
- Problem is in your specific type (e.g., invalid property type)

If this doesn't generate:
- Problem is in generator registration or Roslyn integration

#### 6. Known Issues & Workarounds

**Issue: Incremental generator cache stale after attribute change**
- **Symptom:** Changed `Path = "..."` but endpoint still uses old path
- **Cause:** Roslyn incremental caching based on attribute AST
- **Fix:** Touch a source file, or edit attribute slightly and revert, triggering regeneration

**Issue: Compilation symbols not found in generated code**
- **Symptom:** `The name 'SomeType' does not exist in the current context` in generated endpoint
- **Cause:** Type defined in referenced assembly not scanned properly
- **Fix:** Ensure type is `public` and in referenced assembly

**Issue: GET query test failures (Roslyn-specific)**
- **Symptom:** Generated GET endpoints work in app but fail in xUnit tests
- **Cause:** Roslyn syntax tree in test context differs from compilation
- **Workaround:** See [Testing](#testing-generated-code) section

### Enable Verbose Logging

In Program.cs (server), enable Roslyn diagnostics:

```csharp
builder.Services.AddLogging(opts =>
{
    opts.AddConsole();
    opts.SetMinimumLevel(LogLevel.Debug);
});

// Then in generated endpoint, the logger will show request details
```

---

## Known Limitations

### 1. GET Query Parameter Isolation

**Issue:** In xUnit tests, GET query parameters from `BuildPath()` may not parse correctly due to Roslyn syntax tree differences between compile-time generation and test-time reflection.

**Manifestation:** A query with GET method and query parameters works in the app but fails in unit tests trying to inspect generated type.

**Workaround:** Use integration tests (TestServer) instead of unit tests for GET query validation, or extract parameter binding to a separate utility method.

### 2. EqualityContract Skipping

**Issue (historical):** Early versions skipped the `EqualityContract` property on records, causing deserialization failures.

**Status:** ✅ **Fixed in current version** — The responder generator explicitly skips `EqualityContract`:
```csharp
if (member.IsImplicitlyDeclared || member.Name == "EqualityContract")
{
    continue;
}
```

### 3. Single-Project Scope

**Limitation:** If your commands/queries and handlers live in the **same project**, the generator cannot cross-reference them during incremental generation (chicken-and-egg problem with single-pass Roslyn).

**Workaround:** Separate client-side types (commands/queries) into a shared project, and handlers into a server project. This is the recommended architecture anyway.

### 4. Shim Record Naming Collisions

**Limitation:** If you have two commands with the same name but different namespaces, shim record names may collide.

**Manifestation:** Two endpoints trying to register `SomeCommandBody` shim.

**Workaround:** Use different command names or explicitly disambiguate in attribute paths.

---

## Anatomy of a Complete Example

### Input

```csharp
// Client project
[BluQubeCommand(Path = "commands/todo/update/{id}")]
public record UpdateTodoCommand(int Id, string NewTitle) 
    : ICommand<UpdateTodoCommandResult>;

public record UpdateTodoCommandResult(bool Success);
```

```csharp
// Server project
public class UpdateTodoCommandHandler(
    ITodoService todoService,
    IEnumerable<IValidator<UpdateTodoCommand>> validators,
    ILogger<UpdateTodoCommandHandler> logger)
    : CommandHandler<UpdateTodoCommand, UpdateTodoCommandResult>(validators, logger)
{
    protected override async Task<CommandResult<UpdateTodoCommandResult>> HandleInternal(
        UpdateTodoCommand request, CancellationToken cancellationToken)
    {
        var success = await todoService.UpdateAsync(request.Id, request.NewTitle, cancellationToken);
        return CommandResult<UpdateTodoCommandResult>.Succeeded(
            new UpdateTodoCommandResult(success));
    }
}
```

### Generated Client-Side (Requesting.cs)

```csharp
// obj/Debug/net10.0/.../Client_Requesting.g.cs
namespace YourApp.Client.Commands;

internal class GenericUpdateTodoCommandHandler(
    IHttpClientFactory httpClientFactory,
    CommandResultConverter<UpdateTodoCommandResult> jsonConverter,
    ILogger<GenericCommandHandler<UpdateTodoCommand, UpdateTodoCommandResult>> logger)
        : GenericCommandHandler<UpdateTodoCommand, UpdateTodoCommandResult>(
            httpClientFactory, jsonConverter, logger)
{
    protected override string Path => "commands/todo/update/{id}";

    protected override string BuildPath(UpdateTodoCommand request)
    {
        return $"commands/todo/update/{System.Uri.EscapeDataString(request.Id.ToString())}";
    }
}
```

### Generated Server-Side (Responding.cs)

```csharp
// obj/Debug/net10.0/.../Server_Responding.g.cs
namespace YourApp;

internal static class EndpointRouteBuilderExtensions
{
    internal static IEndpointRouteBuilder AddBluQubeApi(this IEndpointRouteBuilder builder)
    {
        // Shim for non-route parameters
        builder.MapPost("commands/todo/update/{id}", async (
            ICommandRunner commandRunner,
            [FromRoute] int id,
            UpdateTodoCommandBody body) =>
        {
            var command = new UpdateTodoCommand(id, body.NewTitle);
            var data = await commandRunner.Send(command);
            return Results.Json(data);
        });

        return builder;
    }

    internal record UpdateTodoCommandBody(string NewTitle);
}

// JSON converters for UpdateTodoCommandResult
internal class UpdateTodoCommandResultConverter : CommandResultConverter<UpdateTodoCommandResult>
{
    // Handles serialization of CommandResult<UpdateTodoCommandResult>
}
```

### DI Registration (Program.cs)

Client project:
```csharp
builder.Services.AddBluQubeRequesters();
```

Server project:
```csharp
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.Configure<JsonSerializerOptions>(opts => opts.AddBluQubeJsonConverters());
app.MapBluQubeApi();
```

### Runtime Flow

**Client (WASM/Server):**
1. Component calls `await commandRunner.Send(new UpdateTodoCommand(1, "Buy milk"))`
2. `GenericUpdateTodoCommandHandler` intercepts (injected as `GenericCommandHandler<,>`)
3. Builds path: `"commands/todo/update/1"`
4. Serializes body: `{ "newTitle": "Buy milk" }`
5. POSTs to server
6. Deserializes response with `UpdateTodoCommandResultConverter`
7. Returns `CommandResult<UpdateTodoCommandResult>.Succeeded(...)`

**Server:**
1. ASP.NET routes POST `commands/todo/update/1` to generated endpoint
2. Binds `id` from route, `newTitle` from body shim
3. Creates `UpdateTodoCommand(1, "Buy milk")`
4. Calls `commandRunner.Send(command)`
5. MediatR routes to `UpdateTodoCommandHandler.Handle()`
6. Handler validates, then calls `HandleInternal()`
7. `TodoService.UpdateAsync()` updates database
8. Returns `CommandResult<UpdateTodoCommandResult>.Succeeded(new UpdateTodoCommandResult(true))`
9. Endpoint serializes with JSON converter
10. Client receives and deserializes response

---

## Summary

- **Generators:** Two independent IIncrementalGenerator instances (Requesting, Responding)
- **Input:** Attributes on records (`[BluQubeCommand]`, `[BluQubeQuery]`) and markers on responder classes
- **Output:** HTTP requesters, endpoints, shim records, JSON converters, DI registration methods
- **Key design:** Shim records allow user types to stay clean while enabling full ASP.NET binding support
- **Debug:** Always clean build first; inspect `.g.cs` files in `obj/Debug/net10.0/`
- **Gotchas:** Incremental caching requires full rebuild for attribute changes; assembly scanning runs on both current + referenced assemblies
