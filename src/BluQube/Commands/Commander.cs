using MediatR;
using MediatR.Behaviors.Authorization.Exceptions;

namespace BluQube.Commands;

public class Commander(ISender sender) : ICommander
{
    public async Task<CommandResult> Send<TCommand>(TCommand request, CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        try
        {
            return await sender.Send(request, cancellationToken);
        }
        catch (UnauthorizedException)
        {
            return CommandResult.Unauthorized();
        }
    }

    public async Task<CommandResult<TCommandResult>> Send<TCommandResult>(
        ICommand<TCommandResult> request, CancellationToken cancellationToken = default)
        where TCommandResult : ICommandResult
    {
        try
        {
            return await sender.Send(request, cancellationToken);
        }
        catch (UnauthorizedException)
        {
            return CommandResult<TCommandResult>.Unauthorized();
        }
    }
}