# URL Binding

BluQube infers path parameters, query strings, and bodies from the `Path` template and HTTP method.

## Commands

Commands use POST. Path parameters are taken from route placeholders; remaining properties go in the JSON body.

```csharp
[BluQubeCommand(Path = "commands/todo/{id}/title")]
public record UpdateTodoTitleCommand(Guid Id, string Title) : ICommand;
```

Generated behavior:

```text
POST /commands/todo/{escaped-id}/title
Body: { "Title": "..." }
```

The `{id}` placeholder matches the `Id` property case-insensitively.

## POST Queries

Queries POST by default.

```csharp
[BluQubeQuery(Path = "queries/user/{userId}/todos")]
public record GetUserTodosQuery(Guid UserId, int Take = 20) : IQuery<TodosResult>;
```

Generated behavior:

```text
POST /queries/user/{escaped-userId}/todos
Body: { "Take": 20 }
```

## GET Queries

Set `Method = "GET"` to put non-route properties in the query string.

```csharp
[BluQubeQuery(Path = "queries/todos", Method = "GET")]
public record SearchTodosQuery(string? Title = null, bool? IsCompleted = null)
    : IQuery<SearchTodosResult>;
```

Generated behavior:

```text
GET /queries/todos?Title=shopping&IsCompleted=false
```

Null optional query parameters are omitted.

## Mixed Route And Query String

```csharp
[BluQubeQuery(Path = "queries/user/{userId}/todos", Method = "GET")]
public record GetUserTodosQuery(Guid UserId, string? SortBy = null)
    : IQuery<TodosResult>;
```

Generated behavior:

```text
GET /queries/user/{escaped-userId}/todos?SortBy=due-date
```

## Rules And Mistakes

- Path parameter names must correspond to record property names.
- Matching is case-insensitive, but the property must exist.
- Commands always use POST.
- Queries default to POST; use `Method = "GET"` for query-string APIs.
- Only path parameter values are URL-escaped; body properties are serialized as JSON.
- Keep ASP.NET binding attributes off shared request records. BluQube generates server-side shims when needed.
