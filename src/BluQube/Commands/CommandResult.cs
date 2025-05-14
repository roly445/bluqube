using System.Text.Json.Serialization;
using BluQube.Constants;
using MaybeMonad;

namespace BluQube.Commands;

[JsonConverter(typeof(CommandResultConverter))]
public class CommandResult
{
    private readonly Maybe<BluQubeErrorData> _errorData;
    private readonly Maybe<CommandValidationResult> _commandValidationResult;

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

    public CommandResultStatus Status { get; }

    public static CommandResult Invalid(CommandValidationResult commandValidationFailure)
    {
        return new CommandResult(Maybe<BluQubeErrorData>.Nothing, commandValidationFailure);
    }

    public static CommandResult Failed(BluQubeErrorData blueQubeErrorData)
    {
        return new CommandResult(blueQubeErrorData, Maybe<CommandValidationResult>.Nothing);
    }

    public static CommandResult Succeeded()
    {
        return new CommandResult(Maybe<BluQubeErrorData>.Nothing, Maybe<CommandValidationResult>.Nothing);
    }

    public static CommandResult Unauthorized()
    {
        return new CommandResult(new BluQubeErrorData(BluQubeErrorCodes.NotAuthorized), Maybe<CommandValidationResult>.Nothing);
    }
}