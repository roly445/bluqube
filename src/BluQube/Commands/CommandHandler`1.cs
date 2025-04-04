using FluentValidation;
using Microsoft.Extensions.Logging;

namespace BluQube.Commands;

public abstract class CommandHandler<TCommand>(IEnumerable<IValidator<TCommand>> validators, ILogger logger)
    : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    public async Task<CommandResult> Handle(TCommand request, CancellationToken cancellationToken)
    {
        var failures = validators
            .Select(v => v.Validate(request))
            .SelectMany(result => result.Errors)
            .Where(error => error != null)
            .ToList();

        if (failures.Count == 0)
        {
            return await this.HandleInternal(request, cancellationToken);
        }

        logger.LogInformation("Command validation failed");
        return CommandResult.Invalid(new CommandValidationResult
        {
            Failures = failures.Select(
                x => new CommandValidationFailure(
                    x.ErrorMessage, x.PropertyName, x.AttemptedValue)).ToList(),
        });
    }

    protected abstract Task<CommandResult> HandleInternal(TCommand request, CancellationToken cancellationToken);
}