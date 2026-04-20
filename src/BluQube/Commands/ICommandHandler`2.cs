using MediatR;

namespace BluQube.Commands;

/// <summary>
/// Defines a handler for commands that return typed result data.
/// </summary>
/// <typeparam name="TCommand">The type of command this handler processes. Must implement <see cref="ICommand{TResult}"/>.</typeparam>
/// <typeparam name="TResult">The type of data returned by this handler. Must implement <see cref="ICommandResult"/>.</typeparam>
/// <remarks>
/// This interface extends MediatR's <c>IRequestHandler</c> and defines the contract for command handlers in the BluQube framework.
/// Implementations should inherit from <see cref="CommandHandler{TCommand, TResult}"/> which provides validation pipeline support.
/// </remarks>
public interface ICommandHandler<in TCommand, TResult>
    : IRequestHandler<TCommand, CommandResult<TResult>>
    where TCommand : ICommand<TResult>
    where TResult : ICommandResult;