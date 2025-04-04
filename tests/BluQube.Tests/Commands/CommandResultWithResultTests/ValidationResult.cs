using BluQube.Commands;
using BluQube.Tests.Commands.CommanderTests;
using BluQube.Tests.TestHelpers.Stubs;

namespace BluQube.Tests.Commands.CommandResultWithResultTests;

public class ValidationResult
{
    [Fact]
    public async Task ThrowsInvalidOperationExceptionWhenFailed()
    {
        // Arrange
        var commandResult = CommandResult<ICommandResult>.Failed(new BluQube.Commands.ErrorData("some-error"));

        // Act
        var exception = Record.Exception(() => commandResult.ValidationResult);

        // Assert
        await Verify(exception);
    }

    [Fact]
    public async Task ThrowsInvalidOperationExceptionWhenSucceeded()
    {
        // Arrange
        var commandResult = CommandResult<ICommandResult>.Succeeded(new StubCommandWithResultResult("result"));

        // Act
        var exception = Record.Exception(() => commandResult.ValidationResult);

        // Assert
        await Verify(exception);
    }

    [Fact]
    public async Task ThrowsInvalidOperationExceptionWhenUnauthorized()
    {
        // Arrange
        var commandResult = CommandResult<ICommandResult>.Unauthorized();

        // Act
        var exception = Record.Exception(() => commandResult.ValidationResult);

        // Assert
        await Verify(exception);
    }

    [Fact]
    public async Task ReturnDataWhenInvalid()
    {
        // Arrange
        var commandResult = CommandResult<ICommandResult>.Invalid(new CommandValidationResult
        {
            Failures = new List<CommandValidationFailure>
            {
                new("some-property", "some-error"),
            },
        });

        // Act
        var result = commandResult.ValidationResult;

        // Assert
        await Verify(result);
    }
}