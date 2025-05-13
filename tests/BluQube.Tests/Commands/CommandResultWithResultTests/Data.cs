using BluQube.Commands;
using BluQube.Tests.TestHelpers.Stubs;

namespace BluQube.Tests.Commands.CommandResultWithResultTests;

public class Data
{
    [Fact]
    public async Task ThrowsInvalidOperationExceptionWhenFailed()
    {
        // Arrange
        var commandResult = CommandResult<ICommandResult>.Failed(new BluQube.Commands.BlueQubeErrorData("some-error"));

        // Act
        var exception = Record.Exception(() => commandResult.Data);

        // Assert
        await Verify(exception);
    }

    [Fact]
    public async Task ReturnsDataWhenSucceeded()
    {
        // Arrange
        var commandResult = CommandResult<ICommandResult>.Succeeded(new StubWithResultCommandResult("result"));

        // Act
        var result = commandResult.Data;

        // Assert
        await Verify(result);
    }

    [Fact]
    public async Task ThrowsInvalidOperationExceptionWhenUnauthorized()
    {
        // Arrange
        var commandResult = CommandResult<ICommandResult>.Unauthorized();

        // Act
        var exception = Record.Exception(() => commandResult.Data);

        // Assert
        await Verify(exception);
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
        var exception = Record.Exception(() => commandResult.Data);

        // Assert
        await Verify(exception);
    }
}