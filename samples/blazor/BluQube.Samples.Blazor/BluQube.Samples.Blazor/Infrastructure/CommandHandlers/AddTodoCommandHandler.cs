using BluQube.Commands;
using BluQube.Samples.Blazor.Client.Infrastructure.CommandResults;
using BluQube.Samples.Blazor.Client.Infrastructure.Commands;
using BluQube.Samples.Blazor.Infrastructure.Data;
using FluentValidation;

namespace BluQube.Samples.Blazor.Infrastructure.CommandHandlers;

public class AddTodoCommandHandler(ITodoService todoService, IEnumerable<IValidator<AddTodoCommand>> validators, ILogger<AddTodoCommandHandler> logger)
    : CommandHandler<AddTodoCommand, AddTodoCommandResult>(validators, logger)
{
    protected override Task<CommandResult<AddTodoCommandResult>> HandleInternal(AddTodoCommand request, CancellationToken cancellationToken)
    {
        var todo = todoService.AddTodo(request.Title);

        return Task.FromResult(CommandResult<AddTodoCommandResult>.Succeeded(new AddTodoCommandResult(todo.Id)));
    }
}