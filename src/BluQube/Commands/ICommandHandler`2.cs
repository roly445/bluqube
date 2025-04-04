using MediatR;

namespace BluQube.Commands;

public interface ICommandHandler<in TCommand, TResult>
    : IRequestHandler<TCommand, CommandResult<TResult>>
    where TCommand : ICommand<TResult>
    where TResult : ICommandResult;