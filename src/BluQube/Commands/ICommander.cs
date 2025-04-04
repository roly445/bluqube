namespace BluQube.Commands;

public interface ICommander
{
    Task<CommandResult> Send<TCommand>(TCommand request, CancellationToken cancellationToken = default)
        where TCommand : ICommand;

    Task<CommandResult<TCommandResult>> Send<TCommandResult>(ICommand<TCommandResult> request, CancellationToken cancellationToken = default)
        where TCommandResult : ICommandResult;
}