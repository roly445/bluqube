using FluentValidation;
using Microsoft.Extensions.Logging;

namespace BluQube.Commands;

/// <summary>
/// Base class for handlers that execute commands without returning data, with built-in FluentValidation support.
/// </summary>
/// <typeparam name="TCommand">The type of command this handler processes. Must implement <see cref="ICommand"/>.</typeparam>
/// <remarks>
/// Inherit from this class to implement command handlers in the BluQube framework. The base class provides:
/// <list type="bullet">
/// <item><description>Automatic validation using FluentValidation before handler execution.</description></item>
/// <item><description>Aggregation of validation failures from multiple validators.</description></item>
/// <item><description>Short-circuit behavior: if validation fails, <see cref="HandleInternal"/> is never invoked.</description></item>
/// <item><description>Optional <see cref="PostHandle"/> hook for side effects after handler execution (success or validation failure).</description></item>
/// </list>
/// <para>
/// Override <see cref="HandleInternal"/> to implement command logic. Override <see cref="PostHandle"/> for logging, event publishing, or other post-execution side effects.
/// </para>
/// <para>
/// Register validators for <typeparamref name="TCommand"/> in your DI container using <c>AddValidatorsFromAssemblyContaining</c>. The handler constructor injects all registered validators.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class CreateTodoCommandHandler : CommandHandler&lt;CreateTodoCommand&gt;
/// {
///     private readonly ITodoRepository _repository;
///
///     public CreateTodoCommandHandler(
///         IEnumerable&lt;IValidator&lt;CreateTodoCommand&gt;&gt; validators,
///         ILogger&lt;CreateTodoCommandHandler&gt; logger,
///         ITodoRepository repository)
///         : base(validators, logger)
///     {
///         _repository = repository;
///     }
///
///     protected override async Task&lt;CommandResult&gt; HandleInternal(
///         CreateTodoCommand request, CancellationToken cancellationToken)
///     {
///         var entity = new Entity { Title = request.Title };
///         await _repository.AddAsync(entity, cancellationToken);
///         return CommandResult.Succeeded();
///     }
///
///     protected override async Task&lt;CommandResult&gt; PostHandle(
///         CreateTodoCommand request, CommandResult originalResult, CancellationToken cancellationToken)
///     {
///         if (originalResult.Status == CommandResultStatus.Succeeded)
///         {
///             Logger.LogInformation("Item created: {Title}", request.Title);
///         }
///         return await base.PostHandle(request, originalResult, cancellationToken);
///     }
/// }
/// </code>
/// </example>
public abstract class CommandHandler<TCommand>(IEnumerable<IValidator<TCommand>> validators, ILogger logger)
    : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    /// <summary>
    /// Handles the command by running validation, then invoking <see cref="HandleInternal"/> if validation succeeds.
    /// </summary>
    /// <param name="request">The command to execute.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="CommandResult"/> indicating the outcome.
    /// Returns <see cref="CommandResult.Invalid(CommandValidationResult)"/> if validation fails;
    /// otherwise returns the result from <see cref="HandleInternal"/>.
    /// </returns>
    /// <remarks>
    /// This method is called by MediatR. It runs all registered validators in parallel, aggregates failures,
    /// and short-circuits if any validation rules are violated. After handler execution (or validation failure),
    /// it invokes <see cref="PostHandle"/> for side effects.
    /// </remarks>
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

    /// <summary>
    /// Executes the command logic. Override this method to implement handler behavior.
    /// </summary>
    /// <param name="request">The command to execute.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="CommandResult"/> indicating success, failure, or unauthorized status.
    /// Do not return <see cref="CommandResult.Invalid(CommandValidationResult)"/> from this method; validation is handled automatically by <see cref="Handle"/>.
    /// </returns>
    /// <remarks>
    /// This method is only invoked if validation succeeds. If any validator fails, this method is never called.
    /// </remarks>
    protected abstract Task<CommandResult> HandleInternal(TCommand request, CancellationToken cancellationToken);

    /// <summary>
    /// Hook for side effects after command execution or validation failure. Override to add logging, event publishing, or other post-processing.
    /// </summary>
    /// <param name="request">The command that was executed.</param>
    /// <param name="originalCommandResult">The result from <see cref="HandleInternal"/> or the validation failure result.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="CommandResult"/>. The base implementation returns <paramref name="originalCommandResult"/> unchanged.
    /// Override to modify the result or perform side effects, but typically you should return the original result.
    /// </returns>
    /// <remarks>
    /// This method is called after <see cref="HandleInternal"/> (if validation succeeded) or after validation failure.
    /// It's invoked regardless of whether the command succeeded, failed, or was invalid.
    /// </remarks>
    protected virtual Task<CommandResult> PostHandle(TCommand request, CommandResult originalCommandResult,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(originalCommandResult);
    }
}