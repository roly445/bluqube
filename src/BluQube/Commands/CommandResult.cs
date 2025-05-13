using System.Text.Json.Serialization;
using BluQube.Constants;
using MaybeMonad;

namespace BluQube.Commands;

[JsonConverter(typeof(CommandResultConverter))]
public class CommandResult
{
    private readonly Maybe<BlueQubeErrorData> _errorData;
    private readonly Maybe<CommandValidationResult> _commandValidationResult;

    protected CommandResult(Maybe<BlueQubeErrorData> errorData, Maybe<CommandValidationResult> commandValidationResult)
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

    public BlueQubeErrorData ErrorData
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
        return new CommandResult(Maybe<BlueQubeErrorData>.Nothing, commandValidationFailure);
    }

    public static CommandResult Failed(BlueQubeErrorData blueQubeErrorData)
    {
        return new CommandResult(blueQubeErrorData, Maybe<CommandValidationResult>.Nothing);
    }

    public static CommandResult Succeeded()
    {
        return new CommandResult(Maybe<BlueQubeErrorData>.Nothing, Maybe<CommandValidationResult>.Nothing);
    }

    public static CommandResult Unauthorized()
    {
        return new CommandResult(new BlueQubeErrorData(BluQubeErrorCodes.NotAuthorized), Maybe<CommandValidationResult>.Nothing);
    }
}