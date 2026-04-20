using BluQube.Constants;
using MaybeMonad;

namespace BluQube.Commands;

/// <summary>
/// Represents the result of a command execution that returns typed data on success.
/// </summary>
/// <typeparam name="T">The type of data returned on successful execution. Must implement <see cref="ICommandResult"/>.</typeparam>
/// <remarks>
/// This is the return type for all <see cref="ICommand{TResult}"/> handlers. It extends <see cref="CommandResult"/> by adding a <see cref="Data"/> property
/// that contains the result data when <see cref="CommandResult.Status"/> is <see cref="CommandResultStatus.Succeeded"/>.
/// <para>
/// Like the base class, <see cref="CommandResult{T}"/> encapsulates four possible outcomes:
/// Succeeded (with data), Failed (with error), Invalid (with validation failures), or Unauthorized.
/// </para>
/// <para>
/// The <see cref="Data"/> property throws <see cref="InvalidOperationException"/> if accessed when <see cref="CommandResult.Status"/> is not <see cref="CommandResultStatus.Succeeded"/>.
/// Use factory methods (<see cref="Succeeded"/>, <see cref="Failed"/>, <see cref="Invalid"/>, <see cref="Unauthorized()"/>) to create instances.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In a handler:
/// protected override async Task&lt;CommandResult&lt;CreateTodoResult&gt;&gt; HandleInternal(
///     CreateTodoCommand request, CancellationToken cancellationToken)
/// {
///     var todo = new Todo { Id = Guid.NewGuid(), Title = request.Title };
///     await _repository.AddAsync(todo, cancellationToken);
///     return CommandResult&lt;CreateTodoResult&gt;.Succeeded(new CreateTodoResult(todo.Id));
/// }
/// 
/// // Consumer code:
/// var result = await commandRunner.Send(command);
/// if (result.Status == CommandResultStatus.Succeeded)
/// {
///     Console.WriteLine($"Created todo with ID: {result.Data.Id}");
/// }
/// </code>
/// </example>
public class CommandResult<T> : CommandResult
    where T : ICommandResult
{
    private readonly Maybe<T> _data;

    private CommandResult(Maybe<BluQubeErrorData> errorData, Maybe<CommandValidationResult> commandValidationResult, Maybe<T> data)
        : base(errorData, commandValidationResult)
    {
        this._data = data;
    }

    /// <summary>
    /// Gets the result data for a successful command execution.
    /// </summary>
    /// <value>An instance of <typeparamref name="T"/> containing the command result data.</value>
    /// <exception cref="InvalidOperationException">Thrown if <see cref="CommandResult.Status"/> is not <see cref="CommandResultStatus.Succeeded"/>.</exception>
    public T Data
    {
        get
        {
            if (this.Status != CommandResultStatus.Succeeded)
            {
                throw new InvalidOperationException("Data is only available when the status is Succeeded");
            }

            return this._data.Value;
        }
    }

    /// <summary>
    /// Creates a <see cref="CommandResult{T}"/> representing a command that failed FluentValidation.
    /// </summary>
    /// <param name="commandValidationResult">The validation result containing the list of failures.</param>
    /// <returns>A <see cref="CommandResult{T}"/> with status <see cref="CommandResultStatus.Invalid"/>.</returns>
    public new static CommandResult<T> Invalid(CommandValidationResult commandValidationResult)
    {
        return new CommandResult<T>(Maybe<BluQubeErrorData>.Nothing, commandValidationResult, Maybe<T>.Nothing);
    }

    /// <summary>
    /// Creates a <see cref="CommandResult{T}"/> representing a command that failed during execution.
    /// </summary>
    /// <param name="blueQubeErrorData">The error data describing the failure.</param>
    /// <returns>A <see cref="CommandResult{T}"/> with status <see cref="CommandResultStatus.Failed"/>.</returns>
    public new static CommandResult<T> Failed(BluQubeErrorData blueQubeErrorData)
    {
        return new CommandResult<T>(blueQubeErrorData, Maybe<CommandValidationResult>.Nothing, Maybe<T>.Nothing);
    }

    /// <summary>
    /// Creates a <see cref="CommandResult{T}"/> representing a command that executed successfully and returned data.
    /// </summary>
    /// <param name="data">The result data to return.</param>
    /// <returns>A <see cref="CommandResult{T}"/> with status <see cref="CommandResultStatus.Succeeded"/> and the provided data.</returns>
    public static CommandResult<T> Succeeded(T data)
    {
        return new CommandResult<T>(Maybe<BluQubeErrorData>.Nothing, Maybe<CommandValidationResult>.Nothing, data);
    }

    /// <summary>
    /// Creates a <see cref="CommandResult{T}"/> representing a command that was rejected due to authorization failure.
    /// </summary>
    /// <returns>A <see cref="CommandResult{T}"/> with status <see cref="CommandResultStatus.Failed"/> and error code <see cref="BluQubeErrorCodes.NotAuthorized"/>.</returns>
    /// <remarks>
    /// This factory is called automatically by <see cref="CommandRunner"/> when the MediatR authorization behavior throws <c>UnauthorizedException</c>.
    /// </remarks>
    public new static CommandResult<T> Unauthorized()
    {
        return new CommandResult<T>(new BluQubeErrorData(BluQubeErrorCodes.NotAuthorized), Maybe<CommandValidationResult>.Nothing, Maybe<T>.Nothing);
    }
}