using BluQube.Commands;

namespace BluQube.Tests.Commands.CommandResultTests;

public class Status
{
    [Fact]
    public async Task ReturnsFailedWhenFailed()
    {
        // Arrange
        var commandResult = CommandResult.Failed(new BluQube.Commands.ErrorData("some-error"));

        // Act
        var result = commandResult.Status;

        // Assert
        await Verify(result);
    }

    [Fact]
    public async Task ReturnsSucceededWhenSucceeded()
    {
        // Arrange
        var commandResult = CommandResult.Succeeded();

        // Act
        var result = commandResult.Status;

        // Assert
        await Verify(result);
    }

    [Fact]
    public async Task ReturnsFailedWhenUnauthorized()
    {
        // Arrange
        var commandResult = CommandResult.Unauthorized();

        // Act
        var result = commandResult.Status;

        // Assert
        await Verify(result);
    }

    [Fact]
    public async Task ReturnsInvalidWhenInvalid()
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
        var result = commandResult.Status;

        // Assert
        await Verify(result);
    }
}