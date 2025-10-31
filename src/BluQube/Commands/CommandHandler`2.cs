using FluentValidation;
using Microsoft.Extensions.Logging;

namespace BluQube.Commands;

public abstract class CommandHandler<TCommand, TResult>(IEnumerable<IValidator<TCommand>> validators, ILogger logger)
    : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
    where TResult : ICommandResult
{
    public async Task<CommandResult<TResult>> Handle(TCommand request, CancellationToken cancellationToken)
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
        return await this.PostHandle(request, CommandResult<TResult>.Invalid(new CommandValidationResult
        {
            Failures = failures.Select(
                x => new CommandValidationFailure(
                    x.ErrorMessage, x.PropertyName, x.AttemptedValue)).ToList(),
        }), cancellationToken);
    }

    protected abstract Task<CommandResult<TResult>> HandleInternal(
        TCommand request, CancellationToken cancellationToken);

    protected virtual Task<CommandResult<TResult>> PostHandle(TCommand request, CommandResult<TResult> originalCommandResult,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(originalCommandResult);
    }
}