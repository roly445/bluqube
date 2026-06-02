using BluQube.Authorization;
using BluQube.Mediation;

namespace BluQube.Commands;

/// <summary>
/// Dispatches commands to their handlers via the BluQube mediator pipeline with automatic authorization exception handling.
/// </summary>
/// <remarks>
/// This class wraps <see cref="IBluQubeMediator"/> to provide command execution. It automatically catches <c>UnauthorizedException</c> thrown by the BluQube authorization behavior
/// and converts it to an <see cref="CommandResult.Unauthorized()"/> result, preventing exception propagation to client code.
/// <para>
/// Register this service in your DI container and inject <see cref="ICommandRunner"/> where needed.
/// </para>
/// </remarks>
public class CommandRunner(IBluQubeMediator mediator) : ICommandRunner
{
    /// <inheritdoc/>
    public async Task<CommandResult> Send<TCommand>(TCommand request, CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        try
        {
            return await mediator.Send(request, cancellationToken);
        }
        catch (UnauthorizedException)
        {
            return CommandResult.Unauthorized();
        }
    }

    /// <inheritdoc/>
    public async Task<CommandResult<TCommandResult>> Send<TCommandResult>(
        ICommand<TCommandResult> request, CancellationToken cancellationToken = default)
        where TCommandResult : ICommandResult
    {
        try
        {
            return await mediator.Send(request, cancellationToken);
        }
        catch (UnauthorizedException)
        {
            return CommandResult<TCommandResult>.Unauthorized();
        }
    }
}
