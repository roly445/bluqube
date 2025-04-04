using BluQube.Constants;
using MaybeMonad;

namespace BluQube.Commands;

public class CommandResult<T> : CommandResult
    where T : ICommandResult
{
    private readonly Maybe<T> _data;

    private CommandResult(Maybe<ErrorData> errorData, Maybe<CommandValidationResult> commandValidationResult, Maybe<T> data)
        : base(errorData, commandValidationResult)
    {
        this._data = data;
    }

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

    public new static CommandResult<T> Invalid(CommandValidationResult commandValidationResult)
    {
        return new CommandResult<T>(Maybe<ErrorData>.Nothing, commandValidationResult, Maybe<T>.Nothing);
    }

    public new static CommandResult<T> Failed(ErrorData errorData)
    {
        return new CommandResult<T>(errorData, Maybe<CommandValidationResult>.Nothing, Maybe<T>.Nothing);
    }

    public static CommandResult<T> Succeeded(T data)
    {
        return new CommandResult<T>(Maybe<ErrorData>.Nothing, Maybe<CommandValidationResult>.Nothing, data);
    }

    public new static CommandResult<T> Unauthorized()
    {
        return new CommandResult<T>(new ErrorData(ErrorCodes.NotAuthorized), Maybe<CommandValidationResult>.Nothing, Maybe<T>.Nothing);
    }
}