using MediatR;

namespace BluQube.Commands;

public interface ICommandHandler<in TCommand>
    : IRequestHandler<TCommand, CommandResult>
    where TCommand : ICommand;