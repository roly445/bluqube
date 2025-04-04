using BluQube.Commands;
using BluQube.Tests.TestHelpers.Stubs;

namespace BluQube.Tests.Commands.CommandResultWithResultTests;

public class Status
{
    [Fact]
    public async Task ReturnsFailedWhenFailed()
    {
        // Arrange
        var commandResult = CommandResult<ICommandResult>.Failed(new BluQube.Commands.ErrorData("some-error"));

        // Act
        var result = commandResult.Status;

        // Assert
        await Verify(result);
    }

    [Fact]
    public async Task ReturnsSucceededWhenSucceeded()
    {
        // Arrange
        var commandResult = CommandResult<ICommandResult>.Succeeded(new StubWithResultCommandResult("result"));

        // Act
        var result = commandResult.Status;

        // Assert
        await Verify(result);
    }

    [Fact]
    public async Task ReturnsFailedWhenUnauthorized()
    {
        // Arrange
        var commandResult = CommandResult<ICommandResult>.Unauthorized();

        // Act
        var result = commandResult.Status;

        // Assert
        await Verify(result);
    }

    [Fact]
    public async Task ReturnsInvalidWhenInvalid()
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
        var result = commandResult.Status;

        // Assert
        await Verify(result);
    }
}