# Commands And Queries

## Commands

Commands modify state.

```csharp
using BluQube.Attributes;
using BluQube.Commands;

[BluQubeCommand(Path = "commands/todo/add")]
public record AddTodoCommand(string Title) : ICommand<AddTodoCommandResult>;

public record AddTodoCommandResult(Guid Id);
```

Use `ICommand` for commands without data and `ICommand<TResult>` for commands that return data.

## Command Handlers

Put handlers on the server.

```csharp
using BluQube.Commands;
using FluentValidation;

public class AddTodoCommandHandler(
    ITodoService todoService,
    IEnumerable<IValidator<AddTodoCommand>> validators,
    ILogger<AddTodoCommandHandler> logger)
    : CommandHandler<AddTodoCommand, AddTodoCommandResult>(validators, logger)
{
    protected override Task<CommandResult<AddTodoCommandResult>> HandleInternal(
        AddTodoCommand request,
        CancellationToken cancellationToken)
    {
        var todo = todoService.AddTodo(request.Title);
        return Task.FromResult(
            CommandResult<AddTodoCommandResult>.Succeeded(
                new AddTodoCommandResult(todo.Id)));
    }
}
```

For commands with no return data, inherit `CommandHandler<TCommand>` and return `CommandResult.Succeeded()`, `CommandResult.Invalid(...)`, `CommandResult.Failed(...)`, or `CommandResult.Unauthorized()`.

## Queries

Queries read state.

```csharp
using BluQube.Attributes;
using BluQube.Queries;

[BluQubeQuery(Path = "queries/todos", Method = "GET")]
public record GetTodosQuery : IQuery<GetTodosResult>;

public record GetTodosResult(IReadOnlyList<TodoItem> Items) : IQueryResult;
```

## Query Processors

Return a precise result status.

```csharp
using BluQube.Queries;

public class GetTodosQueryProcessor(ITodoService todoService)
    : IQueryProcessor<GetTodosQuery, GetTodosResult>
{
    public Task<QueryResult<GetTodosResult>> Handle(
        GetTodosQuery request,
        CancellationToken cancellationToken)
    {
        var todos = todoService.GetTodos();

        return Task.FromResult(
            todos.Count == 0
                ? QueryResult<GetTodosResult>.Empty()
                : QueryResult<GetTodosResult>.Succeeded(
                    new GetTodosResult(todos)));
    }
}
```

Use:

- `QueryResult<T>.Succeeded(data)` when data exists.
- `QueryResult<T>.NotFound()` when a single requested entity does not exist.
- `QueryResult<T>.Empty()` when a collection query has no rows.
- `QueryResult<T>.Failed(error)` for execution errors.
- `QueryResult<T>.Unauthorized()` for authorization failure.

## Calling From Components Or Services

```razor
@inject ICommandRunner CommandRunner
@inject IQueryRunner QueryRunner

@code {
    private async Task Add()
    {
        var result = await CommandRunner.Send(new AddTodoCommand("Learn BluQube"));

        if (result.IsSucceeded)
        {
            var id = result.Data.Id;
        }
        else if (result.Status == CommandResultStatus.Invalid)
        {
            var failures = result.ValidationResult.Failures;
        }
    }
}
```

Always check status before accessing status-specific properties.
