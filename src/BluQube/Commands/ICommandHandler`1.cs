using MediatR;

namespace BluQube.Commands;

/// <summary>
/// Defines a handler for commands that do not return data.
/// </summary>
/// <typeparam name="TCommand">The type of command this handler processes. Must implement <see cref="ICommand"/>.</typeparam>
/// <remarks>
/// This interface extends MediatR's <c>IRequestHandler</c> and defines the contract for command handlers in the BluQube framework.
/// Implementations should inherit from <see cref="CommandHandler{TCommand}"/> which provides validation pipeline support.
/// </remarks>
public interface ICommandHandler<in TCommand>
    : IRequestHandler<TCommand, CommandResult>
    where TCommand : ICommand;