# Validation And Authorization

## Command Validation

BluQube command validation uses FluentValidation through `CommandHandler<T>`.

```csharp
using FluentValidation;

public class AddTodoCommandValidator : AbstractValidator<AddTodoCommand>
{
    public AddTodoCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(100);
    }
}
```

Register validators:

```csharp
builder.Services.AddValidatorsFromAssemblyContaining<AddTodoCommandValidator>();
```

Handlers must accept validators and pass them to the base class:

```csharp
public class AddTodoCommandHandler(
    IEnumerable<IValidator<AddTodoCommand>> validators,
    ILogger<AddTodoCommandHandler> logger)
    : CommandHandler<AddTodoCommand>(validators, logger)
{
    protected override Task<CommandResult> HandleInternal(
        AddTodoCommand request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(CommandResult.Succeeded());
    }
}
```

If validation fails, `HandleInternal` does not run and the result status is `Invalid`.

## Query Validation

Queries do not use the command validation pipeline. Validate inside the processor and return `QueryResult<T>.Failed(...)`, `NotFound()`, or `Empty()` as appropriate.

## Authorization

Register BluQube mediation and authorization during server setup:

```csharp
builder.Services.AddBluQube(typeof(Program).Assembly);
builder.Services.AddBluQubeAuthorization(typeof(Program).Assembly);
```

`AddBluQube(...)` registers BluQube's first-party mediator and scans for handlers/processors. `AddBluQubeAuthorization(...)` scans for `IBluQubeAuthorizer<TRequest>` implementations and adds the authorization behavior to the BluQube pipeline.

Create one authorizer per protected request type:

```csharp
using BluQube.Authorization;

public class DeleteTodoCommandAuthorizer(IHttpContextAccessor accessor)
    : IBluQubeAuthorizer<DeleteTodoCommand>
{
    public Task<AuthorizationResult> Authorize(
        DeleteTodoCommand request,
        CancellationToken cancellationToken)
    {
        var user = accessor.HttpContext?.User;
        return Task.FromResult(
            user?.Identity?.IsAuthenticated == true
                ? AuthorizationResult.Succeed()
                : AuthorizationResult.Fail("User must be authenticated."));
    }
}
```

Unauthorized commands return `CommandResult.Unauthorized()`. Unauthorized queries return `QueryResult<T>.Unauthorized()`.

## Authorize By Default

To reject requests without authorizers:

```csharp
builder.Services.AddBluQubeAuthorization(typeof(Program).Assembly, options =>
{
    options.RequireAuthorizationByDefault = true;
});
```

Mark intentionally public requests with `IAllowAnonymousBluQubeRequest`:

```csharp
public record LoginCommand(string Email, string Password)
    : ICommand<LoginResult>, IAllowAnonymousBluQubeRequest;
```

If an authorizer exists for an anonymous request type, the authorizer still runs.

## Result Access

Access status-specific properties only after checking status:

```csharp
if (result.Status == CommandResultStatus.Invalid)
{
    var failures = result.ValidationResult.Failures;
}

if (result.Status == CommandResultStatus.Failed)
{
    var error = result.ErrorData;
}
```
