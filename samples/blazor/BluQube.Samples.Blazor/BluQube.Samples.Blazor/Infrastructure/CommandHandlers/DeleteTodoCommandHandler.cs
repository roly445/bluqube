using BluQube.Commands;
using BluQube.Samples.Blazor.Client.Infrastructure.Commands;
using BluQube.Samples.Blazor.Client.Infrastructure.Constants;
using BluQube.Samples.Blazor.Infrastructure.Data;
using FluentValidation;

namespace BluQube.Samples.Blazor.Infrastructure.CommandHandlers;

public class DeleteTodoCommandHandler(ITodoService todoService, IEnumerable<IValidator<DeleteTodoCommand>> validators, ILogger<DeleteTodoCommandHandler> logger)
    : CommandHandler<DeleteTodoCommand>(validators, logger)
{
    protected override Task<CommandResult> HandleInternal(DeleteTodoCommand request, CancellationToken cancellationToken)
    {
        var result = todoService.DeleteTodo(request.TodoId);
        return Task.FromResult(result ? CommandResult.Succeeded() : CommandResult.Failed(new BluQubeErrorData(ErrorCodes.DeleteFailed, "Failed to delete Todo.")));
    }
}