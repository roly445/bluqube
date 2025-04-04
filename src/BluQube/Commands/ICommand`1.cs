using MediatR;

namespace BluQube.Commands;

public interface ICommand<TResult> : IRequest<CommandResult<TResult>>
    where TResult : ICommandResult;