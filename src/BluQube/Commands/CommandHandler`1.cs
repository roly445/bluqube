using FluentValidation;
using Microsoft.Extensions.Logging;

namespace BluQube.Commands;

public abstract class CommandHandler<TCommand>(IEnumerable<IValidator<TCommand>> validators, ILogger logger)
    : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    public async Task<CommandResult> Handle(TCommand request, CancellationToken cancellationToken)
    {
        var validationTasks = validators
            .Select(v => v.ValidateAsync(request, cancellationToken));

        var results = await Task.WhenAll(validationTasks);

        var failures = results
            .SelectMany(result => result.Errors)
            .Where(error => error != null)
            .ToList();

        if (failures.Count == 0)
        {
            return await this.PostHandle(
                request, await this.HandleInternal(request, cancellationToken), cancellationToken);
        }

        logger.LogInformation("Command validation failed");
        return await this.PostHandle(request, CommandResult.Invalid(new CommandValidationResult
        {
            Failures = failures.Select(
                x => new CommandValidationFailure(
                    x.ErrorMessage, x.PropertyName, x.AttemptedValue)).ToList(),
        }), cancellationToken);
    }

    protected abstract Task<CommandResult> HandleInternal(TCommand request, CancellationToken cancellationToken);

    protected virtual Task<CommandResult> PostHandle(TCommand request, CommandResult originalCommandResult,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(originalCommandResult);
    }
}