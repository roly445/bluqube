using BluQube.Commands;
using BluQube.Samples.Blazor.Client.Infrastructure.Commands;
using BluQube.Samples.Blazor.Client.Infrastructure.Constants;
using BluQube.Samples.Blazor.Infrastructure.Data;
using FluentValidation;

namespace BluQube.Samples.Blazor.Infrastructure.CommandHandlers;

public class UpdateToDoTitleCommandHandler(ITodoService todoService, IEnumerable<IValidator<UpdateToDoTitleCommand>> validators, ILogger<UpdateToDoTitleCommandHandler> logger)
    : CommandHandler<UpdateToDoTitleCommand>(validators, logger)
{
    protected override Task<CommandResult> HandleInternal(UpdateToDoTitleCommand request, CancellationToken cancellationToken)
    {
        var todo = todoService.Todos.SingleOrDefault(x => x.Id == request.ToDoId);
        if (todo is null)
        {
            return Task.FromResult(CommandResult.Failed(new ErrorData(ErrorCodes.NotFound, "Todo not found")));
        }

        todo.UpdateTitle(request.Title);
        return Task.FromResult(CommandResult.Succeeded());
    }
}