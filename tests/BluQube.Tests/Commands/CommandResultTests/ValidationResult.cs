using BluQube.Commands;

namespace BluQube.Tests.Commands.CommandResultTests;

public class ValidationResult
{
    [Fact]
    public async Task ThrowsInvalidOperationExceptionWhenFailed()
    {
        // Arrange
        var commandResult = CommandResult.Failed(new BluQube.Commands.ErrorData("some-error"));

        // Act
        var exception = Record.Exception(() => commandResult.ValidationResult);

        // Assert
        await Verify(exception);
    }

    [Fact]
    public async Task ThrowsInvalidOperationExceptionWhenSucceeded()
    {
        // Arrange
        var commandResult = CommandResult.Succeeded();

        // Act
        var exception = Record.Exception(() => commandResult.ValidationResult);

        // Assert
        await Verify(exception);
    }

    [Fact]
    public async Task ThrowsInvalidOperationExceptionWhenUnauthorized()
    {
        // Arrange
        var commandResult = CommandResult.Unauthorized();

        // Act
        var exception = Record.Exception(() => commandResult.ValidationResult);

        // Assert
        await Verify(exception);
    }

    [Fact]
    public async Task ReturnDataWhenInvalid()
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
        var result = commandResult.ValidationResult;

        // Assert
        await Verify(result);
    }
}