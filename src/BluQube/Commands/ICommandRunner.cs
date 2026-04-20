namespace BluQube.Commands;

/// <summary>
/// Dispatches commands for execution via the MediatR pipeline.
/// </summary>
/// <remarks>
/// This is the primary interface for sending commands to handlers. It wraps MediatR's <c>ISender</c> and adds automatic handling of authorization exceptions.
/// Inject <see cref="ICommandRunner"/> into your services or Blazor components to send commands.
/// <para>
/// The runner catches <c>UnauthorizedException</c> from the MediatR authorization behavior and converts it to <see cref="CommandResult.Unauthorized()"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class ItemService
/// {
///     private readonly ICommandRunner _commandRunner;
///
///     public TodoService(ICommandRunner commandRunner)
///     {
///         _commandRunner = commandRunner;
///     }
///
///     public async Task&lt;CommandResult&gt; CreateTodoAsync(string title, CancellationToken ct)
///     {
///         var command = new CreateTodoCommand(title);
///         return await _commandRunner.Send(command, ct);
///     }
/// }
/// </code>
/// </example>
public interface ICommandRunner
{
    /// <summary>
    /// Sends a command that does not return data for execution.
    /// </summary>
    /// <typeparam name="TCommand">The type of command to send. Must implement <see cref="ICommand"/>.</typeparam>
    /// <param name="request">The command instance to execute.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="CommandResult"/> indicating success, failure, validation errors, or authorization issues.</returns>
    Task<CommandResult> Send<TCommand>(TCommand request, CancellationToken cancellationToken = default)
        where TCommand : ICommand;

    /// <summary>
    /// Sends a command that returns typed result data for execution.
    /// </summary>
    /// <typeparam name="TCommandResult">The type of result data returned by the command. Must implement <see cref="ICommandResult"/>.</typeparam>
    /// <param name="request">The command instance to execute.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="CommandResult{TResult}"/> containing either the result data or error information.</returns>
    Task<CommandResult<TCommandResult>> Send<TCommandResult>(ICommand<TCommandResult> request, CancellationToken cancellationToken = default)
        where TCommandResult : ICommandResult;
}