# Authorization Guide

BluQube provides a built-in authorization pipeline that enforces access control before handlers run. Authorization is opt-in by registering an `IBluQubeAuthorizer<TRequest>` for a command or query type. If authorization fails, a `CommandResult.Unauthorized()` or `QueryResult<T>.Unauthorized()` is returned automatically and your handler never runs.

You can also enable authorize-by-default mode. In that mode, every command or query must either have an `IBluQubeAuthorizer<TRequest>` or explicitly implement `IAllowAnonymousBluQubeRequest`.

## Setup

In `Program.cs`, after adding Mediator, call `AddBluQubeAuthorization()`:

```csharp
using BluQube.Authorization;

builder.Services.AddMediator();
builder.Services.AddBluQubeAuthorization(typeof(App).Assembly);
```

The `AddBluQubeAuthorization()` call:

- Scans the specified assembly for `IBluQubeAuthorizer<T>` implementations and registers them
- Adds the `BluQubeAuthorizationBehavior` to the Mediator pipeline

## Authorize by Default

By default, requests without an authorizer are allowed. To make BluQube fail closed, enable `RequireAuthorizationByDefault`:

```csharp
builder.Services.AddBluQubeAuthorization(typeof(App).Assembly, options =>
{
    options.RequireAuthorizationByDefault = true;
});
```

With this option enabled:

- If an `IBluQubeAuthorizer<TRequest>` is registered, it runs.
- If no authorizer is registered, the request is rejected.
- If the request implements `IAllowAnonymousBluQubeRequest`, it is allowed without an authorizer.

Use `IAllowAnonymousBluQubeRequest` for intentionally public commands or queries:

```csharp
public record LoginCommand(string Email, string Password)
    : ICommand<LoginResult>, IAllowAnonymousBluQubeRequest;
```

The anonymous marker only bypasses the missing-authorizer rejection. If an authorizer is registered for the request type, the authorizer still runs.

## Protecting a Command

To require authorization for a command, implement `IBluQubeAuthorizer<TCommand>`:

```csharp
using BluQube.Authorization;

public class AddTodoCommandAuthorizer(IHttpContextAccessor httpContextAccessor)
    : IBluQubeAuthorizer<AddTodoCommand>
{
    public Task<AuthorizationResult> Authorize(
        AddTodoCommand request,
        CancellationToken cancellationToken)
    {
        var user = httpContextAccessor.HttpContext?.User;
        return Task.FromResult(
            user?.Identity?.IsAuthenticated == true
                ? AuthorizationResult.Succeed()
                : AuthorizationResult.Fail("User must be authenticated."));
    }
}
```

The authorizer is discovered and registered automatically by `AddBluQubeAuthorization()`. A registered authorizer always runs before the handler whenever a command of that type is sent.

```csharp
public class AddTodoCommandHandler(
    IEnumerable<IValidator<AddTodoCommand>> validators,
    ILogger<AddTodoCommandHandler> logger,
    ITodoService todoService)
    : CommandHandler<AddTodoCommand>(validators, logger)
{
    protected override async Task<CommandResult> HandleInternal(
        AddTodoCommand request,
        CancellationToken cancellationToken)
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
    return RedirectToLogin();
}

if (result.Status == CommandResultStatus.Succeeded)
{
    // Command executed successfully
}
```

The `CommandRunner.Send()` method automatically catches `UnauthorizedException` from the pipeline and converts it to `CommandResult.Unauthorized()`.

## Request-Specific Rules

Authorizers can inspect request data and user context together:

```csharp
public class UpdateTodoCommandAuthorizer(
    IHttpContextAccessor httpContextAccessor,
    ITodoService todoService)
    : IBluQubeAuthorizer<UpdateTodoCommand>
{
    public async Task<AuthorizationResult> Authorize(
        UpdateTodoCommand request,
        CancellationToken cancellationToken)
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

Queries support authorization the same way as commands. Add an `IBluQubeAuthorizer<TQuery>` for the query type:

```csharp
public class GetUserTodosQueryAuthorizer(IHttpContextAccessor httpContextAccessor)
    : IBluQubeAuthorizer<GetUserTodosQuery>
{
    public Task<AuthorizationResult> Authorize(
        GetUserTodosQuery request,
        CancellationToken cancellationToken)
    {
        var user = httpContextAccessor.HttpContext?.User;
        return Task.FromResult(
            user?.Identity?.IsAuthenticated == true
                ? AuthorizationResult.Succeed()
                : AuthorizationResult.Fail("User must be authenticated."));
    }
}
```

When the query is sent through `QueryRunner.Send()`, unauthorized access returns `QueryResult<T>.Unauthorized()`.

## Common Mistakes

### 1. Forgetting the Authorizer

If `RequireAuthorizationByDefault` is `false`, a command or query without an `IBluQubeAuthorizer<TRequest>` is allowed.

```csharp
// No authorizer exists for AddTodoCommand, so no authorization runs unless
// RequireAuthorizationByDefault is enabled.
public record AddTodoCommand(string Title) : ICommand;
```

If `RequireAuthorizationByDefault` is `true`, either add an authorizer or mark the request as intentionally public:

```csharp
public record PublicTodosQuery : IQuery<TodoListResult>, IAllowAnonymousBluQubeRequest;
```

### 2. Forgetting to Register Authorization

If you forget `AddBluQubeAuthorization()` in `Program.cs`, `IBluQubeAuthorizer<T>` implementations will not be registered and no authorization will run.

```csharp
// Missing this line
builder.Services.AddBluQubeAuthorization(typeof(App).Assembly);
```

### 3. Accessing `Data` Without Checking Status

`CommandResult` and `QueryResult<T>` throw an `InvalidOperationException` if you access error/data properties when the status does not match:

```csharp
// Will throw if Status != Failed
var errorData = result.ErrorData;

// Check status first
if (result.Status == CommandResultStatus.Failed)
{
    var errorData = result.ErrorData;
}
```

## See Also

- [MIGRATION-MEDIATR-TO-MEDIATOR.md](MIGRATION-MEDIATR-TO-MEDIATOR.md) - migration guide from MediatR
- [VALIDATION_GUIDE.md](VALIDATION_GUIDE.md) - validation pipeline and error handling
- [README.md](../README.md#authorization) - high-level overview
