# Getting Started with BluQube

Get a working BluQube app up and running in under 30 minutes.

## Prerequisites

- **.NET SDK 8.0 or higher** (8.0, 9.0, or 10.0)
- **Visual Studio, Visual Studio Code, or Rider**
- Basic knowledge of C# and Blazor

## 1. Create a New Blazor Project

```bash
dotnet new blazor -n MyBlazorApp
cd MyBlazorApp
```

Choose either **Blazor Server** or **Blazor WebAssembly** (BluQube supports both).

## 2. Install BluQube

```bash
dotnet add package BluQube
```

(Check [NuGet.org](https://www.nuget.org/packages/BluQube) for the latest version.)

## 3. Register BluQube Services

Open `Program.cs` and add BluQube services to the dependency injection container:

```csharp
using BluQube.Attributes;
using BluQube.Commands;
using BluQube.Queries;
using FluentValidation;
using MediatR.Behaviors.Authorization.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http.Json;

var builder = WebApplication.CreateBuilder(args);

// Add Razor components
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// Register validators
builder.Services.AddValidatorsFromAssemblyContaining(typeof(Program));

// Register MediatR and handlers
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining(typeof(Program)));
builder.Services.AddMediatorAuthorization(typeof(Program).Assembly);

// Register BluQube runners
builder.Services.AddScoped<ICommandRunner, CommandRunner>();
builder.Services.AddScoped<IQueryRunner, QueryRunner>();

// Configure JSON options with BluQube converters
builder.Services.Configure<JsonOptions>(options =>
{
    options.AddBluQubeJsonConverters();
});

var app = builder.Build();

// ... rest of pipeline ...

// Add BluQube endpoints (this generates handlers for your commands/queries)
app.AddBluQubeApi();

app.Run();
```

## 4. Your First Command

Commands represent actions that modify application state.

### Define the Command

Create `Commands/AddTodoCommand.cs`:

```csharp
using BluQube.Attributes;
using BluQube.Commands;

namespace MyBlazorApp.Commands;

[BluQubeCommand(Path = "commands/add-todo")]
public record AddTodoCommand(string Title, string Description) : ICommand;
```

### Create a Validator (Optional)

Create `Validators/AddTodoCommandValidator.cs`:

```csharp
using FluentValidation;
using MyBlazorApp.Commands;

namespace MyBlazorApp.Validators;

public class AddTodoCommandValidator : AbstractValidator<AddTodoCommand>
{
    public AddTodoCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(100).WithMessage("Title must not exceed 100 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters");
    }
}
```

### Implement the Handler

Create `CommandHandlers/AddTodoCommandHandler.cs`:

```csharp
using BluQube.Commands;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace MyBlazorApp.CommandHandlers;

public class AddTodoCommandHandler(
    IEnumerable<IValidator<AddTodoCommand>> validators,
    ILogger<AddTodoCommandHandler> logger)
    : CommandHandler<AddTodoCommand>(validators, logger)
{
    protected override async Task<CommandResult> HandleInternal(
        AddTodoCommand request, CancellationToken cancellationToken)
    {
        // Your business logic: save to database, call an API, etc.
        logger.LogInformation("Adding todo: {Title}", request.Title);
        
        // Simulate async work
        await Task.Delay(100, cancellationToken);
        
        return CommandResult.Succeeded();
    }
}
```

### Invoke from a Blazor Component

The source generator automatically creates a **client requester** for `AddTodoCommand`. Use `ICommandRunner`:

```razor
@page "/todos"
@inject ICommandRunner CommandRunner
@rendermode InteractiveWebAssembly

<h2>Add Todo</h2>

<input @bind="title" placeholder="Title" />
<input @bind="description" placeholder="Description" />
<button @onclick="AddTodo">Add</button>

@if (!string.IsNullOrEmpty(message))
{
    <p>@message</p>
}

@code {
    private string title = string.Empty;
    private string description = string.Empty;
    private string message = string.Empty;

    private async Task AddTodo()
    {
        var command = new AddTodoCommand(title, description);
        var result = await CommandRunner.Send(command);

        if (result.Status == CommandResultStatus.Succeeded)
        {
            message = "Todo added!";
            title = string.Empty;
            description = string.Empty;
        }
        else if (result.Status == CommandResultStatus.Invalid)
        {
            var failures = result.ValidationResult.Failures;
            message = string.Join("; ", failures.Select(f => f.Message));
        }
        else
        {
            message = "An error occurred.";
        }
    }
}
```

## 5. Your First Query

Queries retrieve data without modifying state.

### Define the Query and Result

Create `Queries/GetTodosQuery.cs`:

```csharp
using BluQube.Attributes;
using BluQube.Queries;

namespace MyBlazorApp.Queries;

[BluQubeQuery(Path = "queries/get-todos")]
public record GetTodosQuery : IQuery<TodoListResult>;

public record TodoListResult(List<TodoItem> Items) : IQueryResult;

public record TodoItem(Guid Id, string Title, string Description);
```

### Implement the Query Processor

Create `QueryProcessors/GetTodosQueryProcessor.cs`:

```csharp
using BluQube.Queries;

namespace MyBlazorApp.QueryProcessors;

public class GetTodosQueryProcessor : GenericQueryProcessor<GetTodosQuery, TodoListResult>
{
    private readonly ILogger<GetTodosQueryProcessor> _logger;

    public GetTodosQueryProcessor(
        IHttpClientFactory httpClientFactory,
        QueryResultConverter<TodoListResult> jsonConverter,
        ILogger<GetTodosQueryProcessor> logger)
        : base(httpClientFactory, jsonConverter, logger)
    {
        _logger = logger;
    }

    protected override string Path => "queries/get-todos";

    public override async Task<QueryResult<TodoListResult>> Handle(
        GetTodosQuery request, CancellationToken cancellationToken)
    {
        // Your business logic: fetch from database, call an API, etc.
        _logger.LogInformation("Fetching todos");

        var items = new List<TodoItem>
        {
            new(Guid.NewGuid(), "Learn BluQube", "Understand CQRS pattern"),
            new(Guid.NewGuid(), "Build an app", "Deploy to production"),
        };

        return QueryResult<TodoListResult>.Succeeded(new TodoListResult(items));
    }
}
```

### Invoke from a Blazor Component

Use `IQueryRunner`:

```razor
@page "/todo-list"
@inject IQueryRunner QueryRunner
@rendermode InteractiveWebAssembly

<h2>My Todos</h2>

<button @onclick="LoadTodos">Load Todos</button>

@if (todos != null)
{
    <ul>
        @foreach (var todo in todos.Items)
        {
            <li>
                <strong>@todo.Title</strong><br />
                @todo.Description
            </li>
        }
    </ul>
}

@code {
    private TodoListResult? todos;

    protected override async Task OnInitializedAsync()
    {
        await LoadTodos();
    }

    private async Task LoadTodos()
    {
        var query = new GetTodosQuery();
        var result = await QueryRunner.Send(query);

        if (result.IsSucceeded)
        {
            todos = result.Data;
        }
        else if (result.IsNotFound)
        {
            todos = new TodoListResult(new List<TodoItem>());
        }
    }
}
```

## 6. What Just Happened?

**Source Generation** — BluQube used Roslyn source generators to automatically:

1. **On the client:** Created HTTP requesters that serialize your commands/queries to the server
2. **On the server:** Created API endpoints (`/commands/add-todo`, `/queries/get-todos`) that deserialize requests and route them to your handlers

This happens at build time — no runtime reflection, fully AOT-safe.

**Validation Pipeline:**
- Validators run *before* `HandleInternal()` / `Handle()`
- Validation failures return `CommandResult.Invalid(...)` immediately
- Handlers only run if validation passes

**Result Types:**
- `CommandResult.Succeeded()` — command completed successfully
- `CommandResult.Invalid(...)` — validation failed
- `CommandResult.Failed(...)` — handler threw an error
- `QueryResult<T>.Succeeded(data)` — query succeeded with data
- `QueryResult<T>.NotFound()` — query succeeded but entity not found
- `QueryResult<T>.Empty()` — collection query succeeded with zero results

## 7. Build and Run

```bash
dotnet build
dotnet run
```

Navigate to `https://localhost:7000` (or the port shown) and test your commands and queries.

## 8. Next Steps

- **Add Authentication?** → See [Authorization Guide](./AUTHORIZATION_GUIDE.md) (coming soon)
- **Add More Validation?** → See [Validation Guide](./VALIDATION_GUIDE.md) (coming soon)
- **Build REST-style endpoints?** → See [URL Binding Guide](./URL_BINDING_GUIDE.md) for path parameters and query strings
- **Stuck?** → Check [Troubleshooting](./TROUBLESHOOTING.md) (coming soon)
- **Want to contribute?** → See [CONTRIBUTING.md](../CONTRIBUTING.md) (coming soon)

## Key Files You Just Created

| File | Purpose |
|------|---------|
| `Commands/AddTodoCommand.cs` | Command definition with `[BluQubeCommand]` attribute |
| `CommandHandlers/AddTodoCommandHandler.cs` | Logic to handle the command |
| `Validators/AddTodoCommandValidator.cs` | FluentValidation rules for the command |
| `Queries/GetTodosQuery.cs` | Query definition with `[BluQubeQuery]` attribute |
| `QueryProcessors/GetTodosQueryProcessor.cs` | Logic to execute the query |
| Blazor components (`.razor` files) | UI that invokes commands and queries |

## Common Patterns

### Command with Path Parameter

For RESTful endpoints like `DELETE /commands/todo/{id}`:

```csharp
[BluQubeCommand(Path = "commands/todo/{id}")]
public record DeleteTodoCommand(Guid Id) : ICommand;

// Handler receives the command with Id already bound
public class DeleteTodoCommandHandler : CommandHandler<DeleteTodoCommand>
{
    protected override async Task<CommandResult> HandleInternal(
        DeleteTodoCommand request, CancellationToken cancellationToken)
    {
        // request.Id is automatically extracted from the URL
        return CommandResult.Succeeded();
    }
}
```

### GET Query (Idempotent)

For `GET /queries/todo/{id}?filter=completed`:

```csharp
[BluQubeQuery(Path = "queries/todo/{id}", Method = "GET")]
public record GetTodoQuery(Guid Id, string? Filter = null) : IQuery<TodoDetailResult>;

// {id} comes from the route; Filter comes from the query string
```

### Unauthorized Handler

Use `[Authorize]` attribute to require authentication:

```csharp
[Authorize]
public class AdminOnlyCommandHandler : CommandHandler<AdminCommand>
{
    // This handler requires the user to be authenticated
    // ...
}
```

---

**Ready to dive deeper?** Each feature has a dedicated guide in the `/docs` folder. Happy building! 🚀
