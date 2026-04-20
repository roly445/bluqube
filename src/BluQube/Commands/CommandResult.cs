using System.Text.Json.Serialization;
using BluQube.Constants;
using MaybeMonad;

namespace BluQube.Commands;

/// <summary>
/// Represents the result of a command execution that does not return data.
/// </summary>
/// <remarks>
/// This is the return type for all <see cref="ICommand"/> handlers. It encapsulates the outcome of command execution:
/// <list type="bullet">
/// <item><description><see cref="CommandResultStatus.Succeeded"/> — Command executed successfully.</description></item>
/// <item><description><see cref="CommandResultStatus.Failed"/> — Command execution failed with an error (access <see cref="ErrorData"/>).</description></item>
/// <item><description><see cref="CommandResultStatus.Invalid"/> — Command validation failed (access <see cref="ValidationResult"/>).</description></item>
/// <item><description><see cref="CommandResultStatus.Unknown"/> — Deserialization encountered an unrecognized status (should not occur in normal operation).</description></item>
/// </list>
/// <para>
/// The <see cref="ErrorData"/> and <see cref="ValidationResult"/> properties throw <see cref="InvalidOperationException"/> if accessed when <see cref="Status"/> is not the corresponding failure type.
/// Use factory methods (<see cref="Succeeded()"/>, <see cref="Failed"/>, <see cref="Invalid"/>, <see cref="Unauthorized()"/>) to create instances.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In a handler:
/// protected override async Task&lt;CommandResult&gt; HandleInternal(CreateTodoCommand request, CancellationToken cancellationToken)
/// {
///     var entity = new Entity { Title = request.Title };
///     await _repository.AddAsync(entity, cancellationToken);
///     return CommandResult.Succeeded();
/// }
///
/// // Error handling:
/// if (!_repository.Exists(request.Id))
/// {
///     return CommandResult.Failed(new BluQubeErrorData("ITEM_NOT_FOUND", "The item does not exist"));
/// }
///
/// // Consumer code:
/// var result = await commandRunner.Send(command);
/// if (result.Status == CommandResultStatus.Succeeded)
/// {
///     Console.WriteLine("Success!");
/// }
/// else if (result.Status == CommandResultStatus.Failed)
/// {
///     Console.WriteLine($"Error: {result.ErrorData.Message}");
/// }
/// </code>
/// </example>
[JsonConverter(typeof(CommandResultConverter))]
public class CommandResult
{
    private readonly Maybe<BluQubeErrorData> _errorData;
    private readonly Maybe<CommandValidationResult> _commandValidationResult;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandResult"/> class.
    /// </summary>
    /// <param name="errorData">Optional error data for failed commands.</param>
    /// <param name="commandValidationResult">Optional validation result for invalid commands.</param>
    /// <remarks>
    /// This constructor is protected to enforce the use of factory methods. The status is determined automatically:
    /// if both parameters are empty, status is <see cref="CommandResultStatus.Succeeded"/>;
    /// if <paramref name="errorData"/> has a value, status is <see cref="CommandResultStatus.Failed"/>;
    /// otherwise status is <see cref="CommandResultStatus.Invalid"/>.
    /// </remarks>
    protected CommandResult(Maybe<BluQubeErrorData> errorData, Maybe<CommandValidationResult> commandValidationResult)
    {
        this._errorData = errorData;
        this._commandValidationResult = commandValidationResult;
        if (errorData.HasNoValue && commandValidationResult.HasNoValue)
        {
            this.Status = CommandResultStatus.Succeeded;
        }
        else if (errorData.HasValue)
        {
            this.Status = CommandResultStatus.Failed;
        }
        else
        {
            this.Status = CommandResultStatus.Invalid;
        }
    }

    /// <summary>
    /// Gets the error data for a failed command.
    /// </summary>
    /// <value>A <see cref="BluQubeErrorData"/> instance containing the error code and message.</value>
    /// <exception cref="InvalidOperationException">Thrown if <see cref="Status"/> is not <see cref="CommandResultStatus.Failed"/>.</exception>
    public BluQubeErrorData ErrorData
    {
        get
        {
            if (this.Status != CommandResultStatus.Failed)
            {
                throw new InvalidOperationException("ErrorData is only available when the status is Failed");
            }

            return this._errorData.Value;
        }
    }

    /// <summary>
    /// Gets the validation result for an invalid command.
    /// </summary>
    /// <value>A <see cref="CommandValidationResult"/> containing the list of validation failures.</value>
    /// <exception cref="InvalidOperationException">Thrown if <see cref="Status"/> is not <see cref="CommandResultStatus.Invalid"/>.</exception>
    public CommandValidationResult ValidationResult
    {
        get
        {
            if (this.Status != CommandResultStatus.Invalid)
            {
                throw new InvalidOperationException("ValidationResult is only available when the status is Invalid");
            }

            return this._commandValidationResult.Value;
        }
    }

    /// <summary>
    /// Gets the status of the command execution.
    /// </summary>
    /// <value>A <see cref="CommandResultStatus"/> value indicating success, failure, validation error, or unknown.</value>
    public CommandResultStatus Status { get; }

    /// <summary>
    /// Creates a <see cref="CommandResult"/> representing a command that failed FluentValidation.
    /// </summary>
    /// <param name="commandValidationFailure">The validation result containing the list of failures.</param>
    /// <returns>A <see cref="CommandResult"/> with status <see cref="CommandResultStatus.Invalid"/>.</returns>
    public static CommandResult Invalid(CommandValidationResult commandValidationFailure)
    {
        return new CommandResult(Maybe<BluQubeErrorData>.Nothing, commandValidationFailure);
    }

    /// <summary>
    /// Creates a <see cref="CommandResult"/> representing a command that failed during execution.
    /// </summary>
    /// <param name="blueQubeErrorData">The error data describing the failure.</param>
    /// <returns>A <see cref="CommandResult"/> with status <see cref="CommandResultStatus.Failed"/>.</returns>
    public static CommandResult Failed(BluQubeErrorData blueQubeErrorData)
    {
        return new CommandResult(blueQubeErrorData, Maybe<CommandValidationResult>.Nothing);
    }

    /// <summary>
    /// Creates a <see cref="CommandResult"/> representing a command that executed successfully.
    /// </summary>
    /// <returns>A <see cref="CommandResult"/> with status <see cref="CommandResultStatus.Succeeded"/>.</returns>
    public static CommandResult Succeeded()
    {
        return new CommandResult(Maybe<BluQubeErrorData>.Nothing, Maybe<CommandValidationResult>.Nothing);
    }

    /// <summary>
    /// Creates a <see cref="CommandResult"/> representing a command that was rejected due to authorization failure.
    /// </summary>
    /// <returns>A <see cref="CommandResult"/> with status <see cref="CommandResultStatus.Failed"/> and error code <see cref="BluQubeErrorCodes.NotAuthorized"/>.</returns>
    /// <remarks>
    /// This factory is called automatically by <see cref="CommandRunner"/> when the MediatR authorization behavior throws <c>UnauthorizedException</c>.
    /// </remarks>
    public static CommandResult Unauthorized()
    {
        return new CommandResult(new BluQubeErrorData(BluQubeErrorCodes.NotAuthorized), Maybe<CommandValidationResult>.Nothing);
    }
}