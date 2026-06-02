namespace BluQube.Commands;

/// <summary>
/// Defines a handler for commands that do not return data.
/// </summary>
/// <typeparam name="TCommand">The type of command this handler processes. Must implement <see cref="ICommand"/>.</typeparam>
/// <remarks>
/// This interface defines the contract for command handlers in the BluQube framework.
/// Implementations should inherit from <see cref="CommandHandler{TCommand}"/> which provides validation pipeline support.
/// </remarks>
public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    /// <summary>
    /// Handles the command.
    /// </summary>
    /// <param name="request">The command to execute.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The command result.</returns>
    ValueTask<CommandResult> Handle(TCommand request, CancellationToken cancellationToken);
}