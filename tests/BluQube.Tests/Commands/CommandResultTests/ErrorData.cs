using BluQube.Commands;

namespace BluQube.Tests.Commands.CommandResultTests;

public class ErrorData
{
    [Fact]
    public async Task ReturnsDataWhenFailed()
    {
        // Arrange
        var commandResult = CommandResult.Failed(new BluQube.Commands.BlueQubeErrorData("some-error"));

        // Act
        var result = commandResult.ErrorData;

        // Assert
        await Verify(result);
    }

    [Fact]
    public async Task ThrowsInvalidOperationExceptionSucceeded()
    {
        // Arrange
        var commandResult = CommandResult.Succeeded();

        // Act
        var exception = Record.Exception(() => commandResult.ErrorData);

        // Assert
        await Verify(exception);
    }

    [Fact]
    public async Task ReturnsDataWhenUnauthorized()
    {
        // Arrange
        var commandResult = CommandResult.Unauthorized();

        // Act
        var result = commandResult.ErrorData;

        // Assert
        await Verify(result);
    }

    [Fact]
    public async Task ThrowsInvalidOperationExceptionWhenInvalid()
    {
        // Arrange
        var commandResult = CommandResult.Invalid(new CommandValidationResult
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