# URL Binding Guide

## Overview

BluQube automatically binds command and query properties to RESTful URL patterns—path parameters, query strings, and request bodies are inferred from your path template. This enables REST-style endpoints like `/commands/todo/{id}/update` or `/queries/todo/{id}?filter=active` without manual parameter binding code.

---

## How It Works

When you define a command or query with `{paramName}` in the path template, BluQube's source generator:

1. **Extracts route parameters** using pattern matching on `{paramName}` syntax
2. **Matches properties** to path parameters (case-insensitive)
3. **Binds remaining properties** based on the HTTP method:
   - **Commands (POST):** route params → URL path, others → JSON body
   - **Queries (GET):** route params → URL path, others → query string
   - **Queries (POST, default):** route params → URL path, others → JSON body

Property names are matched to path parameter names case-insensitively. For example, `{TodoId}` in the path matches a record property named `TodoId`, `todoid`, or `todoId`.

---

## Path Parameters

Path parameters are declared using `{paramName}` in the `Path` property of your attribute. The generator automatically extracts these and binds matching record properties.

### Command with Single Path Parameter

```csharp
[BluQubeCommand(Path = "commands/todo/{id}")]
public record DeleteTodoCommand(Guid Id) : ICommand;
```

**Client usage:**
```csharp
var cmd = new DeleteTodoCommand(todoId: Guid.Parse("abc-123-def"));
// Generated code produces: POST /commands/todo/abc-123-def
// Request body: empty
```

**Server endpoint generated:**
```csharp
app.MapPost("commands/todo/{id}", async (ICommandRunner runner, [FromRoute] Guid id) => {
    var cmd = new DeleteTodoCommand(Id: id);
    var result = await runner.Send(cmd);
    return Results.Json(result);
});
```

### Command with Path Parameter and Body Fields

Mix route parameters with request body fields. Path params go in the URL; other properties serialize to JSON.

```csharp
[BluQubeCommand(Path = "commands/todo/{id}/update-title")]
public record UpdateTodoCommand(Guid Id, string Title) : ICommand;
```

**Client usage:**
```csharp
var cmd = new UpdateTodoCommand(Id: todoId, Title: "New Title");
// Generated code produces: POST /commands/todo/[todoId-escaped]/update-title
// Request body: {"Title":"New Title"}
```

**Server endpoint generated:**
```csharp
app.MapPost("commands/todo/{id}/update-title", async (ICommandRunner runner, [FromRoute] Guid id, UpdateTodoCommandBody body) => {
    var cmd = new UpdateTodoCommand(Id: id, Title: body.Title);
    var result = await runner.Send(cmd);
    return Results.Json(result);
});
```

### Query with Multiple Path Parameters

You can use multiple path parameters in a single route.

```csharp
[BluQubeQuery(Path = "queries/user/{userId}/todo/{todoId}")]
public record GetUserTodoQuery(Guid UserId, Guid TodoId) : IQuery<TodoDetailResult>;
```

**Client usage:**
```csharp
var query = new GetUserTodoQuery(UserId: userId, TodoId: todoId);
// Generated code produces: POST /queries/user/[userId-escaped]/todo/[todoId-escaped]
// Request body: empty
```

---

## GET Queries with Query String

By default, queries use POST. Set `Method = "GET"` to enable query string binding for non-route parameters.

```csharp
[BluQubeQuery(Path = "queries/todo/{id}", Method = "GET")]
public record GetTodoQuery(Guid Id, string? Filter = null) : IQuery<TodoResult>;
```

**Client usage:**
```csharp
var query = new GetTodoQuery(Id: todoId, Filter: "completed");
// Generated code produces: GET /queries/todo/[todoId-escaped]?Filter=completed
```

**Server endpoint generated:**
```csharp
app.MapGet("queries/todo/{id}", async (IQueryRunner runner, [FromRoute] Guid id, [AsParameters] GetTodoQueryParams queryParams) => {
    var query = new GetTodoQuery(Id: id, Filter: queryParams.Filter);
    var result = await runner.Send(query);
    return Results.Json(result);
});
```

### Optional Query Parameters

Query parameters are automatically optional—`null` values are omitted from the query string.

```csharp
[BluQubeQuery(Path = "queries/todo/search", Method = "GET")]
public record SearchTodosQuery(string? Title = null, bool? IsCompleted = null) : IQuery<SearchResult>;
```

**Client usage:**
```csharp
var query = new SearchTodosQuery(Title: "Shopping", IsCompleted: null);
// Generated code produces: GET /queries/todo/search?Title=Shopping
// (IsCompleted is omitted because it's null)
```

---

## POST Queries (Default)

If you omit `Method` or set `Method = "POST"`, queries use POST with remaining properties in the request body.

```csharp
[BluQubeQuery(Path = "queries/todo/{id}")]
public record GetTodoDetailQuery(Guid Id) : IQuery<TodoDetailResult>;
```

**Client usage:**
```csharp
var query = new GetTodoDetailQuery(Id: todoId);
// Generated code produces: POST /queries/todo/[todoId-escaped]
// Request body: empty (since all parameters are bound to route)
```

### POST Query with Body Parameters

Combine route parameters with body properties for complex queries.

```csharp
[BluQubeQuery(Path = "queries/user/{userId}/todos")]
public record GetUserTodosQuery(Guid UserId, string? SortBy = null, int PageSize = 10) : IQuery<TodosListResult>;
```

**Client usage:**
```csharp
var query = new GetUserTodosQuery(UserId: userId, SortBy: "due-date", PageSize: 20);
// Generated code produces: POST /queries/user/[userId-escaped]/todos
// Request body: {"SortBy":"due-date","PageSize":20}
```

---

## Case-Insensitive Property Matching

Path parameter names are matched to record properties case-insensitively. All of these work identically:

```csharp
// Path uses {todoId}
[BluQubeCommand(Path = "commands/todo/{todoId}")]
public record DeleteCommand1(Guid TodoId) : ICommand;  // ✓ Exact match

public record DeleteCommand2(Guid todoId) : ICommand;  // ✓ Case variation
public record DeleteCommand3(Guid TODOID) : ICommand;  // ✓ All caps
```

---

## URL Encoding

URLs are automatically encoded using `Uri.EscapeDataString()`, which is AOT-compatible and requires no reflection.

**Special characters are safely escaped:**

```csharp
[BluQubeCommand(Path = "commands/note/{id}")]
public record CreateNoteCommand(Guid Id, string Title) : ICommand;

var cmd = new CreateNoteCommand(Id: noteId, Title: "Meeting: Q2 Planning & Review");
// Generated code produces: POST /commands/note/[noteId-escaped]
// Request body: {"Title":"Meeting: Q2 Planning & Review"}
// Note: Title (in body) is NOT URL-escaped; only path params are
```

The generated `BuildPath()` method handles escaping automatically, so you don't need to think about it.

---

## WASM Compatibility

BluQube's URL binding uses a shim record pattern on the server that keeps your client types clean and fully compatible with WebAssembly.

**Your client command:**
```csharp
[BluQubeCommand(Path = "commands/todo/{id}/update")]
public record UpdateTodoCommand(Guid Id, string Title) : ICommand;
```

**Server-side shim (generated):**
```csharp
// Generated internal shim record
internal record UpdateTodoCommandBody(string Title);

// Generated endpoint
app.MapPost("commands/todo/{id}/update", 
    async (ICommandRunner runner, [FromRoute] Guid id, UpdateTodoCommandBody body) => {
        var cmd = new UpdateTodoCommand(id, body.Title);
        // ...
    });
```

Your client record stays untouched—no ASP.NET attributes, no server-specific dependencies. This means:
- ✓ Same code works in WASM and Server Blazor
- ✓ No `[FromRoute]` pollution on shared types
- ✓ Fully serializable to JSON for WASM interop

---

## Examples

### Example 1: Delete with ID in Route

Simple delete endpoint with an ID in the path.

```csharp
[BluQubeCommand(Path = "commands/todo/{id}")]
public record DeleteTodoCommand(Guid Id) : ICommand;

// Usage
var cmd = new DeleteTodoCommand(todoId);
var result = await commandRunner.Send(cmd);
```

### Example 2: Search GET Query

GET endpoint with query string filters.

```csharp
[BluQubeQuery(Path = "queries/todo/search", Method = "GET")]
public record SearchTodosQuery(
    string? Title = null,
    string? AssignedTo = null,
    bool? IsCompleted = null) : IQuery<SearchResult>;

// Usage
var query = new SearchTodosQuery(Title: "Deploy", AssignedTo: "alice");
var result = await queryRunner.Send(query);
// GET /queries/todo/search?Title=Deploy&AssignedTo=alice
```

### Example 3: Hierarchical Resource with Mixed Binding

Multiple path parameters plus body data.

```csharp
[BluQubeCommand(Path = "commands/project/{projectId}/todo/{todoId}/assign")]
public record AssignTodoCommand(Guid ProjectId, Guid TodoId, string AssignedTo) : ICommand;

// Usage
var cmd = new AssignTodoCommand(ProjectId: projId, TodoId: todoId, AssignedTo: "bob");
var result = await commandRunner.Send(cmd);
// POST /commands/project/[projId]/todo/[todoId]/assign
// Body: {"AssignedTo":"bob"}
```

### Example 4: GET Query with Path Param and Filters

Combine route binding with query parameters.

```csharp
[BluQubeQuery(Path = "queries/user/{userId}/todos", Method = "GET")]
public record GetUserTodosQuery(
    Guid UserId,
    string? SortBy = null,
    int Skip = 0,
    int Take = 10) : IQuery<TodosListResult>;

// Usage
var query = new GetUserTodosQuery(UserId: userId, SortBy: "due-date", Skip: 10, Take: 20);
var result = await queryRunner.Send(query);
// GET /queries/user/[userId]/todos?SortBy=due-date&Skip=10&Take=20
```

---

## Common Mistakes

### Mistake 1: Property Name Doesn't Match Path Parameter

The path parameter name won't auto-match a property with a different name. Property names must correspond to `{paramName}` in the path (case-insensitive).

**Wrong:**
```csharp
[BluQubeCommand(Path = "commands/todo/{id}")]
public record DeleteCommand(Guid TodoId) : ICommand;  // ✗ Property is TodoId, param is {id}
```

**Right:**
```csharp
[BluQubeCommand(Path = "commands/todo/{id}")]
public record DeleteCommand(Guid Id) : ICommand;  // ✓ Matches {id}
```

### Mistake 2: Forgetting `Method = "GET"` for Query String

Queries default to POST. If you want query string binding, you must explicitly set `Method = "GET"`.

**Wrong:**
```csharp
[BluQubeQuery(Path = "queries/search")]
public record SearchQuery(string? Term = null) : IQuery<SearchResult>;
// Generates: POST /queries/search with body {"Term":"..."}
```

**Right:**
```csharp
[BluQubeQuery(Path = "queries/search", Method = "GET")]
public record SearchQuery(string? Term = null) : IQuery<SearchResult>;
// Generates: GET /queries/search?Term=value
```

### Mistake 3: Not Using Path Parameters for Hierarchical Routes

If your business logic requires hierarchical routes (e.g., `/user/{userId}/todo/{todoId}`), make those properties part of your command/query record and use the path template.

**Wrong:**
```csharp
[BluQubeCommand(Path = "commands/todo/update")]
public record UpdateTodoCommand(Guid UserId, Guid TodoId, string Title) : ICommand;
// All three properties in body—loses REST semantics
```

**Right:**
```csharp
[BluQubeCommand(Path = "commands/user/{userId}/todo/{todoId}/update")]
public record UpdateTodoCommand(Guid UserId, Guid TodoId, string Title) : ICommand;
// UserId and TodoId from route, Title from body
```

---

## See Also

- [README](../README.md#url-binding) — Quick overview of URL binding
- [AUTHORIZATION_GUIDE.md](./AUTHORIZATION_GUIDE.md) — Protecting endpoints with `[Authorize]`
- [VALIDATION_GUIDE.md](./VALIDATION_GUIDE.md) — Validating commands and queries
