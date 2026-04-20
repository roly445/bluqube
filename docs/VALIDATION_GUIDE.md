# Validation Guide

BluQube uses [FluentValidation](https://docs.fluentvalidation.net/) to validate commands and queries before executing handlers. Validators are registered in dependency injection, automatically injected into handlers, and executed as part of the handler pipeline. If validation fails, the handler never runs—the response returns `CommandResult.Invalid()` with failure details.

## How Validation Works

When you send a command:

1. **All validators** for that command type are collected from DI
2. **Validators run in parallel** via `Task.WhenAll` (efficient)
3. **Failures aggregate** across all validators
4. **If any failures exist**, handler skips; response is `CommandResult.Invalid(CommandValidationResult { Failures = [...] })`
5. **If valid**, `HandleInternal()` executes normally

This is built into `CommandHandler<T>.Handle()` and happens automatically—you don't call validators directly.

## Setup

Register validators in `Program.cs`:

```csharp
// In Program.cs before app.Build()
builder.Services.AddValidatorsFromAssemblyContaining<AddTodoCommandValidator>();
```

FluentValidation scans the assembly and registers all `AbstractValidator<T>` implementations. The generic parameter is the command/query type they validate.

## Write a Validator

Inherit from `AbstractValidator<T>` and define rules:

```csharp
using FluentValidation;
using BluQube.Samples.Blazor.Client.Infrastructure.Commands;
using BluQube.Samples.Blazor.Infrastructure.Data;

public class AddTodoCommandValidator : AbstractValidator<AddTodoCommand>
{
    public AddTodoCommandValidator(ITodoService todoService)
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .Must(s => !todoService.Todos.Any(t => 
                t.Title.Equals(s, StringComparison.OrdinalIgnoreCase)))
            .WithMessage("Title must be unique");
    }
}
```

**Key points:**
- Validators can be injected with services (e.g., `ITodoService` for duplicate checking)
- Use `RuleFor(x => x.PropertyName)` to target properties
- Chain rules: `.NotEmpty().MinimumLength(3)`
- Use `.WithMessage("...")` for custom error messages
- Return `Task` or `void` — both work (FluentValidation handles async)

For a more complex example with validation logic:

```csharp
public class UpdateTitleCommandValidator : AbstractValidator<UpdateTitleCommand>
{
    public UpdateTitleCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(100)
            .Matches(@"^[a-zA-Z0-9\s\-]+$")
            .WithMessage("Title can only contain letters, numbers, spaces, and hyphens");

        RuleFor(x => x.TodoId)
            .NotEmpty()
            .WithMessage("TodoId cannot be empty");
    }
}
```

## Multiple Validators

You can register multiple validators for the same command—all run, and failures aggregate:

```csharp
// Each validator inherits from AbstractValidator<DeleteTodoCommand>
public class DeleteTodoCommandValidator : AbstractValidator<DeleteTodoCommand>
{
    public DeleteTodoCommandValidator()
    {
        RuleFor(x => x.TodoId).NotEmpty();
    }
}

public class DeleteTodoAuthValidator : AbstractValidator<DeleteTodoCommand>
{
    public DeleteTodoAuthValidator(IAuthService authService)
    {
        RuleFor(x => x.TodoId)
            .Must(id => authService.CanDelete(id))
            .WithMessage("You cannot delete this todo");
    }
}
```

Both validators run. If either fails, the command returns `Invalid` without executing `HandleInternal()`.

## Query Validation

**Queries do not use validators** in the same pipeline. Query processors (`IQueryProcessor<TQuery, TResult>`) have no built-in validation phase. If you need query validation:

- Perform checks inside the `Handle()` method
- Return `QueryResult<T>.Failed(...)` for validation-like errors
- Or create custom validation logic specific to your query

Example query processor without validators:

```csharp
public class GetTodosQueryProcessor(ITodoService todoService) 
    : IQueryProcessor<GetTodosQuery, GetTodosResult>
{
    public async Task<QueryResult<GetTodosResult>> Handle(
        GetTodosQuery request, CancellationToken cancellationToken)
    {
        // Manual validation/checks if needed
        if (request.PageSize > 1000)
            return QueryResult<GetTodosResult>.Failed(
                new BluQubeErrorData("PageSize cannot exceed 1000"));

        var todos = await todoService.GetAllAsync(cancellationToken);
        return QueryResult<GetTodosResult>.Succeeded(new GetTodosResult(todos));
    }
}
```

## Handling Validation Failures in UI

In your Blazor component, check if validation failed and display errors:

```razor
@page "/add-todo"
@inject ICommandRunner commandRunner

<div class="form">
    <input @bind="title" placeholder="Enter title" />
    <button @onclick="AddTodo">Add</button>

    @if (validationErrors.Any())
    {
        <div class="validation-summary">
            @foreach (var failure in validationErrors)
            {
                <div class="error">
                    <strong>@failure.PropertyName:</strong> @failure.ErrorMessage
                </div>
            }
        </div>
    }
</div>

@code {
    private string title = "";
    private List<CommandValidationFailure> validationErrors = [];

    private async Task AddTodo()
    {
        var result = await commandRunner.Send(new AddTodoCommand(title));

        if (result.Status == CommandResultStatus.Invalid)
        {
            validationErrors = result.ValidationResult.Failures.ToList();
        }
        else if (result.IsSucceeded)
        {
            validationErrors.Clear();
            title = "";
            // Refresh todos list, etc.
        }
    }
}
```

The `CommandValidationResult` contains:
- `Failures` — list of `CommandValidationFailure` objects
- `IsValid` — boolean shortcut

Each `CommandValidationFailure` has:
- `ErrorMessage` — the validation message
- `PropertyName` — which property failed (may be null)
- `AttemptedValue` — the value that failed validation (may be null)

## Common Mistakes

### Validator Registered but Wrong Assembly

```csharp
// ❌ Wrong — scans assembly A, but validators are in assembly B
builder.Services.AddValidatorsFromAssemblyContaining<SomeUnrelatedType>();

// ✅ Correct — scans the assembly containing your validators
builder.Services.AddValidatorsFromAssemblyContaining<AddTodoCommandValidator>();
```

### Missing Validators Injection in Handler

`CommandHandler<T>` requires validators in its constructor:

```csharp
// ✅ Correct
public class MyCommandHandler(
    IEnumerable<IValidator<MyCommand>> validators,  // Required
    ILogger logger)                                  // Required
    : CommandHandler<MyCommand>(validators, logger)
{
    protected override async Task<CommandResult> HandleInternal(
        MyCommand request, CancellationToken cancellationToken)
    {
        // Handler logic
        return CommandResult.Succeeded();
    }
}

// ❌ Wrong — missing validators parameter
public class MyCommandHandler(ILogger logger)
    : CommandHandler<MyCommand>(???, logger)  // What goes here?
{
    // ...
}
```

If validators parameter is missing, MediatR fails to resolve the handler.

### Handler Bypassing Validation

Don't call `HandleInternal()` directly. The pipeline runs validation first:

```csharp
// ❌ Wrong — skips the validation pipeline
public async Task<CommandResult> Handle(MyCommand request, CancellationToken ct)
{
    return await this.HandleInternal(request, ct);  // No validation!
}

// ✅ Correct — use the inherited Handle() method
// It automatically runs validation before HandleInternal()
```

The `Handle()` method in `CommandHandler<T>` is the pipeline entry point. Override it only if you need custom behavior; in most cases, just implement `HandleInternal()`.

### Forgetting to Register Validators

If you write a validator but don't call `AddValidatorsFromAssemblyContaining<T>()`, it won't be registered, and the handler won't run validation:

```csharp
// ❌ In Program.cs — validators ignored
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMediatR(cfg => ...);  // No validator registration!

// ✅ Correct
builder.Services.AddValidatorsFromAssemblyContaining<AddTodoCommandValidator>();
builder.Services.AddMediatR(cfg => ...);
```

### Validation Passes but Handler Still Fails

If a command is valid but `HandleInternal()` fails (e.g., database error), return `CommandResult.Failed(...)`:

```csharp
public class AddTodoCommandHandler : CommandHandler<AddTodoCommand>
{
    protected override async Task<CommandResult> HandleInternal(
        AddTodoCommand request, CancellationToken ct)
    {
        try
        {
            await _db.AddTodo(request.Title);
            return CommandResult.Succeeded();
        }
        catch (Exception ex)
        {
            // Validation passed, but execution failed
            return CommandResult.Failed(
                new BluQubeErrorData("Failed to add todo: " + ex.Message));
        }
    }
}
```

Use `Invalid` for validation failures, `Failed` for business/execution errors.

## Tips

- **Async validation** — validators can use async methods (e.g., database lookups for uniqueness checks)
- **Reusable validators** — share validation logic across handlers by creating base validators or utility classes
- **Test validators independently** — test your `AbstractValidator<T>` classes separately from handlers using FluentValidation's test helpers
- **Order doesn't matter** — all validators run in parallel; don't rely on execution order
- **Empty validators list is valid** — if no validators are registered for a command, it's always valid (no failures = success)

See [FluentValidation docs](https://docs.fluentvalidation.net/latest/) for all available rules and advanced patterns.

## Troubleshooting

**Validator never runs**  
→ Check `AddValidatorsFromAssemblyContaining<T>()` is called with an assembly that contains your validator  
→ Verify validator inherits from `AbstractValidator<YourCommandType>`

**Handler invoked even when validation fails**  
→ You called `HandleInternal()` directly instead of using the inherited `Handle()` method  
→ Remove any custom `Handle()` override and let the base class run validation

**Validation failures not visible in response**  
→ Check `result.Status == CommandResultStatus.Invalid` before accessing `result.ValidationResult`  
→ Accessing properties on wrong status throws `InvalidOperationException`

For more troubleshooting, see [TROUBLESHOOTING.md](./TROUBLESHOOTING.md).
