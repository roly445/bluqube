using BluQube.Commands;
using BluQube.Samples.Blazor.Client.Infrastructure.Commands;
using BluQube.Samples.Blazor.Client.Infrastructure.Constants;
using BluQube.Samples.Blazor.Infrastructure.Data;
using FluentValidation;

namespace BluQube.Samples.Blazor.Infrastructure.CommandHandlers;

public class MarkTodoAsCompletedCommandHandler(ITodoService todoService, IEnumerable<IValidator<MarkTodoAsCompletedCommand>> validators, ILogger<MarkTodoAsCompletedCommandHandler> logger)
    : CommandHandler<MarkTodoAsCompletedCommand>(validators, logger)
{
    protected override Task<CommandResult> HandleInternal(MarkTodoAsCompletedCommand request, CancellationToken cancellationToken)
    {
        var todo = todoService.Todos.SingleOrDefault(x => x.Id == request.TodoId);
        if (todo is null)
        {
            return Task.FromResult(CommandResult.Failed(new BluQubeErrorData(ErrorCodes.NotFound, "Todo not found")));
        }

        todo.MarkAsCompleted();
        return Task.FromResult(CommandResult.Succeeded());
    }
}