using BluQube.Commands;
using BluQube.Tests.TestHelpers.Stubs;

namespace BluQube.Tests.Commands.CommandResultWithResultTests;

public class ErrorData
{
    [Fact]
    public async Task ReturnsDataWhenFailed()
    {
        // Arrange
        var commandResult = CommandResult<ICommandResult>.Failed(new BluQube.Commands.ErrorData("some-error"));

        // Act
        var result = commandResult.ErrorData;

        // Assert
        await Verify(result);
    }

    [Fact]
    public async Task ThrowsInvalidOperationExceptionSucceeded()
    {
        // Arrange
        var commandResult = CommandResult<ICommandResult>.Succeeded(new StubCommandWithResultResult("result"));

        // Act
        var exception = Record.Exception(() => commandResult.ErrorData);

        // Assert
        await Verify(exception);
    }

    [Fact]
    public async Task ReturnsDataWhenUnauthorized()
    {
        // Arrange
        var commandResult = CommandResult<ICommandResult>.Unauthorized();

        // Act
        var result = commandResult.ErrorData;

        // Assert
        await Verify(result);
    }

    [Fact]
    public async Task ThrowsInvalidOperationExceptionWhenInvalid()
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
        var exception = Record.Exception(() => commandResult.ErrorData);

        // Assert
        await Verify(exception);
    }
}