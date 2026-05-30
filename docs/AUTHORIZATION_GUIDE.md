# Authorization Guide

BluQube provides a built-in authorization pipeline that enforces access control at the handler level. When a command or query handler is decorated with an `[Authorize]` attribute, the authorization behavior runs before your handler executes. If authorization fails, a `CommandResult.Unauthorized()` or `QueryResult<T>.Unauthorized()` is returned automatically—your handler never runs.

## Setup

Authorization requires two steps in your application's DI configuration:

### 1. Enable the Authorization Behavior

In `Program.cs`, after adding Mediator, call `AddBluQubeAuthorization()`:

```csharp
using BluQube.Authorization;

// ... other service registrations ...

builder.Services.AddMediator();
builder.Services.AddBluQubeAuthorization(typeof(App).Assembly);
```

The `AddBluQubeAuthorization()` call:
- Scans the specified assembly for `IBluQubeAuthorizer<T>` implementations and registers them
- Adds the `BluQubeAuthorizationBehavior` to the Mediator pipeline

### 2. Define Authorization Policies (Optional)

If you want to use policy-based authorization (beyond simple `[Authorize]`), define policies in ASP.NET Core:

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => 
        policy.RequireRole("Admin"));
    options.AddPolicy("Moderators", policy =>
        policy.RequireRole("Moderator", "Admin"));
});
```

## Protecting a Command

To require authentication for a command, decorate the handler with `[Authorize]`:

```csharp
using BluQube.Authorization;

[Authorize]  // Any authenticated user can execute
public class AddTodoCommandHandler(
    IEnumerable<IValidator<AddTodoCommand>> validators,
    ILogger<AddTodoCommandHandler> logger,
    ITodoService todoService)
    : CommandHandler<AddTodoCommand>(validators, logger)
{
    protected override async Task<CommandResult> HandleInternal(
        AddTodoCommand request, CancellationToken cancellationToken)
    {
        await todoService.AddAsync(request.Title, cancellationToken);
        return CommandResult.Succeeded();
    }
}
```

When the command is sent:

```csharp
var result = await commandRunner.Send(new AddTodoCommand("My Todo"));

if (result.Status == CommandResultStatus.Unauthorized)
{
    // User is not authenticated
    return RedirectToLogin();
}

if (result.Status == CommandResultStatus.Succeeded)
{
    // Command executed successfully
}
```

The `CommandRunner.Send()` method automatically catches `UnauthorizedException` from the pipeline and converts it to `CommandResult.Unauthorized()`.

## Policy-Based Authorization

For fine-grained control, use policy-based authorization by specifying the policy name on the `[Authorize]` attribute:

```csharp
[Authorize("AdminOnly")]  // Only users with Admin role can execute
public class DeleteTodoCommandHandler(
    IEnumerable<IValidator<DeleteTodoCommand>> validators,
    ILogger<DeleteTodoCommandHandler> logger,
    ITodoService todoService)
    : CommandHandler<DeleteTodoCommand>(validators, logger)
{
    protected override async Task<CommandResult> HandleInternal(
        DeleteTodoCommand request, CancellationToken cancellationToken)
    {
        var result = todoService.DeleteTodo(request.TodoId);
        return result 
            ? CommandResult.Succeeded() 
            : CommandResult.Failed(new BluQubeErrorData("DeleteFailed", "Todo not found."));
    }
}
```

## Dynamic Authorization Logic with Authorizers

For complex authorization that depends on request data or user context, implement `IBluQubeAuthorizer<TRequest>`:

```csharp
using BluQube.Authorization;

public class AddTodoCommandAuthorizer(IHttpContextAccessor httpContextAccessor)
    : IBluQubeAuthorizer<AddTodoCommand>
{
    public Task<AuthorizationResult> Authorize(
        AddTodoCommand request, CancellationToken cancellationToken)
    {
        var user = httpContextAccessor.HttpContext?.User;
        return Task.FromResult(
            user?.Identity?.IsAuthenticated == true
                ? AuthorizationResult.Succeed()
                : AuthorizationResult.Fail("User must be authenticated."));
    }
}
```

The authorizer is discovered and registered automatically by `AddBluQubeAuthorization()`. It runs before the handler whenever a command of that type is sent, allowing you to inspect request data and user context together:

```csharp
public class UpdateTodoCommandAuthorizer(
    IHttpContextAccessor httpContextAccessor,
    ITodoService todoService)
    : IBluQubeAuthorizer<UpdateTodoCommand>
{
    public async Task<AuthorizationResult> Authorize(
        UpdateTodoCommand request, CancellationToken cancellationToken)
    {
        var userId = httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value;
        var todo = await todoService.GetTodoAsync(request.TodoId, cancellationToken);

        if (todo?.OwnerId == userId)
        {
            return AuthorizationResult.Succeed();
        }

        var isAdmin = httpContextAccessor.HttpContext?.User.IsInRole("Admin") == true;
        return isAdmin
            ? AuthorizationResult.Succeed()
            : AuthorizationResult.Fail("Only the owner or an admin can update this todo.");
    }
}
```

## Query Authorization

Queries support authorization the same way as commands. Decorate query handlers with `[Authorize]`:

```csharp
[Authorize]
public class GetUserTodosQueryProcessor(ITodoService todoService)
    : GenericQueryProcessor<GetUserTodosQuery, GetUserTodosResult>
{
    public async ValueTask<QueryResult<GetUserTodosResult>> Handle(
        GetUserTodosQuery request, CancellationToken cancellationToken)
    {
        var todos = await todoService.GetUserTodosAsync(cancellationToken);
        return QueryResult<GetUserTodosResult>.Succeeded(
            new GetUserTodosResult(todos));
    }
}
```

When the query is sent through `QueryRunner.Send()`, unauthorized access returns `QueryResult<T>.Unauthorized()`:

```csharp
var result = await queryRunner.Send(new GetUserTodosQuery());

if (result.Status == QueryResultStatus.Unauthorized)
{
    // User is not authenticated; redirect to login
    return RedirectToLogin();
}

var todos = result.Data;  // Only accessible if Status == Succeeded
```

## Handling Unauthorized Results in the UI

In Blazor components, check for unauthorized status and handle navigation:

```csharp
@page "/my-todos"
@using BluQube.Commands

@inject NavigationManager NavManager
@inject ICommandRunner CommandRunner

<PageTitle>My Todos</PageTitle>

@if (isUnauthorized)
{
    <p>You must be logged in to view this page.</p>
}
else if (todos != null)
{
    <div>
        @foreach (var todo in todos)
        {
            <TodoItem Todo="@todo" />
        }
    </div>
}

@code {
    private List<Todo>? todos;
    private bool isUnauthorized;

    protected override async Task OnInitializedAsync()
    {
        var result = await CommandRunner.Send(new GetMyTodosQuery());

        if (result.Status == QueryResultStatus.Unauthorized)
        {
            isUnauthorized = true;
            NavManager.NavigateTo("/access");
            return;
        }

        todos = result.Data.Todos;
    }
}
```

## Common Mistakes

### 1. **Attribute on the Wrong Class**

❌ **Wrong** — putting `[Authorize]` on the command class:
```csharp
[Authorize]
public record AddTodoCommand(string Title) : ICommand;
```

✅ **Correct** — put it on the handler:
```csharp
[Authorize]
public class AddTodoCommandHandler : CommandHandler<AddTodoCommand>
{
    // ...
}
```

The authorization behavior runs against the handler, not the command.

### 2. **Forgetting to Register the Authorization Behavior**

If you forget `AddBluQubeAuthorization()` in `Program.cs`, handlers with `[Authorize]` attributes will be ignored—no authorization will run, and all requests will succeed.

```csharp
// ❌ Missing this line
builder.Services.AddBluQubeAuthorization(typeof(App).Assembly);
```

### 3. **Using a Policy That Isn't Defined**

If you reference a policy in `[Authorize("MyPolicy")]` but never define it with `AddAuthorization(options => ...)`, the authorization behavior will fail with an exception.

```csharp
// ❌ Policy "AdminOnly" not defined
[Authorize("AdminOnly")]
public class DeleteHandler : CommandHandler<DeleteCommand> { }

// ✅ Define the policy first
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});
```

### 4. **Accessing `Data` Without Checking Status**

`CommandResult` and `QueryResult<T>` throw an `InvalidOperationException` if you access error/data properties when the status doesn't match:

```csharp
// ❌ Will throw if Status != Succeeded
var errorData = result.ErrorData;  // Only valid when Status == Failed

// ✅ Check status first
if (result.Status == CommandResultStatus.Failed)
{
    var errorData = result.ErrorData;
}
```

## See Also

- [MIGRATION-MEDIATR-TO-MEDIATOR.md](MIGRATION-MEDIATR-TO-MEDIATOR.md) — migration guide from MediatR
- [VALIDATION_GUIDE.md](VALIDATION_GUIDE.md) — validation pipeline and error handling
- [README.md](../README.md#authorization) — high-level overview
